# DemographicServices API Documentation

This documentation covers four Azure Functions services that handle participant demographic data operations, including PDS integration and NEMS/CAAS subscription management.

---

## 1. RetrievePDSDemographic Service

### RetrievePDSDemographic Overview

Service that retrieves participant demographic data from the Personal Demographics Service (PDS) using NHS number.

**Function App URL:** <https://env-uks-retrieve-pds-demographic.azurewebsites.net>

### RetrievePDSDemographic Endpoints

#### **GET** `/api/RetrievePdsDemographic`

**Purpose:** Retrieves and stores participant demographic information from PDS

**HTTP Method:** GET

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number
- `sourceFileName` (optional, string) - Source file name for audit trail

**Request Examples:**

```text
GET /api/RetrievePdsDemographic?nhsNumber=1234567890
GET /api/RetrievePdsDemographic?nhsNumber=1234567890&sourceFileName=batch_001.parquet
```

**Processing Flow:**

1. Validates NHS number format (10 digits)
2. Obtains bearer token for PDS authentication
3. Calls PDS API to retrieve demographic data
4. Parses FHIR JSON response
5. Checks confidentiality code (rejects if "R" - restricted)
6. Maps PDS data to internal ParticipantDemographic model
7. Upserts record to demographic database
8. Returns demographic data

**Response Codes:**

- `200 OK` - Demographic data successfully retrieved and stored
- `400 Bad Request` - Invalid or missing NHS number
- `404 Not Found` - Patient not found in PDS OR confidentiality code is "R"
- `500 Internal Server Error` - Bearer token error, PDS API error, or database error

**Response Body (200 OK):**

```json
{
  "nhsNumber": "1234567890",
  "firstName": "Jane",
  "surname": "Doe",
  "dateOfBirth": "1980-01-15",
  "postcode": "SW1A 1AA",
  "confidentialityCode": "U",
  // ... other demographic fields from PDS
}
```

**Special Handling:**

- **Confidentiality Code "R":** Treated as 404 Not Found to protect restricted records
- **404 from PDS:** Logged and exception created for tracking
- **Bearer Token Failure:** Returns 500 with error message

**PDS Integration:**

- Uses FHIR format for data exchange
- Requires OAuth bearer token authentication
- Endpoint format: `{RetrievePdsParticipantURL}/{nhsNumber}`

**Configuration Requirements:**

- `RetrievePdsParticipantURL` - Base URL for PDS API
- Bearer token service configuration
- Database connection for demographic storage

**Authorization:** Anonymous

**Location:** RetrievePDSDemographic.cs:47

---

## 2. DurableDemographicFunction Service

### DurableDemographicFunction Overview

Durable Functions orchestration service that handles bulk insertion of demographic data with retry logic.

**Function App URL:** <https://env-uks-durable-demographic-function.azurewebsites.net>

### DurableDemographicFunction Endpoints

#### **GET/POST** `/api/DurableDemographicFunction_HttpStart`

**Purpose:** Starts a durable orchestration to insert demographic data

**HTTP Methods:** GET, POST

**Request Body:**

```json
[
  {
    "nhsNumber": 1234567890,
    "firstName": "Jane",
    "surname": "Doe",
    "dateOfBirth": "1980-01-15T00:00:00Z",
    "postcode": "SW1A 1AA",
    // ... other ParticipantDemographic fields
  },
  {
    "nhsNumber": 9876543210,
    "firstName": "John",
    "surname": "Smith",
    // ... other fields
  }
]
```

**Response Codes:**

- `202 Accepted` - Orchestration started successfully
- `500 Internal Server Error` - Failed to start orchestration

**Response Body (202 Accepted):**

```json
{
  "id": "orchestration-instance-id",
  "statusQueryGetUri": "https://.../runtime/webhooks/durabletask/instances/orchestration-instance-id",
  "sendEventPostUri": "https://.../runtime/webhooks/durabletask/instances/orchestration-instance-id/raiseEvent/{eventName}",
  "terminatePostUri": "https://.../runtime/webhooks/durabletask/instances/orchestration-instance-id/terminate",
  "purgeHistoryDeleteUri": "https://.../runtime/webhooks/durabletask/instances/orchestration-instance-id"
}
```

**Orchestration Behavior:**

- Validates input data is not null or empty
- Calls `InsertDemographicData` activity with retry logic
- Retries up to `MaxRetryCount` times (configured)
- Each retry is logged with attempt number

**Location:** DurableDemographicFunction.cs:100

---

#### **GET/POST** `/api/GetOrchestrationStatus`

**Purpose:** Retrieves the status of a running or completed orchestration

**HTTP Methods:** GET, POST

**Request Body:**

```json
"orchestration-instance-id"
```

**Response Codes:**

- `200 OK` - Status retrieved or orchestration not found

**Response Body (200 OK):**

```text
Running
```

or

```text
Completed
```

or

```text
Failed
```

**Possible Statuses:**

- `Running` - Orchestration is currently executing
- `Completed` - Orchestration finished successfully
- `Failed` - Orchestration encountered an error
- `Pending` - Orchestration is waiting to start
- `Terminated` - Orchestration was manually terminated

**Special Behavior:**

- If orchestration instance is null, assumes it has completed
- Returns status as plain text (not JSON)

**Location:** DurableDemographicFunction.cs:130

---

### **Orchestrator** `DurableDemographicFunction`

**Type:** Durable Functions orchestrator (not an HTTP endpoint)

**Purpose:** Coordinates demographic data insertion with automatic retry

**Input:** JSON string containing `List<ParticipantDemographic>`

**Orchestration Steps:**

1. Deserialize input JSON to list of ParticipantDemographic
2. Validate input is not null or empty
3. Call `InsertDemographicData` activity with retry options
4. Retry up to configured MaxRetryCount on failure

**Retry Logic:**

- Automatically retries on failure
- Logs each retry attempt
- Stops after MaxRetryCount attempts

**Location:** DurableDemographicFunction.cs:41

---

### **Activity** `InsertDemographicData`

**Type:** Durable Functions activity (not an HTTP endpoint)

**Purpose:** Inserts demographic records into database

**Input:** JSON string containing `List<ParticipantDemographic>`

**Processing:**

1. Deserializes JSON to list of ParticipantDemographic

2. Calls data service to insert records in batch
3. Throws exception if insert fails (triggers retry)

**Error Handling:**

- Throws `InvalidOperationException` if records not added

- Exception triggers orchestrator retry logic

**Location:** DurableDemographicFunction.cs:79

---

## 3. ManageCaasSubscription Service

### ManageCaasSubscription Overview

Service that manages CAAS (Clinical Audit & Analytics Service) subscriptions via MESH mailbox integration.

**Function App URL:** <https://env-uks-manage-caas-subscription.azurewebsites.net>

### ManageCaasSubscription Endpoints

#### **POST** `/api/Subscribe`

**Purpose:** Creates a new CAAS subscription for a participant via MESH

**HTTP Method:** POST

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
POST /api/Subscribe?nhsNumber=1234567890
```

**Processing Flow:**

1. Validates NHS number format
2. Checks if subscription already exists in database
3. If exists, returns existing subscription ID
4. Sends subscription request to MESH mailbox
5. Saves subscription record to database with source = MESH
6. Returns message ID

**Response Codes:**

- `200 OK` - Subscription created or already exists
- `400 Bad Request` - Invalid NHS number
- `500 Internal Server Error` - MESH send failure or database save failure

**Response Body (200 OK - New):**

```text
Subscription request accepted. MessageId: mesh-message-id-123
```

**Response Body (200 OK - Existing):**

```text
Already subscribed. Subscription ID: mesh-message-id-456. Source: MESH
```

**MESH Integration:**

- Sends subscription request from `CaasFromMailbox` to `CaasToMailbox`
- Message ID used as subscription ID
- Subscription stored with source = MESH

**Location:** ManageCaasSubscription.cs:61

---

#### **POST** `/api/SubscribeMany`

**Purpose:** Creates CAAS subscriptions for multiple participants in a single request

**HTTP Method:** POST

**Request Body:**

```json
[
  "1234567890",
  "9876543210",
  "1111111111"
]
```

**Processing Flow:**

1. Validates all NHS numbers
2. Removes invalid NHS numbers to failed list
3. Checks database for existing subscriptions
4. Removes already-subscribed NHS numbers from batch
5. Sends batch subscription request to MESH
6. Saves subscription records with unique IDs

**Response Codes:**

- `200 OK` - Batch subscription sent
- `400 Bad Request` - No NHS numbers provided
- `500 Internal Server Error` - MESH send failure or database save failure

**Response Body (200 OK):**

```text
Subscription request accepted. MessageId: mesh-message-id-batch-789
```

**Subscription ID Format:**

- Each participant gets unique ID: `{messageId}_1`, `{messageId}_2`, etc.

**Special Handling:**

- Invalid NHS numbers are tracked but don't fail entire request
- Existing subscriptions are logged and skipped
- All valid, non-subscribed participants processed in single MESH message

**Location:** ManageCaasSubscription.cs:137

---

#### **POST** `/api/Unsubscribe`

**Purpose:** Stub endpoint for removing CAAS subscriptions

**HTTP Method:** POST

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
POST /api/Unsubscribe?nhsNumber=1234567890
```

**Response Codes:**

- `200 OK` - Stub response
- `400 Bad Request` - Invalid NHS number

**Response Body (200 OK):**

```text
Stub: CAAS subscription would be removed.
```

**Note:** This is a stub endpoint. Actual unsubscribe functionality not yet implemented.

**Location:** ManageCaasSubscription.cs:243

---

#### **GET** `/api/CheckSubscriptionStatus`

**Purpose:** Checks if an active CAAS subscription exists for an NHS number

**HTTP Method:** GET

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
GET /api/CheckSubscriptionStatus?nhsNumber=1234567890
```

**Response Codes:**

- `200 OK` - Active subscription found
- `400 Bad Request` - Invalid NHS number
- `404 Not Found` - No subscription found
- `500 Internal Server Error` - Database error

**Response Body (200 OK):**

```text
Active subscription found. Subscription ID: mesh-message-id-123
```

**Response Body (404 Not Found):**

```text
No subscription found for this NHS number.
```

**Location:** ManageCaasSubscription.cs:261

---

#### **ALL** `/api/NemsSubscriptionDataService/{*key}`

**Purpose:** Generic CRUD data service endpoint for NEMS subscription data

**HTTP Methods:** GET, POST, PUT, DELETE

**Route Parameter:**

- `key` (optional, string) - Route parameter for data service operations

**Behavior:**

- Pass-through to underlying data service handler
- Supports standard CRUD operations on NemsSubscription table
- Route tail can specify filters, IDs, or other data service operations

**Request Examples:**

```text
GET /api/NemsSubscriptionDataService
GET /api/NemsSubscriptionDataService/12345
POST /api/NemsSubscriptionDataService
PUT /api/NemsSubscriptionDataService/12345
DELETE /api/NemsSubscriptionDataService/12345
```

**Response Codes:**

- Varies based on data service operation
- `500 Internal Server Error` - Data service error

**Location:** ManageCaasSubscription.cs:309

---

#### **Timer Trigger** `PollMeshMailbox`

**Type:** Timer-triggered function (not an HTTP endpoint)

**Purpose:** Nightly MESH mailbox validation via handshake

**Trigger Schedule:** `59 23 * * *` (23:59 daily)

**Behavior:**

- Executes MESH handshake with configured mailbox
- Validates MESH connectivity
- Logs handshake result

**Configuration:**

- `CaasFromMailbox` - Mailbox ID to validate

**Location:** ManageCaasSubscription.cs:337

---

## 4. ManageNemsSubscription Service

### ManageNemsSubscription Overview

Service that manages NEMS (National Events Management Service) subscriptions for demographic change notifications.

**Function App URL:** <https://env-uks-manage-nems-subscription.azurewebsites.net>

### ManageNemsSubscription Endpoints

#### **POST** `/api/Subscribe`

**Purpose:** Creates a new NEMS subscription for a participant

**HTTP Method:** POST

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
POST /api/Subscribe?nhsNumber=1234567890
```

**Processing Flow:**

1. Validates NHS number format
2. Calls subscription manager to create and send subscription to NEMS
3. Returns subscription ID on success

**Response Codes:**

- `200 OK` - Subscription created successfully
- `400 Bad Request` - Invalid NHS number
- `500 Internal Server Error` - Failed to create subscription in NEMS

**Response Body (200 OK):**

```text
Subscription created successfully. Subscription ID: nems-sub-id-123
```

**NEMS Integration:**

- Creates subscription in NEMS API
- Subscription ID returned from NEMS
- Subscription stored with source = NEMS

**Location:** ManageNemsSubscription.cs:44

---

#### **POST** `/api/Unsubscribe`

**Purpose:** Removes a NEMS subscription for a participant

**HTTP Method:** POST

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
POST /api/Unsubscribe?nhsNumber=1234567890
```

**Processing Flow:**

1. Validates NHS number format
2. Looks up subscription ID from database
3. If not found, returns 404
4. Removes subscription from NEMS
5. Returns success

**Response Codes:**

- `200 OK` - Successfully unsubscribed
- `400 Bad Request` - Invalid NHS number
- `404 Not Found` - No subscription found
- `500 Internal Server Error` - Failed to remove subscription

**Response Body (200 OK):**

```text
Successfully unsubscribed
```

**Response Body (404 Not Found):**

```text
No subscription found.
```

**Location:** ManageNemsSubscription.cs:85

---

#### **GET** `/api/CheckSubscriptionStatus`

**Purpose:** Checks if an active NEMS subscription exists for an NHS number

**HTTP Method:** GET

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
GET /api/CheckSubscriptionStatus?nhsNumber=1234567890
```

**Response Codes:**

- `200 OK` - Active subscription found
- `400 Bad Request` - Invalid NHS number
- `404 Not Found` - No subscription found
- `500 Internal Server Error` - Database error

**Response Body (200 OK):**

```text
Active subscription found. Subscription ID: nems-sub-id-123
```

**Response Body (404 Not Found):**

```text
No subscription found for this NHS number.
```

**Location:** ManageNemsSubscription.cs:136

---

#### **ALL** `/api/NemsSubscriptionDataService/{*key}`

**Purpose:** Generic CRUD data service endpoint for NEMS subscription data

**HTTP Methods:** GET, POST, PUT, DELETE

**Route Parameter:**

- `key` (optional, string) - Route parameter for data service operations

**Behavior:**

- Pass-through to underlying data service handler
- Supports standard CRUD operations on NemsSubscription table
- Same table used by both NEMS and CAAS subscriptions

**Request Examples:**

```text
GET /api/NemsSubscriptionDataService
GET /api/NemsSubscriptionDataService/12345
POST /api/NemsSubscriptionDataService
PUT /api/NemsSubscriptionDataService/12345
DELETE /api/NemsSubscriptionDataService/12345
```

**Response Codes:**

- Varies based on data service operation
- `500 Internal Server Error` - Data service error

**Location:** ManageNemsSubscription.cs:170

---

## Architecture & Data Flow

### PDS Demographic Retrieval Flow

```text
1. External System/API Call
   ↓
2. RetrievePdsDemographic (HTTP GET)
   ↓
3. Obtain Bearer Token
   ↓
4. Call PDS API (FHIR format)
   ↓
5. Check Response
   ├─ 404 or Confidentiality Code "R" → Create Exception → Return 404
   └─ Success → Parse FHIR JSON
   ↓
6. Map to ParticipantDemographic
   ↓
7. Upsert to Database
   ↓
8. Return Demographic Data
```

### Bulk Demographic Insert Flow

```text
1. External System sends demographic batch
   ↓
2. DurableDemographicFunction_HttpStart (HTTP POST)
   ↓
3. Start Orchestration
   ↓
4. DurableDemographicFunction (Orchestrator)
   ↓
5. InsertDemographicData (Activity)
   ├─ Success → Complete
   └─ Failure → Retry (up to MaxRetryCount)
   ↓
6. Poll GetOrchestrationStatus to check completion
```

### CAAS Subscription Flow

```text
1. Subscribe Request (HTTP POST)
   ↓
2. Validate NHS Number
   ↓
3. Check Existing Subscription
   ├─ Exists → Return Existing ID
   └─ Not Exists → Continue
   ↓
4. Send Subscription via MESH
   ↓
5. Save to Database (source = MESH)
   ↓
6. Return Message ID
   ↓
7. Nightly: PollMeshMailbox validates connectivity
```

### NEMS Subscription Flow

```text
1. Subscribe Request (HTTP POST)
   ↓
2. Validate NHS Number
   ↓
3. Create Subscription in NEMS API
   ↓
4. Save to Database (source = NEMS)
   ↓
5. Return Subscription ID
```

---

## Sample curl Commands

### RetrievePDSDemographic

```bash
# Retrieve demographic data from PDS
curl -X GET "https://env-uks-retrieve-pds-demographic.azurewebsites.net/api/RetrievePdsDemographic?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Retrieve with source file tracking
curl -X GET "https://env-uks-retrieve-pds-demographic.azurewebsites.net/api/RetrievePdsDemographic?nhsNumber=1234567890&sourceFileName=batch_001.parquet" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Pretty-printed response
curl -X GET "https://env-uks-retrieve-pds-demographic.azurewebsites.net/api/RetrievePdsDemographic?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" | jq .
```

### DemographicDurableFunction

```bash
# Start demographic insert orchestration
curl -X POST "https://env-uks-durable-demographic-function.azurewebsites.net/api/DurableDemographicFunction_HttpStart" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '[
    {
      "nhsNumber": 1234567890,
      "firstName": "Jane",
      "surname": "Doe",
      "dateOfBirth": "1980-01-15T00:00:00Z"
    }
  ]'

# Check orchestration status
curl -X POST "https://env-uks-durable-demographic-function.azurewebsites.net/api/GetOrchestrationStatus" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '"orchestration-instance-id"'

# Using durable functions status endpoint (from 202 response)
curl -X GET "https://env-uks-durable-demographic-function.azurewebsites.net/runtime/webhooks/durabletask/instances/orchestration-instance-id" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"
```

### ManageCaasSubscription

```bash
# Create CAAS subscription
curl -X POST "https://env-uks-manage-caas-subscription.azurewebsites.net/api/Subscribe?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Create multiple CAAS subscriptions
curl -X POST "https://env-uks-manage-caas-subscription.azurewebsites.net/api/SubscribeMany" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '["1234567890", "9876543210", "1111111111"]'

# Check CAAS subscription status
curl -X GET "https://env-uks-manage-caas-subscription.azurewebsites.net/api/CheckSubscriptionStatus?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Unsubscribe (stub)
curl -X POST "https://env-uks-manage-caas-subscription.azurewebsites.net/api/Unsubscribe?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Query subscription data service
curl -X GET "https://env-uks-manage-caas-subscription.azurewebsites.net/api/NemsSubscriptionDataService" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"
```

### ManageNemsSubscription

```bash
# Create NEMS subscription
curl -X POST "https://env-uks-manage-nems-subscription.azurewebsites.net/api/Subscribe?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Remove NEMS subscription
curl -X POST "https://env-uks-manage-nems-subscription.azurewebsites.net/api/Unsubscribe?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Check NEMS subscription status
curl -X GET "https://env-uks-manage-nems-subscription.azurewebsites.net/api/CheckSubscriptionStatus?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Query subscription data service
curl -X GET "https://env-uks-manage-nems-subscription.azurewebsites.net/api/NemsSubscriptionDataService" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"
```

---

## Configuration Summary

### RetrievePDSDemographic Summary

```json
{
  "RetrievePdsParticipantURL": "https://pds-api-url/Patient",
  "BearerTokenServiceConfig": {
    "TokenEndpoint": "https://auth-url/token",
    "ClientId": "client-id",
    "ClientSecret": "client-secret"
  }
}
```

### DemographicDurableFunction Summary

```json
{
  "MaxRetryCount": 3,
  "ParticipantDemographicDataServiceURL": "https://demographic-service-url"
}
```

### ManageCaasSubscription Summary

```json
{
  "CaasToMailbox": "target-mesh-mailbox-id",
  "CaasFromMailbox": "source-mesh-mailbox-id",
  "IsStubbed": false,
  "MeshApiBaseUrl": "https://mesh-api-url"
}
```

### ManageNemsSubscription Summary

```json
{
  "NemsApiBaseUrl": "https://nems-api-url",
  "NemsApiKey": "api-key",
  "NemsSubscriptionDataServiceURL": "https://nems-subscription-service-url"
}
```

---

## Notes

### Subscription Source Tracking

Both CAAS and NEMS subscriptions are stored in the same `NEMS_SUBSCRIPTION` table with a `SubscriptionSource` field to distinguish:

- `SubscriptionSource.MESH` - CAAS subscriptions via MESH
- `SubscriptionSource.NEMS` - NEMS subscriptions via API

### PDS Confidentiality Codes

- **"U"** (Unrestricted) - Normal processing
- **"R"** (Restricted) - Treated as 404 Not Found
- **"V"** (Very Restricted) - Typically also restricted

### Durable Functions

The DemographicDurableFunction uses Azure Durable Functions to:

- Provide reliable message processing
- Automatic retry on transient failures
- Status tracking for long-running operations
- Guaranteed execution semantics
