# ServiceNowIntegration API Documentation

This documentation covers two Azure Functions services that handle bidirectional integration with ServiceNow for participant management cases.

---

## 1. ServiceNowMessageHandler Service

### ServiceNowMessageHandler Overview

Service that handles incoming participant requests from ServiceNow and sends status updates back to ServiceNow cases.

**Function App URL:** <https://env-uks-servicenow-message-handler.azurewebsites.net>

### ServiceNowMessageHandler Endpoints

#### **POST** `/api/servicenow/receive`

**Purpose:** Receives participant addition requests from ServiceNow and queues them for processing

**HTTP Method:** POST

**Request Body:**

```json
{
  "serviceNowCaseNumber": "CASE123456",
  "variableData": {
    "nhsNumber": "1234567890",
    "firstName": "John",
    "familyName": "Smith",
    "dateOfBirth": "1980-01-15T00:00:00Z",
    "bsoCode": "BSO001",
    "reasonForAdding": "VeryHighRisk",
    "requiredGpCode": "GP001"
  }
}
```

**Request Fields:**

- `serviceNowCaseNumber` (string, required) - ServiceNow case identifier
- `variableData` (object, required) - Participant details
  - `nhsNumber` (string, required) - 10-digit NHS number
  - `firstName` (string, required) - Participant's first name
  - `familyName` (string, required) - Participant's family name
  - `dateOfBirth` (datetime, required) - Participant's date of birth
  - `bsoCode` (string, optional) - Breast Screening Office code
  - `reasonForAdding` (string, required) - Reason code (e.g., "VeryHighRisk")
  - `requiredGpCode` (string, optional) - Required GP practice code

**Validation Rules:**

- `serviceNowCaseNumber` must not be null or whitespace
- All required fields in `variableData` must be present
- `variableData` must pass data annotation validation
- `nhsNumber` must be parseable to long integer

**Response Codes:**

- `202 Accepted` - Request received and queued successfully
- `400 Bad Request` - Invalid request body, missing fields, or validation failure
- `500 Internal Server Error` - Database save error or queue send failure

**Response Body (202 Accepted):**

```text
(Empty response body)
```

**Processing Flow:**

1. Deserializes and validates request body
2. Validates ServiceNow case number is present
3. Validates variable data using data annotations
4. Creates `ServicenowCase` record in database
   - Sets `Id` to new GUID
   - Sets `ServicenowId` to case number
   - Sets `NhsNumber` from request
   - Sets `Status` to "New"
   - Sets `RecordInsertDatetime` to current UTC
5. Creates `ServiceNowParticipant` object
   - Hardcodes `ScreeningId = 1` (Breast Screening)
   - Maps all participant data from request
6. Sends participant to Service Bus topic
   - Topic: `ServiceNowParticipantManagementTopic`
7. Returns 202 Accepted

**ServiceNow Case Tracking:**

- Case stored in database with status "New"
- Case updated to "Complete" when participant successfully added to cohort distribution
- Case tracked for auditing and reconciliation

**Location:** ReceiveServiceNowMessageFunction.cs:44

---

#### **PUT** `/api/servicenow/send/{caseNumber}`

**Purpose:** Sends status update or resolution message back to ServiceNow case

**HTTP Method:** PUT

**Route Parameters:**

- `caseNumber` (string, required) - ServiceNow case number

**Request Body:**

```json
{
  "messageType": "Success"
}
```

**Request Fields:**

- `messageType` (enum, required) - Type of message to send
  - `Success` - Participant successfully added
  - `UnableToAddParticipant` - Fatal error, cannot add participant
  - `AddRequestInProgress` - Temporary error, processing in progress

**Request Examples:**

```text
PUT /api/servicenow/send/CASE123456
PUT /api/servicenow/send/CASE789012
```

**Response Codes:**

- `200 OK` - Message sent to ServiceNow successfully
- `400 Bad Request` - Invalid request body or deserialization error
- `500 Internal Server Error` - ServiceNow API call failed or unexpected error

**Response Body (200 OK):**

```text
(Empty response body)
```

**Message Templates:**

**Success Message:**

- Sends resolution to ServiceNow
- Template: "Case {caseNumber} has been successfully processed. The participant has been added to the cohort distribution."
- Resolves and closes the case

**UnableToAddParticipant Message:**

- Sends update to ServiceNow
- Template: "Unable to add participant for case {caseNumber}. Please review the case details and try again."
- Sets case to failed state
- Parameter: `isFailedState = true`

**AddRequestInProgress Message:**

- Sends update to ServiceNow
- Template: "Add request for case {caseNumber} is in progress. The system is processing the participant addition."
- Updates case with in-progress status

**ServiceNow API Integration:**

- Uses `ServiceNowClient` to communicate with ServiceNow API
- Two operation types:
  - `SendUpdate(caseNumber, message, isFailedState)` - Updates case with message
  - `SendResolution(caseNumber, message)` - Resolves case with success message

**Error Handling:**

- Logs ServiceNow API status codes on failure
- Returns 500 if ServiceNow API call fails
- All exceptions logged with context

**Location:** SendServiceNowMessageFunction.cs:38

---

## 2. ServiceNowCohortLookup Service

### ServiceNowCohortLookup Overview

Daily scheduled service that matches ServiceNow cases with cohort distribution participants for auditing and case completion.

**Function App URL:** <https://env-uks-servicenow-cohort-lookup.azurewebsites.net>

### ServiceNowCohortLookup Functions

#### **Timer Trigger** `ServiceNowCohortLookup`

**Type:** Timer-triggered function (not an HTTP endpoint)

**Purpose:** Daily reconciliation of ServiceNow cases with cohort distribution participants

**Trigger Schedule:** `0 0 0 * * *` (Daily at midnight UTC)

**Processing Flow:**

1. **Retrieve New ServiceNow Cases**
   - Queries `ServicenowCase` table
   - Filters by `Status == "New"`
   - Logs count of new cases found

2. **Retrieve Yesterday's Participants**
   - Queries `CohortDistribution` table
   - Filters by `RecordInsertDateTime.Date == Yesterday`
   - Logs count of participants found

3. **Match Cases to Participants**
   - Creates NHS number lookup dictionary from participants
   - For each ServiceNow case:
     - Validates NHS number exists and is valid format
     - Looks up participant by NHS number
     - If match found, updates case status to "Complete"

4. **Update Matched Cases**
   - Sets `Status = "Complete"`
   - Sets `RecordUpdateDatetime = DateTime.UtcNow`
   - Persists to database

5. **Log Results**
   - Logs processed count / total cases
   - Logs next scheduled execution time

**Matching Logic:**

- Match is based solely on NHS number
- Only considers participants inserted yesterday
- Only processes cases with status "New"

**Error Handling:**

- Case-level errors logged as warnings
- Processing continues for remaining cases
- Top-level errors logged as errors
- Individual case failures don't stop batch processing

**Logging:**

- Start time logged
- New cases count logged
- Yesterday's participants count logged
- Each case status update logged
- Processing summary logged (success/total)
- Next execution time logged

**Performance:**

- Uses dictionary lookup for O(1) participant matching
- Processes all cases in single batch
- Filters participants by date for efficiency

**Audit Trail:**

- Tracks which ServiceNow cases have been completed
- Provides reconciliation between ServiceNow and cohort distribution
- Enables reporting on processing delays

**Location:** ServiceNowCohortLookup.cs:38

---

## Architecture & Data Flow

### ServiceNow to Cohort Manager Flow

```text
1. ServiceNow Case Created
   ↓
2. ServiceNow calls POST /api/servicenow/receive
   ↓
3. ReceiveServiceNowMessage endpoint
   ├─ Validate request
   ├─ Create ServicenowCase record (Status = "New")
   └─ Create ServiceNowParticipant object
   ↓
4. Send to ServiceNowParticipantManagementTopic
   ↓
5. ManageServiceNowParticipantFunction processes
   ├─ PDS validation
   ├─ Data matching
   └─ Add/Update participant management
   ↓
6. Send to CohortDistributionTopic
   ↓
7. DistributeParticipant processes
   ├─ Validation & Transformation
   └─ Add to cohort distribution
   ↓
8. SendServiceNowMessage called with "Success"
   ↓
9. PUT /api/servicenow/send/{caseNumber}
   ↓
10. ServiceNow case resolved and closed
```

### Error Flow - Unable to Add Participant

```text
1-4. [Same as above]
   ↓
5. ManageServiceNowParticipantFunction encounters error
   ├─ PDS not found
   ├─ Data doesn't match
   └─ Participant blocked
   ↓
6. SendServiceNowMessage called with "UnableToAddParticipant"
   ↓
7. PUT /api/servicenow/send/{caseNumber}
   ↓
8. ServiceNow case marked as failed
```

### Error Flow - Temporary Error

```text
1-4. [Same as above]
   ↓
5. ManageServiceNowParticipantFunction encounters temporary error
   ├─ Queue send failure
   ├─ PDS API error
   └─ Database timeout
   ↓
6. SendServiceNowMessage called with "AddRequestInProgress"
   ↓
7. PUT /api/servicenow/send/{caseNumber}
   ↓
8. ServiceNow case updated with in-progress status
   ↓
9. Can be retried or manually investigated
```

### Daily Reconciliation Flow

```text
1. Timer triggers at midnight UTC
   ↓
2. ServiceNowCohortLookup function starts
   ↓
3. Query all cases with Status = "New"
   ↓
4. Query all participants inserted yesterday
   ↓
5. For each new case:
   ├─ Match by NHS number to participant
   ├─ If match found → Update case to "Complete"
   └─ If no match → Leave as "New"
   ↓
6. Log summary: X/Y cases processed
   ↓
7. Schedule next execution
```

---

## Sample curl Commands

### ReceiveServiceNowMessage

```bash
# Receive participant request from ServiceNow
curl -X POST "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/receive" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "serviceNowCaseNumber": "CASE123456",
    "variableData": {
      "nhsNumber": "1234567890",
      "firstName": "John",
      "familyName": "Smith",
      "dateOfBirth": "1980-01-15T00:00:00Z",
      "bsoCode": "BSO001",
      "reasonForAdding": "VeryHighRisk",
      "requiredGpCode": "GP001"
    }
  }'

# VHR (Very High Risk) participant
curl -X POST "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/receive" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "serviceNowCaseNumber": "CASE789012",
    "variableData": {
      "nhsNumber": "9876543210",
      "firstName": "Jane",
      "familyName": "Doe",
      "dateOfBirth": "1975-05-20T00:00:00Z",
      "reasonForAdding": "VeryHighRisk"
    }
  }'

# Include response headers
curl -i -X POST "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/receive" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @servicenow-request.json
```

### SendServiceNowMessage

```bash
# Send success message
curl -X PUT "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/send/CASE123456" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "messageType": "Success"
  }'

# Send unable to add message
curl -X PUT "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/send/CASE789012" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "messageType": "UnableToAddParticipant"
  }'

# Send in-progress message
curl -X PUT "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/send/CASE456789" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "messageType": "AddRequestInProgress"
  }'

# Include response headers
curl -i -X PUT "https://env-uks-servicenow-message-handler.azurewebsites.net/api/servicenow/send/CASE123456" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{"messageType": "Success"}'
```

---

## ServiceNow Case Lifecycle

### Complete Success Path

```text
1. NEW → Case created in ServiceNow
   ↓
2. NEW → ReceiveServiceNowMessage creates case record
   ↓
3. NEW → Participant queued for processing
   ↓
4. NEW → Participant validated and processed
   ↓
5. COMPLETE → Participant added to cohort distribution
   ↓
6. COMPLETE → SendServiceNowMessage sends "Success"
   ↓
7. COMPLETE → ServiceNow case resolved
   ↓
8. COMPLETE → Daily lookup confirms completion
```

### Failed Path

```text
1. NEW → Case created in ServiceNow
   ↓
2. NEW → ReceiveServiceNowMessage creates case record
   ↓
3. NEW → Participant queued for processing
   ↓
4. NEW → Validation fails (PDS not found, data mismatch)
   ↓
5. NEW → SendServiceNowMessage sends "UnableToAddParticipant"
   ↓
6. FAILED → ServiceNow case marked as failed
   ↓
7. NEW → Case remains "New" in database (manual review required)
```

### In-Progress/Retry Path

```text
1. NEW → Case created in ServiceNow
   ↓
2. NEW → ReceiveServiceNowMessage creates case record
   ↓
3. NEW → Participant queued for processing
   ↓
4. NEW → Temporary error (queue failure, PDS timeout)
   ↓
5. NEW → SendServiceNowMessage sends "AddRequestInProgress"
   ↓
6. IN_PROGRESS → ServiceNow case updated
   ↓
7. NEW → Case can be retried or investigated
```

---

## Configuration Summary

### ServiceNowMessageHandler

```json
{
  "ServiceNowParticipantManagementTopic": "servicenow-participant-topic",
  "ServiceNowApiBaseUrl": "https://servicenow-instance.service-now.com",
  "ServiceNowApiUsername": "api-user",
  "ServiceNowApiPassword": "api-password",
  "ServiceNowCaseDataServiceURL": "https://servicenow-case-service-url"
}
```

### ServiceNowCohortLookup

```json
{
  "CohortDistributionDataServiceURL": "https://cohort-distribution-url",
  "ServiceNowCaseDataServiceURL": "https://servicenow-case-service-url"
}
```

---

## Database Schema Reference

### ServicenowCase Table

| Field | Type | Description |
|-------|------|-------------|
| Id | GUID | Primary key |
| ServicenowId | string | ServiceNow case number |
| NhsNumber | long? | Participant NHS number |
| Status | enum | Case status (New, Complete) |
| RecordInsertDatetime | datetime | When case was created |
| RecordUpdateDatetime | datetime? | When case was last updated |

---

## Integration Patterns

### ServiceNow API Authentication

- Uses OAuth or basic authentication
- Credentials configured in `ServiceNowMessageHandlerConfig`
- Bearer token refresh handled by `ServiceNowClient`

### Message Type Usage

**When to use "Success":**

- Participant successfully added to cohort distribution
- All validations passed
- Ready for screening

**When to use "UnableToAddParticipant":**

- PDS returns 404 (participant not found)
- Data doesn't match between ServiceNow and PDS
- Participant is blocked
- NHS number superseded

**When to use "AddRequestInProgress":**

- Queue send failures
- PDS API errors (non-404)
- Database timeouts
- Temporary system issues

---

## Error Scenarios & Responses

### ReceiveServiceNowMessage Responses

| Scenario | Response Code | Action |
|----------|--------------|---------|
| Valid request | 202 Accepted | Case created, participant queued |
| Missing case number | 400 Bad Request | Fix ServiceNow payload |
| Invalid NHS number | 400 Bad Request | Verify NHS number format |
| Missing required fields | 400 Bad Request | Complete ServiceNow form |
| Database save fails | 500 Internal Server Error | Check database connectivity |
| Queue send fails | 500 Internal Server Error | Check Service Bus connectivity |

### SendServiceNowMessage Responses

| Scenario | Response Code | Action |
|----------|--------------|---------|
| Valid message sent | 200 OK | ServiceNow updated |
| Invalid request body | 400 Bad Request | Fix message type |
| ServiceNow API fails | 500 Internal Server Error | Check ServiceNow connectivity |
| Invalid case number | 500 Internal Server Error | Verify case exists in ServiceNow |

---

## Notes

### ServiceNow Case Statuses

- **New:** Case created, participant not yet processed
- **Complete:** Participant successfully added to cohort distribution
- Cases can remain "New" if processing failed (requires manual review)

### ScreeningId Hardcoding

- Currently hardcoded to `ScreeningId = 1` (Breast Screening)
- All ServiceNow participants assigned to Breast Screening
- Future enhancement: Support multiple screening services

### Daily Reconciliation Timing

- Runs at midnight UTC
- Matches yesterday's participants
- One-day processing lag expected
- Cases processed same day may not match until next run

### NHS Number Matching

- ServiceNowCohortLookup matches solely on NHS number
- Assumes NHS number uniqueness
- No additional validation performed during lookup

### Case Completion

- Cases marked "Complete" are audited but not reprocessed
- "New" cases remain eligible for daily reconciliation
- Failed cases (from ServiceNow perspective) may remain "New" in database
