# ExceptionHandling API Documentation

This documentation covers two Azure Functions services that handle exception and error management for the cohort management system.

---

## 1. CreateException Service

### CreateException Overview

Service that creates and persists validation exceptions, system errors, and data quality issues. Supports both direct HTTP calls and Service Bus message processing.

**Function App URL:** <https://env-uks-create-exception.azurewebsites.net>

### CreateException Endpoints

#### **GET/POST** `/api/CreateException`

**Purpose:** Creates a new exception record in the exception management system

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "dateCreated": "2025-01-15T10:30:00Z",
  "ruleId": 42,
  "ruleDescription": "Invalid postcode format",
  "category": 1,
  "fatal": 0,
  "screeningName": "Breast Screening",
  "fileName": "batch_001.parquet",
  "exceptionDate": "2025-01-15T10:30:00Z"
}
```

**Request Fields:**

- `nhsNumber` (string) - Participant NHS number
- `dateCreated` (datetime) - When the exception was detected
- `ruleId` (int) - Validation rule identifier
- `ruleDescription` (string) - Description of the validation rule that failed
- `category` (int) - Exception category code
- `fatal` (int) - Whether exception is fatal (0 = non-fatal, 1 = fatal)
- `screeningName` (string) - Name of screening service
- `fileName` (string) - Source file name or batch identifier
- `exceptionDate` (datetime) - Date of the exception

**Response Codes:**

- `200 OK` - Exception created successfully
- `400 Bad Request` - Invalid request body or deserialization error
- `500 Internal Server Error` - Failed to create exception in database

**Response Body (200 OK):**

```text
(Empty response body)
```

**Response Body (500 Internal Server Error):**

```text
could not create exception please see database for more details
```

**Processing:**

1. Deserializes request body to `ValidationException` object
2. Calls database layer to create exception record
3. Returns success or error status

**Location:** CreateException.cs:30

---

### **Service Bus Trigger** `RunCreateException`

**Type:** Service Bus triggered function (not an HTTP endpoint)

**Purpose:** Processes exception creation messages from Service Bus topic

**Trigger Configuration:**

- **Topic:** `CreateExceptionTopic` (configured via environment variable)
- **Subscription:** `ExceptionSubscription` (configured via environment variable)
- **Connection:** `ServiceBusConnectionString`
- **Auto Complete:** False (manual message completion)

**Message Format:**

```json
{
  "nhsNumber": "1234567890",
  "dateCreated": "2025-01-15T10:30:00Z",
  "ruleId": 42,
  "ruleDescription": "Invalid postcode format",
  "category": 1,
  "fatal": 0,
  "screeningName": "Breast Screening",
  "fileName": "batch_001.parquet",
  "exceptionDate": "2025-01-15T10:30:00Z"
}
```

**Processing Flow:**

1. Receives message from Service Bus topic
2. Deserializes message body to `ValidationException`
3. Attempts to create exception record in database
4. On success: Completes message (removes from queue)
5. On failure: Dead-letters message for manual review

**Error Handling:**

- Database failures: Message sent to dead letter queue
- Deserialization errors: Message sent to dead letter queue
- All errors logged for debugging

**Message Actions:**

- **Complete:** Successful processing, message removed from queue
- **Dead Letter:** Failed processing, message moved to dead letter queue for investigation

**Location:** CreateException.cs:55

---

## 2. UpdateException Service

### UpdateException Overview

Service that updates existing exception records, primarily for adding ServiceNow case numbers to exceptions.

**Function App URL:** <https://env-uks-update-exception.azurewebsites.net>

### UpdateException Endpoints

#### **PUT** `/api/UpdateException`

**Purpose:** Updates an exception record with ServiceNow case information

**HTTP Method:** PUT

**Request Body:**

```json
{
  "exceptionId": "123",
  "serviceNowNumber": "SNOW123456"
}
```

**Request Fields:**

- `exceptionId` (string) - Exception record ID (must be valid integer)
- `serviceNowNumber` (string, nullable) - ServiceNow case number

**Response Codes:**

- `200 OK` - Exception updated successfully
- `204 No Content` - Empty request body OR exception not found
- `400 Bad Request` - Invalid exception ID (not a valid integer or zero)
- `500 Internal Server Error` - Database update failed

**Response Body (200 OK):**

```text
(Empty response body)
```

**Processing Flow:**

1. Validates request body is not empty
2. Deserializes to `UpdateExceptionRequest`
3. Validates exception ID is valid integer (not zero)
4. Queries database for exception record by ID
5. If not found, returns 204 No Content
6. Updates exception record with ServiceNow number
7. Sets `RecordUpdatedDate` to current UTC time
8. Persists changes to database
9. Returns success or error status

**Update Fields:**

- `ServiceNowId` - Set to provided ServiceNow number (can be null)
- `RecordUpdatedDate` - Set to current UTC timestamp

**Validation Rules:**

- Exception ID must be parseable to integer
- Exception ID must not be zero
- Exception must exist in database

**Location:** UpdateException.cs:30

---

## Architecture & Data Flow

### Direct Exception Creation Flow

```text
1. System Component detects validation error/exception
   ↓
2. HTTP POST to CreateException endpoint
   ↓
3. Deserialize ValidationException
   ↓
4. Create exception record in database
   ↓
5. Return success/failure
```

### Service Bus Exception Creation Flow

```text
1. System Component publishes exception to Service Bus topic
   ↓
2. CreateException Service Bus trigger activates
   ↓
3. Deserialize message to ValidationException
   ↓
4. Create exception record in database
   ├─ Success → Complete message (remove from queue)
   └─ Failure → Dead letter message (move to DLQ)
```

### Exception Update Flow

```text
1. ServiceNow integration creates case
   ↓
2. HTTP PUT to UpdateException endpoint
   ↓
3. Validate exception ID
   ↓
4. Retrieve exception from database
   ↓
5. Update with ServiceNow number
   ↓
6. Set RecordUpdatedDate
   ↓
7. Persist to database
   ↓
8. Return success/failure
```

---

## Sample curl Commands

### CreateException Commands

```bash
# Create exception via HTTP
curl -X POST "https://env-uks-create-exception.azurewebsites.net/api/CreateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "dateCreated": "2025-01-15T10:30:00Z",
    "ruleId": 42,
    "ruleDescription": "Invalid postcode format",
    "category": 1,
    "fatal": 0,
    "screeningName": "Breast Screening",
    "fileName": "batch_001.parquet",
    "exceptionDate": "2025-01-15T10:30:00Z"
  }'

# Create fatal exception
curl -X POST "https://env-uks-create-exception.azurewebsites.net/api/CreateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "9876543210",
    "dateCreated": "2025-01-15T11:00:00Z",
    "ruleId": 1,
    "ruleDescription": "NHS number failed validation",
    "category": 1,
    "fatal": 1,
    "screeningName": "Breast Screening",
    "fileName": "batch_002.parquet",
    "exceptionDate": "2025-01-15T11:00:00Z"
  }'

# Using GET method (if supported)
curl -X GET "https://env-uks-create-exception.azurewebsites.net/api/CreateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @exception.json

# Include response headers
curl -i -X POST "https://env-uks-create-exception.azurewebsites.net/api/CreateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @exception.json
```

### UpdateException Commands

```bash
# Update exception with ServiceNow number
curl -X PUT "https://env-uks-update-exception.azurewebsites.net/api/UpdateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "exceptionId": "123",
    "serviceNowNumber": "SNOW123456"
  }'

# Update with null ServiceNow number (clear field)
curl -X PUT "https://env-uks-update-exception.azurewebsites.net/api/UpdateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "exceptionId": "123",
    "serviceNowNumber": null
  }'

# Include response headers to see status
curl -i -X PUT "https://env-uks-create-exception.azurewebsites.net/api/UpdateException" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "exceptionId": "456",
    "serviceNowNumber": "SNOW789012"
  }'
```

---

## Exception Categories

The `category` field in exceptions represents different types of validation or system errors:

### Common Categories

- **1** - Validation Exception
- **2** - System Exception
- **3** - Transform Executed (audit of successful transformation)
- **4** - Business Rule Violation
- **5** - Data Quality Issue

### Fatal Flag

- **0** - Non-fatal exception (warning, can be reviewed later)
- **1** - Fatal exception (blocks processing, requires immediate attention)

---

## Configuration Summary

### CreateException

```json
{
  "CreateExceptionTopic": "exception-topic",
  "ExceptionSubscription": "exception-subscription",
  "ServiceBusConnectionString": "Endpoint=sb://...",
  "DatabaseConnection": "connection-string"
}
```

### UpdateException

```json
{
  "ExceptionManagementDataServiceURL": "https://exception-service-url",
  "DatabaseConnection": "connection-string"
}
```

---

## Integration Patterns

### When to Use HTTP vs Service Bus

**Use HTTP CreateException when:**

- Immediate response required
- Synchronous validation workflow
- Small number of exceptions
- Interactive user interface

**Use Service Bus RunCreateException when:**

- Batch processing with many exceptions
- Asynchronous workflow
- Decoupled system components
- Guaranteed delivery required
- Processing can be retried

### Service Bus Message Patterns

**Publishing to Exception Topic:**

```csharp
var exception = new ValidationException
{
    NhsNumber = "1234567890",
    RuleId = 42,
    RuleDescription = "Invalid postcode",
    Category = 1,
    Fatal = 0,
    ScreeningName = "Breast Screening",
    FileName = "batch_001.parquet"
};

var message = new ServiceBusMessage(JsonSerializer.Serialize(exception));
await sender.SendMessageAsync(message);
```

**Dead Letter Queue Handling:**

- Messages that fail processing are moved to dead letter queue
- DLQ messages should be monitored and investigated
- Common reasons: database connectivity, invalid data format
- Manual intervention may be required to resolve and reprocess

---

## Error Scenarios & Responses

### CreateException Responses

| Scenario | Response Code | Response Body | Action |
|----------|--------------|---------------|---------|
| Valid exception | 200 OK | (empty) | Exception created |
| Invalid JSON | 400 Bad Request | Error message | Fix JSON format |
| Database error | 500 Internal Server Error | "could not create exception..." | Check database connection |
| Missing fields | 400 Bad Request | Error message | Provide all required fields |

### UpdateException Responses

| Scenario | Response Code | Response Body | Action |
|----------|--------------|---------------|---------|
| Valid update | 200 OK | (empty) | Exception updated |
| Empty body | 204 No Content | (empty) | Provide request body |
| Exception not found | 204 No Content | (empty) | Check exception ID exists |
| Invalid exception ID | 400 Bad Request | (empty) | Provide valid integer ID |
| Database error | 500 Internal Server Error | (empty) | Check database connection |

---

## Best Practices

### Exception Creation

1. **Always provide accurate rule IDs** - Enables tracking which validation rules are failing most
2. **Use descriptive rule descriptions** - Helps troubleshooting and exception resolution
3. **Set fatal flag appropriately** - Fatal exceptions block processing, use sparingly
4. **Include source file name** - Essential for batch processing audit trail
5. **Use correct category codes** - Enables proper exception classification and reporting

### Exception Updates

1. **Verify exception exists before updating** - Handle 204 No Content appropriately
2. **Track ServiceNow case lifecycle** - Update exceptions when cases are created/closed
3. **Use transaction safety** - RecordUpdatedDate ensures concurrent update detection
4. **Monitor failed updates** - 500 errors may indicate database issues

### Service Bus Processing

1. **Monitor dead letter queue daily** - Failed messages require investigation
2. **Set appropriate message TTL** - Prevent old exceptions from being processed
3. **Use correlation IDs** - Track messages through the system
4. **Implement retry policies** - Transient failures should be retried automatically
5. **Alert on DLQ depth** - High DLQ count indicates systemic issues

---

## Database Schema Reference

### Exception Management Table Fields

| Field | Type | Description |
|-------|------|-------------|
| ExceptionId | int | Primary key, auto-incrementing |
| NhsNumber | string | Participant NHS number |
| DateCreated | datetime | Exception creation timestamp |
| RuleId | int | Validation rule identifier |
| RuleDescription | string | Description of failed rule |
| Category | int | Exception category code |
| Fatal | int | Fatal flag (0 = non-fatal, 1 = fatal) |
| ScreeningName | string | Screening service name |
| FileName | string | Source file or batch identifier |
| ExceptionDate | datetime | Date of exception occurrence |
| ServiceNowId | string | ServiceNow case number (nullable) |
| RecordUpdatedDate | datetime | Last update timestamp |

---

## Notes

### Exception Lifecycle

1. **Creation** - Exception detected and logged via CreateException
2. **Review** - Exception appears in exception management UI
3. **ServiceNow** - Case created in ServiceNow for resolution
4. **Update** - Exception linked to ServiceNow case via UpdateException
5. **Resolution** - Exception resolved in ServiceNow
6. **Closure** - Exception record updated with resolution details

### Automatic vs Manual Exception Handling

- **Automatic:** System exceptions, validation failures during batch processing
- **Manual:** User-reported issues, data quality problems requiring investigation

### Integration Points

- **Validation Services:** Create exceptions when validation rules fail
- **Transform Services:** Create transform executed audit records
- **ServiceNow Integration:** Updates exceptions with case numbers
- **Exception Management UI:** Displays and manages exception records
