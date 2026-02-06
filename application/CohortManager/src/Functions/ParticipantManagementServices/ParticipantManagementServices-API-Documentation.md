# ParticipantManagementServices API Documentation

This documentation covers four Azure Functions services that handle participant management operations including creation, updates, deletion, and blocking.

---

## 1. DeleteParticipant Service

### DeleteParticipant Overview

Service that handles deletion and preview of participant records from the cohort distribution system.

**Function App URL:** <https://env-uks-delete-participant.azurewebsites.net>

### DeleteParticipant Endpoints

#### **GET/POST** `/api/DeleteParticipant`

**Purpose:** Deletes participant records matching all specified criteria

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z"
}
```

**Request Fields:**

- `nhsNumber` (string, required) - 10-digit NHS number
- `familyName` (string, required) - Participant's family name (surname)
- `dateOfBirth` (datetime, required) - Participant's date of birth

**Matching Logic:**
All three fields must match for a record to be deleted:

1. NHS number must match exactly
2. Family name must match exactly
3. Date of birth must match exactly

**Response Codes:**

- `200 OK` - Participant(s) deleted successfully
- `400 Bad Request` - Invalid request body, missing fields, or deserialization error
- `404 Not Found` - No participants found matching all criteria
- `500 Internal Server Error` - Database error or deletion failed

**Response Body (200 OK):**

```text
(Empty response body)
```

**Response Body (404 Not Found):**

```text
No participants found with the specified parameters
```

**Processing Flow:**

1. Validates all required fields are present
2. Queries cohort distribution table for NHS number and family name
3. Filters results by date of birth
4. If matches found, deletes all matching records
5. Logs exception if deletion fails

**Safety:**

- Multiple records can be deleted if they all match the criteria
- Three-point verification (NHS number, family name, DOB) provides safety check
- All deletions are logged

**Location:** DeleteParticipant.cs:43

---

#### **GET/POST** `/api/PreviewParticipant`

**Purpose:** Previews participant records that would be deleted without actually deleting them

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z"
}
```

**Request Fields:**

- `nhsNumber` (string, required) - 10-digit NHS number
- `familyName` (string, required) - Participant's family name
- `dateOfBirth` (datetime, required) - Participant's date of birth

**Response Codes:**

- `200 OK` - Matching participant(s) found, returns participant data
- `400 Bad Request` - Invalid request body or missing required fields
- `404 Not Found` - No matching participants found
- `500 Internal Server Error` - Database error

**Response Body (200 OK):**

```json
[
  {
    "cohortDistributionId": 12345,
    "nhsNumber": 1234567890,
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z",
    "firstName": "John",
    "screeningId": 67890,
    // ... other cohort distribution fields
  }
]
```

**Response Body (404 Not Found):**

```text
No matching records found.
```

**Use Case:**

- Preview participants before deletion
- Verify correct participants will be deleted
- Audit and safety check before destructive operation

**Location:** DeleteParticipant.cs:124

---

## 2. ManageParticipant Service

### ManageParticipant Overview

Service Bus triggered service that manages participant records by adding new participants or updating existing ones.

**Function App URL:** <https://env-uks-manage-participant.azurewebsites.net>

### ManageParticipant Functions

#### **Service Bus Trigger** `ManageParticipant`

**Type:** Service Bus triggered function (not an HTTP endpoint)

**Purpose:** Processes participant management messages to add or update participant records

**Trigger Configuration:**

- **Topic:** `ParticipantManagementTopic` (configured via environment variable)
- **Subscription:** `ManageParticipantSubscription` (configured via environment variable)
- **Connection:** `ServiceBusConnectionString_internal`

**Message Format:**

```json
{
  "Participant": {
    "nhsNumber": "1234567890",
    "screeningId": "67890",
    "participantId": "abc123",
    "recordType": "Add",
    // ... other participant fields
  },
  "FileName": "batch_001.parquet",
  "BasicParticipantData": {
    "nhsNumber": "1234567890",
    "screeningId": "67890",
    "recordType": "Add"
  }
}
```

**Processing Flow:**

1. **NHS Number Validation**
   - Validates NHS number format
   - If invalid, creates exception and returns

2. **Database Lookup**
   - Queries participant management table by NHS number and screening ID

3. **Add New Participant Path**
   - If participant not found in database:
   - Creates new `ParticipantManagement` record
   - Sets `RecordInsertDateTime` to current UTC time
   - Calls data service to add record

4. **Update Existing Participant Path**
   - If participant exists:
   - Checks if participant is blocked (`BlockedFlag == 1`)
   - If blocked, creates exception with category "0" and returns
   - If not blocked, updates existing record
   - Sets `RecordUpdateDateTime` to current UTC time
   - Calls data service to update record

5. **Queue for Distribution**
   - On successful add/update, sends participant to cohort distribution topic
   - Topic name from configuration: `CohortDistributionTopic`

**Error Handling:**

- Invalid NHS number → Exception created, processing stopped
- Blocked participant → Exception created with category "0", processing stopped
- Data service failure → Exception created, processing stopped
- All exceptions logged with participant details and filename

**Blocked Participant Behavior:**

- Participants with `BlockedFlag == 1` are not processed
- Exception created to track blocked participant attempt
- Prevents blocked participants from entering cohort distribution

**Location:** ManageParticipant.cs:39

---

## 3. ManageServiceNowParticipant Service

### ManageServiceNowParticipant Overview

Service Bus triggered service that processes participant additions from ServiceNow, validates against PDS, and manages participant records.

**Function App URL:** <https://env-uks-manage-servicenow-participant.azurewebsites.net>

### ManageServiceNowParticipant Functions

#### **Service Bus Trigger** `ManageServiceNowParticipantFunction`

**Type:** Service Bus triggered function (not an HTTP endpoint)

**Purpose:** Processes ServiceNow participant requests with PDS validation and NEMS subscription

**Trigger Configuration:**

- **Topic:** `ServiceNowParticipantManagementTopic` (configured via environment variable)
- **Subscription:** `ManageServiceNowParticipantSubscription` (configured via environment variable)
- **Connection:** `ServiceBusConnectionString_internal`

**Message Format:**

```json
{
  "nhsNumber": 1234567890,
  "screeningId": 67890,
  "serviceNowCaseNumber": "CASE123456",
  "firstName": "John",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z",
  "reasonForAdding": "VeryHighRisk",
  "primaryCareProvider": "GP001"
}
```

**Processing Flow:**

1. **PDS Validation**
   - Calls `RetrievePdsDemographic` API with NHS number
   - If 404 Not Found → Sends ServiceNow message "UnableToAddParticipant", returns
   - If non-200 response → Sends ServiceNow message "AddRequestInProgress", returns
   - Deserializes PDS demographic data

2. **Data Matching Validation**
   - Validates NHS number matches between ServiceNow and PDS
   - If mismatch → NHS number superseded, sends "UnableToAddParticipant", returns
   - Validates participant data matches using normalized name comparison:
     - First name must match (normalized)
     - Family name must match (normalized)
     - Date of birth must match
   - If data doesn't match → Sends "UnableToAddParticipant", returns

3. **Participant Management Record**
   - Queries participant management table
   - **If not exists:** Add new participant
     - Sets `RecordType = New`
     - Sets `EligibilityFlag = 1`
     - Sets `ReferralFlag = 1`
     - Sets `IsHigherRisk = 1` if `ReasonForAdding == VeryHighRisk`
     - Sets reason for removal fields from PDS
   - **If exists and not blocked:** Update participant
     - Sets `RecordType = Amended`
     - Updates eligibility and referral flags
     - Handles VHR flag (once set, remains set)
   - **If blocked:** Sends "UnableToAddParticipant", returns

4. **NEMS Subscription**
   - Subscribes participant to NEMS for demographic updates
   - Logs error if subscription fails (non-blocking)

5. **Queue for Distribution**
   - Creates `BasicParticipantCsvRecord` for distribution
   - Sends to `CohortDistributionTopic`
   - If queue send fails → Sends ServiceNow message "AddRequestInProgress"

**Name Normalization:**
Names are normalized before comparison to handle:

- Accented characters (É→E, Ñ→N, Ö→O)
- Spaces, hyphens, apostrophes removed
- Case-insensitive comparison
- Unicode NFD normalization

**ServiceNow Message Types:**

- `Success` - Participant successfully processed
- `UnableToAddParticipant` - Fatal error, cannot add participant
- `AddRequestInProgress` - Temporary error, can be retried

**VHR (Very High Risk) Handling:**

- Set to 1 if `ReasonForAdding == VeryHighRisk`
- Once set, flag is maintained on updates
- Never cleared automatically

**Location:** ManageServiceNowParticipantFunction.cs:46

---

## 4. UpdateBlockedFlag Service

### UpdateBlockedFlag Overview

Service that manages participant blocking and unblocking to prevent participants from being processed.

**Function App URL:** <https://env-uks-update-blocked-flag.azurewebsites.net>

### UpdateBlockedFlag Endpoints

#### **POST** `/api/BlockParticipant`

**Purpose:** Blocks a participant from being processed by setting BlockedFlag to 1

**HTTP Method:** POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z",
  "screeningId": "67890"
}
```

**Request Fields:**

- `nhsNumber` (string, required) - 10-digit NHS number
- `familyName` (string, required) - Participant's family name
- `dateOfBirth` (datetime, required) - Participant's date of birth
- `screeningId` (string, required) - Screening service ID

**Response Codes:**

- `200 OK` - Participant successfully blocked
- `400 Bad Request` - Invalid request, deserialization error, or participant not found/data mismatch
- `500 Internal Server Error` - Unexpected error

**Response Body (200 OK):**

```text
Participant successfully blocked
```

**Response Body (400 Bad Request):**

```text
Request couldn't be deserialized
```

or

```text
Participant not found or data doesn't match
```

**Processing:**

1. Validates request can be deserialized
2. Calls `BlockParticipantHandler` to process request
3. Handler performs three-point check (NHS number, family name, DOB)
4. Updates `BlockedFlag = 1` in participant management table
5. Returns success or error message

**Location:** UpdateBlockedFlag.cs:39

---

#### **POST** `/api/GetParticipant`

**Purpose:** Retrieves and validates participant details before blocking

**HTTP Method:** POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z",
  "screeningId": "67890"
}
```

**Request Fields:**

- `nhsNumber` (string, required) - 10-digit NHS number
- `familyName` (string, required) - Participant's family name
- `dateOfBirth` (datetime, required) - Participant's date of birth
- `screeningId` (string, required) - Screening service ID

**Response Codes:**

- `200 OK` - Participant found and validated
- `400 Bad Request` - Invalid NHS number or deserialization error
- `404 Not Found` - Participant not found
- `500 Internal Server Error` - Unexpected error

**Response Body (200 OK):**

```json
{
  "nhsNumber": "1234567890",
  "familyName": "Smith",
  "dateOfBirth": "1980-01-15T00:00:00Z",
  "screeningId": "67890",
  "blockedFlag": 0,
  // ... other participant details
}
```

**Response Body (404 Not Found):**

```text
Participant Couldn't be found
```

**Response Body (400 Bad Request):**

```text
Invalid NHS Number
```

**Processing:**

1. Looks up participant in Cohort Manager database
2. If not found, looks up in PDS
3. Performs three-point validation check
4. Returns participant details for manual verification
5. Used before blocking to ensure correct participant

**Use Case:**

- Preview participant before blocking
- Verify participant identity
- Manual assurance workflow

**Location:** UpdateBlockedFlag.cs:91

---

#### **POST** `/api/UnblockParticipant`

**Purpose:** Unblocks a participant by setting BlockedFlag to 0

**HTTP Method:** POST

**Query Parameters:**

- `nhsNumber` (required, string) - 10-digit NHS number

**Request Example:**

```text
POST /api/UnblockParticipant?nhsNumber=1234567890
```

**Response Codes:**

- `200 OK` - Participant successfully unblocked
- `400 Bad Request` - No NHS number provided, invalid NHS number, or participant data issue
- `404 Not Found` - Participant not found
- `500 Internal Server Error` - Unexpected error

**Response Body (200 OK):**

```text
Participant successfully unblocked
```

**Response Body (404 Not Found):**

```text
Participant Couldn't be found
```

**Response Body (400 Bad Request):**

```text
No NHS Number provided
```

or

```text
Invalid NHS Number provided
```

**Processing:**

1. Validates NHS number is provided and valid format
2. Calls `UnblockParticipantHandler`
3. Updates all participant management records for NHS number
4. Sets `BlockedFlag = 0`
5. Returns success or error message

**Note:** Unblocks all screening services for the participant (all screening IDs)

**Location:** UpdateBlockedFlag.cs:155

---

## Architecture & Data Flow

### Standard Participant Flow

```text
1. Source System/File Processing
   ↓
2. Service Bus → ParticipantManagementTopic
   ↓
3. ManageParticipant (Service Bus Trigger)
   ↓
4. Validate NHS Number
   ↓
5. Check if participant exists
   ├─ Not Exists → Add new participant
   └─ Exists → Check blocked flag
       ├─ Blocked → Create exception, stop
       └─ Not Blocked → Update participant
   ↓
6. Send to CohortDistributionTopic
   ↓
7. DistributeParticipant processes record
```

### ServiceNow Participant Flow

```text
1. ServiceNow Case Created
   ↓
2. Service Bus → ServiceNowParticipantManagementTopic
   ↓
3. ManageServiceNowParticipantFunction (Service Bus Trigger)
   ↓
4. Retrieve from PDS
   ├─ Not Found → Send "UnableToAddParticipant", stop
   └─ Found → Continue
   ↓
5. Validate data matches (normalized names, DOB)
   ├─ Doesn't Match → Send "UnableToAddParticipant", stop
   └─ Matches → Continue
   ↓
6. Check if participant exists
   ├─ Not Exists → Add new (set VHR if applicable)
   ├─ Exists & Blocked → Send "UnableToAddParticipant", stop
   └─ Exists & Not Blocked → Update (maintain VHR)
   ↓
7. Subscribe to NEMS
   ↓
8. Send to CohortDistributionTopic
   ↓
9. DistributeParticipant processes record
```

### Participant Blocking Flow

```text
1. User identifies participant to block
   ↓
2. POST /api/GetParticipant (preview)
   ↓
3. Verify correct participant
   ↓
4. POST /api/BlockParticipant
   ↓
5. BlockedFlag set to 1
   ↓
6. Future ManageParticipant calls rejected
```

### Participant Deletion Flow

```text
1. User identifies participant to delete
   ↓
2. POST /api/PreviewParticipant (preview)
   ↓
3. Review matching participants
   ↓
4. POST /api/DeleteParticipant
   ↓
5. All matching records deleted from cohort distribution
```

---

## Sample curl Commands

### DeleteParticipant Commands

```bash
# Preview participants before deletion
curl -X POST "https://durable-demographic-function.azurewebsites.net/api/PreviewParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z"
  }'

# Delete participants
curl -X POST "https://env-uks-delete-participant.azurewebsites.net/api/DeleteParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z"
  }'

# Preview with response
curl -i -X POST "https://env-uks-delete-participant.azurewebsites.net/api/PreviewParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @participant-details.json | jq .
```

### UpdateBlockedFlag Commands

```bash
# Get participant details before blocking
curl -X POST "https://env-uks-update-blocked-flag.azurewebsites.net/GetParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z",
    "screeningId": "67890"
  }'

# Block participant
curl -X POST "https://env-uks-update-blocked-flag.azurewebsites.net/api/BlockParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z",
    "screeningId": "67890"
  }'

# Unblock participant
curl -X POST "https://env-uks-update-blocked-flag.azurewebsites.net/api/UnblockParticipant?nhsNumber=1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get participant with response headers
curl -i -X POST "https://env-uks-update-blocked-flag.azurewebsites.net/api/GetParticipant" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @participant.json
```

---

## Configuration Summary

### DeleteParticipant

```json
{
  "CohortDistributionDataServiceURL": "https://cohort-distribution-url"
}
```

### ManageParticipant

```json
{
  "ParticipantManagementTopic": "participant-management-topic",
  "ManageParticipantSubscription": "manage-participant-sub",
  "ServiceBusConnectionString_internal": "service-bus-connection",
  "CohortDistributionTopic": "cohort-distribution-topic",
  "ParticipantManagementDataServiceURL": "https://participant-service-url"
}
```

### ManageServiceNowParticipant

```json
{
  "ServiceNowParticipantManagementTopic": "servicenow-participant-topic",
  "ManageServiceNowParticipantSubscription": "manage-servicenow-sub",
  "ServiceBusConnectionString_internal": "service-bus-connection",
  "CohortDistributionTopic": "cohort-distribution-topic",
  "RetrievePdsDemographicURL": "https://pds-demographic-url",
  "SendServiceNowMessageURL": "https://servicenow-message-url",
  "ManageNemsSubscriptionSubscribeURL": "https://nems-subscribe-url"
}
```

### UpdateBlockedFlag

```json
{
  "ParticipantManagementDataServiceURL": "https://participant-service-url",
  "RetrievePdsDemographicURL": "https://pds-demographic-url"
}
```

---

## Notes

### Blocked Flag Behavior

- **BlockedFlag = 0:** Normal processing (default)
- **BlockedFlag = 1:** Participant cannot be processed
- Blocking prevents:
  - Adding to cohort distribution
  - Updates to participant management
  - Processing through workflows
- Blocking does NOT delete existing records

### Three-Point Validation

Used across multiple endpoints for safety:

1. NHS Number (exact match)
2. Family Name (exact match or normalized)
3. Date of Birth (exact match)

Ensures correct participant is being operated on.

### VHR (Very High Risk) Flag

- Set automatically if `ReasonForAdding == VeryHighRisk` from ServiceNow
- Once set to 1, never automatically cleared
- Maintained across updates
- Affects screening pathway and prioritization

### Name Normalization in ServiceNow

ServiceNow participant processing uses normalized name comparison to handle:

- Different character encodings
- Accented characters
- Spaces and hyphens
- Case variations

Prevents false negative matches due to formatting differences.

### ServiceNow Message Types

- **Success:** Participant successfully added to cohort distribution
- **UnableToAddParticipant:** Fatal error (PDS not found, data mismatch, blocked)
- **AddRequestInProgress:** Temporary error (queue failure, PDS error)

Messages sent back to ServiceNow to update case status.
