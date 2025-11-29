# CohortDistributionServices API Documentation

This documentation covers four Azure Functions services that handle cohort distribution operations for screening services.

---

## 1. RetrieveCohortDistribution Service

### RetrieveCohortDistribution Overview

Service that retrieves cohort distribution participant data for external systems (e.g., BS SELECT).

**Function App URL:** <https://env-uks-retrieve-cohort-distribution-data.azurewebsites.net>

### RetrieveCohortDistribution Endpoints

#### **GET** `/api/RetrieveCohortDistributionData`

**Purpose:** Retrieves cohort distribution participant data based on request ID or returns unextracted participants

**HTTP Method:** GET

**Query Parameters:**

- `requestId` (optional, GUID) - Request identifier to retrieve specific cohort batch
- `rowCount` (optional, int) - Number of rows to return (capped by `MaxRowCount` configuration)

**Request Examples:**

```text
GET /api/RetrieveCohortDistributionData
GET /api/RetrieveCohortDistributionData?rowCount=50
GET /api/RetrieveCohortDistributionData?requestId=550e8400-e29b-41d4-a716-446655440000
GET /api/RetrieveCohortDistributionData?requestId=550e8400-e29b-41d4-a716-446655440000&rowCount=100
```

**Behavior:**

- **No requestId provided:** Returns batch of unextracted cohort distribution participants
- **requestId provided:**
  - If valid GUID: Retrieves next batch for that request ID from audit table
  - If next batch exists: Returns participants for that batch
  - If no next batch: Falls back to unextracted participants
  - If invalid GUID format: Returns 400 Bad Request

**Response Codes:**

- `200 OK` - Returns `List<CohortDistributionParticipantDto>` as JSON
- `204 No Content` - No participants found
- `400 Bad Request` - Invalid `requestId` format or request not found in database
- `500 Internal Server Error` - Exception occurred

**Response Body (200 OK):**

```json
[
  {
    "nhsNumber": "1234567890",
    "screeningId": "123456",
    "screeningName": "Breast Screening",
    "firstName": "Jane",
    "surname": "Doe",
    // ... other participant fields
  }
]
```

**Configuration Requirements:**

- `MaxRowCount` - Maximum number of records to return per request
- Database connection for cohort distribution data

**Authorization:** Anonymous

**Location:** RetrieveCohortDistribution.cs:54

---

## 2. RetrieveCohortRequestAudit Service

### RetrieveCohortRequestAudit Overview

Service that retrieves cohort distribution audit history for monitoring and tracking extraction requests.

**Function App URL:** <https://env-uks-retrieve-cohort-request-audit.azurewebsites.net>

### RetrieveCohortRequestAudit Endpoints

#### **GET** `/api/RetrieveCohortRequestAudit`

**Purpose:** Retrieves cohort audit history data based on request ID, status code, and/or date

**HTTP Method:** GET

**Query Parameters:**

- `requestId` (optional, string) - Filter by specific request ID
- `statusCode` (optional, int) - Filter by HTTP status code (200, 204, or 500 only)
- `dateFrom` (optional, date) - Filter records from this date onwards (format: `yyyyMMdd`)

**Request Examples:**

```text
GET /api/RetrieveCohortRequestAudit
GET /api/RetrieveCohortRequestAudit?dateFrom=20250101
GET /api/RetrieveCohortRequestAudit?statusCode=200
GET /api/RetrieveCohortRequestAudit?requestId=550e8400-e29b-41d4-a716-446655440000
GET /api/RetrieveCohortRequestAudit?statusCode=200&dateFrom=20250101
```

**Validation Rules:**

- `dateFrom` must be in format `yyyyMMdd` (ISO 8601 date)
- `statusCode` must be one of: `200`, `204`, or `500`
- All parameters are optional; if none provided, returns all audit records

**Response Codes:**

- `200 OK` - Returns `List<RequestAudit>` as JSON
- `204 No Content` - No audit records found
- `400 Bad Request` - Invalid date format or invalid status code
- `500 Internal Server Error` - Exception occurred

**Response Body (200 OK):**

```json
[
  {
    "requestId": "550e8400-e29b-41d4-a716-446655440000",
    "statusCode": 200,
    "requestDateTime": "2025-01-15T10:30:00Z",
    "recordCount": 100,
    // ... other audit fields
  }
]
```

**Error Response (400 Bad Request):**

```json
{
  "error": "Invalid date format. Please use yyyyMMdd."
}
```

or

```json
{
  "error": "Invalid status code. Only status codes 200, 204 and 500 are accepted."
}
```

**Authorization:** Anonymous

**Location:** RetrieveCohortRequestAudit.cs:46

---

## 3. DistributeParticipant Service

### DistributeParticipant Overview

Durable Functions orchestration service that distributes participants to cohort distribution table with validation and transformation.

**Function App URL:** <https://env-uks-distribute-participant.azurewebsites.net>

### DistributeParticipant Functions

#### **Service Bus Trigger** `DistributeParticipant`

**Type:** Service Bus triggered function (not an HTTP endpoint)

**Endpoint:** <https://env-uks-distribute-participant.azurewebsites.net/api/DistributeParticipant> (Note: not an HTTP endpoint in practice)

**Purpose:** Entry point that starts the participant distribution orchestration

**Trigger Configuration:**

- **Topic:** `CohortDistributionTopic` (configured via environment variable)
- **Subscription:** `DistributeParticipantSubscription` (configured via environment variable)
- **Connection:** `ServiceBusConnectionString_internal`

**Message Format:**

```json
{
  "BasicParticipantData": {
    "NhsNumber": "1234567890",
    "ScreeningId": "123456",
    "RecordType": "Add|Amend|Remove"
  },
  "Participant": {
    "ParticipantId": "abc123",
    "ScreeningId": "123456",
    "ReferralFlag": "0",
    "Postcode": "SW1A 1AA",
    "ScreeningAcronym": "BSS",
    "PrimaryCareProvider": "GP123"
  },
  "FileName": "source-file-name-or-servicenow-case-number"
}
```

**Required Fields:**

- `BasicParticipantData.ScreeningId`
- `BasicParticipantData.NhsNumber`

**Behavior:**

1. Validates required parameters (NhsNumber, ScreeningId)
2. Starts durable orchestration `DistributeParticipantOrchestrator`
3. Logs orchestration instance ID
4. Exceptions are logged to exception handler

**Location:** DistributeParticipant.cs:36

---

#### **Orchestrator** `DistributeParticipantOrchestrator`

**Type:** Durable Functions orchestrator (not an HTTP endpoint)

**Purpose:** Coordinates the complete participant distribution workflow

**Orchestration Steps:**

1. **Retrieve Participant Data** (`RetrieveParticipantData` activity)
   - Queries participant management and demographic tables
   - Combines data into `CohortDistributionParticipant`
   - Returns null if participant not found in either table

2. **Check Exception Status**
   - If participant has unresolved exception (`ExceptionFlag == 1`)
   - AND `IgnoreParticipantExceptions` is false
   - Logs exception and exits (participant not distributed)

3. **Allocate Service Provider** (`AllocateServiceProvider` activity)
   - Allocates service provider based on postcode area
   - Uses `allocationConfig.json` for postcode-to-provider mapping
   - Falls back to "BS SELECT" if no match found
   - For ServiceNow participants with PrimaryCareProvider, uses that instead

4. **Validation & Transformation** (`ValidationOrchestrator` sub-orchestrator)
   - Validates participant data
   - Transforms data according to business rules
   - Returns null if validation/transformation fails

5. **Add to Cohort Distribution** (`AddParticipant` activity)
   - Inserts participant into cohort distribution table
   - Sets extracted flag based on configuration
   - Sets record insert and update timestamps

6. **ServiceNow Notification** (`SendServiceNowMessage` activity)
   - Only executed if participant came from ServiceNow (`ReferralFlag == "1"`)
   - Sends success message to ServiceNow
   - Uses `FileName` property as ServiceNow case number

**Error Handling:**

- Each step can throw exceptions
- All exceptions logged to exception handler
- Failed participants do not proceed to next step

**Configuration Requirements:**

- `IgnoreParticipantExceptions` - Whether to distribute participants with exceptions
- `IsExtractedToBSSelect` - Default extraction flag value
- `SendServiceNowMessageURL` - URL for ServiceNow integration

**Location:** DistributeParticipant.cs:68

---

#### **Activity Functions**

##### `RetrieveParticipantData`

Retrieves participant data from management and demographic tables.

**Input:** `BasicParticipantData` (NhsNumber, ScreeningId, RecordType)

**Output:** `CohortDistributionParticipant` or null

**Location:** DistributeParticipantActivities.cs:45

---

##### `AllocateServiceProvider`

Allocates service provider based on postcode area.

**Input:** `Participant` (contains Postcode, ScreeningAcronym)

**Output:** `string` (service provider name)

**Logic:**

- Uses first part of postcode outcode for matching
- Matches against `allocationConfig.json`
- Finds longest matching postcode prefix
- Filters by screening acronym
- Default: "BS SELECT"

**Location:** DistributeParticipantActivities.cs:83

---

##### `AddParticipant`

Adds transformed participant to cohort distribution table.

**Input:** `CohortDistributionParticipant` (transformed)

**Output:** `bool` (success/failure)

**Side Effects:**

- Sets `Extracted` flag from configuration
- Sets `RecordInsertDateTime` and `RecordUpdateDateTime` to current UTC time
- Inserts record into database

**Location:** DistributeParticipantActivities.cs:111

---

##### `SendServiceNowMessage`

Sends success notification to ServiceNow.

**Input:** `string` (ServiceNow case number)

**Output:** None (async Task)

**HTTP Call:**

- Method: PUT
- URL: `{SendServiceNowMessageURL}/{caseNumber}`
- Body: `{ "MessageType": "Success" }`

**Location:** DistributeParticipantActivities.cs:128

---

## 4. TransformDataService

### TransformDataService Overview

Service that transforms cohort distribution participant data according to business rules and data quality standards.

**Function App URL:** <https://env-uks-transform-data-service.azurewebsites.net>

### TransformDataService Endpoints

#### **GET/POST** `/api/TransformDataService`

**Purpose:** Transforms participant data using rules engine and data quality checks

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "Participant": {
    "nhsNumber": "1234567890",
    "firstName": "Jane",
    "surname": "Doe",
    "screeningName": "Breast Screening",
    "namePrefix": "mrs",
    "recordType": "Add",
    "referralFlag": true,
    // ... other participant fields
  },
  "ExistingParticipant": {
    // Existing participant record from database (optional)
    // Used for comparison in transformation rules
  }
}
```

**Transformation Process:**

1. **NHS Number Validation**
   - Validates NHS Number can be parsed to long integer
   - Returns 400 Bad Request if invalid

2. **Character Transformation**
   - Removes invalid characters from string fields
   - Standardizes text formatting

3. **General Transformation Rules** (`transformRules.json`)
   - Executes "Common" workflow rules for all participants
   - Executes "Referred" workflow rules for referred participants with new records
   - Rules can transform field values based on business logic
   - Uses RulesEngine with custom `TransformAction`

4. **Name Prefix Transformation** (`namePrefixRules.json`)
   - Converts to uppercase
   - Validates against allowed prefixes
   - Standardizes variations (e.g., "mrs" → "Mrs")
   - Creates exception if invalid prefix
   - Returns null if prefix cannot be transformed

5. **Reason for Removal Transformation**
   - Special handling for removal reason codes
   - Compares with existing participant data

**Response Codes:**

- `200 OK` - Transformation successful, returns transformed participant
- `202 Accepted` - Transformation completed with warnings (exceptions logged)
- `400 Bad Request` - Invalid request body or transformation exception
- `500 Internal Server Error` - Unexpected exception occurred

**Response Body (200 OK):**

```json
{
  "nhsNumber": "1234567890",
  "firstName": "Jane",
  "surname": "Doe",
  "screeningName": "Breast Screening",
  "namePrefix": "Mrs",
  // ... transformed participant fields
}
```

**Error Handling:**

- **ArgumentException:** Logged as system exception, returns 202 Accepted
- **TransformationException:** Returns 400 Bad Request
- **General Exception:** Logged as system exception, returns 500 Internal Server Error

**Transformation Rules:**

The service uses two rule files:

1. **transformRules.json** - General transformation rules
   - Common rules apply to all participants
   - Referred rules apply to referred participants
   - Can access database lookup data
   - Can access excluded SMU list
   - Can compare with existing participant data

2. **namePrefixRules.json** - Name prefix standardization
   - Validates and transforms name prefixes
   - Creates exception (rule 83) for invalid prefixes
   - Logs transform executed exceptions with rule number

**Exception Categories:**

- **TransformExecuted (Category):** Successfully executed transformation logged for audit
- **TransformationException:** Failed transformation that prevents processing

**Configuration Requirements:**

- `transformRules.json` - Transformation rules configuration
- `namePrefixRules.json` - Name prefix rules configuration
- Database connection for data lookups
- Cached excluded SMU values

**Authorization:** Anonymous

**Location:** TransformDataService.cs:48

---

## Architecture & Data Flow

### Complete Participant Distribution Flow

```text
1. External System/File Processing
   ↓
2. Service Bus Message → DistributeParticipant (Service Bus Trigger)
   ↓
3. Start Orchestration → DistributeParticipantOrchestrator
   ↓
4. Retrieve Participant Data (Activity)
   ├─ Query Participant Management Table
   └─ Query Participant Demographic Table
   ↓
5. Check Exception Status
   ├─ If has exception AND not ignored → Exit
   └─ If OK or ignored → Continue
   ↓
6. Allocate Service Provider (Activity)
   └─ Match postcode to provider
   ↓
7. Validation & Transformation (Sub-orchestrator)
   ├─ Static Validation (External Service)
   └─ Transform Data Service (HTTP Call)
       ├─ Character transformation
       ├─ General rules (transformRules.json)
       ├─ Name prefix rules (namePrefixRules.json)
       └─ Reason for removal rules
   ↓
8. Add to Cohort Distribution (Activity)
   └─ Insert into database
   ↓
9. If ServiceNow Participant → Send Success Message (Activity)
   ↓
10. External System Retrieves Data
    └─ RetrieveCohortDistributionData API
```

---

## Sample curl Commands

### RetrieveCohortDistribution Commands

```bash
# Get unextracted participants (default row count)
curl -X GET "https://env-uks-retrieve-cohort-distribution-data.azurewebsites.net/api/RetrieveCohortDistributionData" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get with row count limit
curl -X GET "https://env-uks-retrieve-cohort-distribution-data.azurewebsites.net/api/RetrieveCohortDistributionData?rowCount=50" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get specific request batch
curl -X GET "https://env-uks-retrieve-cohort-distribution-data.azurewebsites.net/api/RetrieveCohortDistributionData?requestId=550e8400-e29b-41d4-a716-446655440000" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

```

### RetrieveCohortRequestAudit Commands

```bash
# Get all audit records
curl -X GET "https://env-uks-retrieve-cohort-request-audit.azurewebsites.net/api/RetrieveCohortRequestAudit" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get audit records from specific date
curl -X GET "https://env-uks-retrieve-cohort-request-audit.azurewebsites.net/api/RetrieveCohortRequestAudit?dateFrom=20250101" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get successful extractions (200 OK)
curl -X GET "https://env-uks-retrieve-cohort-request-audit.azurewebsites.net/api/RetrieveCohortRequestAudit?statusCode=200" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Combine filters
curl -X GET "https://env-uks-retrieve-cohort-request-audit.azurewebsites.net/api/RetrieveCohortRequestAudit?statusCode=200&dateFrom=20250101" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" | jq .

# Get specific request
curl -X GET "https://env-uks-retrieve-cohort-request-audit.azurewebsites.net/api/RetrieveCohortRequestAudit?requestId=550e8400-e29b-41d4-a716-446655440000" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"
```

### TransformDataService Commands

```bash
# Transform participant data
curl -X POST "https://env-uks-transform-data-service.azurewebsites.net/api/TransformDataService" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "Participant": {
      "nhsNumber": "1234567890",
      "firstName": "Jane",
      "surname": "Doe",
      "screeningName": "Breast Screening",
      "namePrefix": "mrs",
      "recordType": "Add",
      "referralFlag": true
    },
    "ExistingParticipant": null
  }'

# Transform with existing participant comparison
curl -X POST "https://env-uks-transform-data-service.azurewebsites.net/api/TransformDataService" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @participant-transform-request.json | jq .

# Include response headers
curl -i -X POST "https://env-uks-transform-data-service.azurewebsites.net/api/TransformDataService" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @participant-transform-request.json
```

---

## Configuration Summary

### RetrieveCohortDistribution

```json
{
  "MaxRowCount": 1000,
  "CohortDistributionDataServiceURL": "https://cohort-distribution-url",
  "BsSelectRequestAuditDataService": "https://audit-service-url"
}
```

### RetrieveCohortRequestAudit

```json
{
  "CohortDistributionDataServiceURL": "https://cohort-distribution-url",
  "BsSelectRequestAuditDataService": "https://audit-service-url"
}
```

### DistributeParticipant

```json
{
  "CohortDistributionTopic": "cohort-distribution-topic",
  "DistributeParticipantSubscription": "distribute-participant-sub",
  "ServiceBusConnectionString_internal": "service-bus-connection",
  "IgnoreParticipantExceptions": false,
  "IsExtractedToBSSelect": true,
  "SendServiceNowMessageURL": "https://servicenow-integration-url"
}
```

### TransformDataService

```json
{
  "transformRules.json": "transformation-rules-file-path",
  "namePrefixRules.json": "name-prefix-rules-file-path"
}
```
