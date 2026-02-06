# ScreeningDataServices API Documentation

This documentation covers multiple Azure Functions services that provide CRUD (Create, Read, Update, Delete) data access endpoints for various screening-related data entities.

---

## Overview

The screeningDataServices directory contains multiple data service endpoints that follow a consistent pattern:

- **Generic CRUD Services:** Provide standard database operations (GET, POST, PUT, DELETE) for specific data entities
- **Custom Services:** Provide specialized query and update operations with business logic

All data services are RESTful HTTP endpoints that support OData-style queries and standard CRUD operations.

## Service Endpoints covered in this documentation

### BsSelectRequestAuditDataService

- Manages audit records for BS SELECT requests.
- **Function App URL:** <https://env-uks-bs-request-audit-data-service.azurewebsites.net>

### CohortDistributionDataService

- Manages cohort distribution participant records.
- **Function App URL:** <https://env-uks-cohort-distribution-data-service.azurewebsites.net>

### ExceptionManagementDataService

- Manages system and validation exceptions.
- **Function App URL:** <https://env-uks-exception-management-data-service.azurewebsites.net>

### GeneCodeLkpDataService

- Manages gene code lookup data for genetic screening.
- **Function App URL:** <https://env-uks-gene-code-lkp-data-service.azurewebsites.net>

### HigherRiskReferralReasonLkpDataService

- Manages higher risk referral reason lookup data.
- **Function App URL:** <https://env-uks-higher-risk-referral-reason-lkp-data-service.azurewebsites.net>

### NemsSubscriptionDataService

- Manages National Event Management Service subscription records.
- **Function App URL:** <https://env-uks-nems-subscription-data-service.azurewebsites.net>

### ParticipantDemographicDataService

- Manages participant demographic data.
- **Function App URL:** <https://env-uks-participant-demographic-data-service.azurewebsites.net>

### ParticipantManagementDataService

- Manages participant management records.
- **Function App URL:** <https://env-uks-participant-management-data-service.azurewebsites.net>

### ReferenceDataService

- Provides access to various reference data tables.
- **Function App URL:** <https://env-uks-reference-data-service.azurewebsites.net>

### ScreeningLkpDataService

- Manages screening service lookup data.
- **Function App URL:** <https://env-uks-screening-lkp-data-service.azurewebsites.net>

### ServiceNowCasesDataService

- Manages ServiceNow case tracking records.
- **Function App URL:** <https://env-uks-servicenow-cases-data-service.azurewebsites.net>

---

## Generic CRUD Data Services

### Pattern

All generic data services follow the same implementation pattern using `IRequestHandler` to provide consistent CRUD operations.

#### **ALL Methods** `/api/{ServiceName}/{*key}`

**Purpose:** Provides full CRUD operations for a specific data entity

**HTTP Methods:** GET, POST, PUT, DELETE

**Route Parameters:**

- `key` (optional, string) - Entity identifier, filter expression, or OData query

**Authorization:** Anonymous

### Supported Operations

#### GET Operations

```bash
# Get all records
GET /api/{ServiceName}

# Get single record by ID
GET /api/{ServiceName}/123

# Get with OData filter
GET /api/{ServiceName}?$filter=FieldName eq 'value'

# Get with OData select
GET /api/{ServiceName}?$select=Field1,Field2

# Get with OData orderby
GET /api/{ServiceName}?$orderby=FieldName desc

# Get with OData top and skip (pagination)
GET /api/{ServiceName}?$top=10&$skip=20

# Combined OData query
GET /api/{ServiceName}?$filter=Status eq 1&$orderby=Date desc&$top=50
```

#### POST Operations

```bash
# Create new record
POST /api/{ServiceName}
Content-Type: application/json

{
  "field1": "value1",
  "field2": "value2"
}
```

#### PUT Operations

```bash
# Update existing record
PUT /api/{ServiceName}/123
Content-Type: application/json

{
  "id": 123,
  "field1": "updated value",
  "field2": "updated value"
}
```

#### DELETE Operations

```bash
# Delete record by ID
DELETE /api/{ServiceName}/123
```

**Response Codes:**

- `200 OK` - Successful operation, returns data
- `201 Created` - Record created successfully
- `204 No Content` - Successful operation with no return data
- `400 Bad Request` - Invalid request or validation error
- `404 Not Found` - Record not found
- `500 Internal Server Error` - Server error

---

## Available Data Services

### 1. CohortDistributionDataService

**Endpoint:** `/api/CohortDistributionDataService/{*key}`

**Entity:** `CohortDistribution`

**Purpose:** Manages cohort distribution participant records

**Key Fields:**

- `CohortDistributionId` - Primary key
- `NHSNumber` - Participant NHS number
- `ScreeningId` - Screening service ID
- `RecordInsertDateTime` - When record was created
- `RecordUpdateDateTime` - When record was last updated
- `Extracted` - Extraction status flag

**Common Queries:**

```bash
# Get by NHS number
GET /api/CohortDistributionDataService?$filter=NHSNumber eq 1234567890

# Get unextracted records
GET /api/CohortDistributionDataService?$filter=Extracted eq '0'

# Get by screening ID
GET /api/CohortDistributionDataService?$filter=ScreeningId eq 1
```

**Location:** CohortDistributionDataService.cs:25

---

## 2. ParticipantManagementDataService

**Endpoint:** `/api/ParticipantManagementDataService/{*key}`

**Entity:** `ParticipantManagement`

**Purpose:** Manages participant management records

**Key Fields:**

- `ParticipantId` - Primary key
- `NHSNumber` - Participant NHS number
- `ScreeningId` - Screening service ID
- `RecordType` - Record type (New, Amended, Removed)
- `BlockedFlag` - Whether participant is blocked
- `EligibilityFlag` - Eligibility status
- `ReferralFlag` - Referral status
- `IsHigherRisk` - VHR flag

**Common Queries:**

```bash
# Get by NHS number and screening ID
GET /api/ParticipantManagementDataService?$filter=NHSNumber eq 1234567890 and ScreeningId eq 1

# Get blocked participants
GET /api/ParticipantManagementDataService?$filter=BlockedFlag eq 1

# Get VHR participants
GET /api/ParticipantManagementDataService?$filter=IsHigherRisk eq 1
```

**Location:** ParticipantManagementDataService.cs:25

---

### 3. ParticipantDemographicDataService

**Endpoint:** `/api/ParticipantDemographicDataService/{*key}`

**Entity:** `ParticipantDemographic`

**Purpose:** Manages participant demographic data

**Key Fields:**

- `ParticipantId` - Primary key
- `NhsNumber` - Participant NHS number
- `FirstName` - First name
- `FamilyName` - Family name
- `DateOfBirth` - Date of birth
- `Postcode` - Postcode
- `PrimaryCareProvider` - GP practice code

**Common Queries:**

```bash
# Get by NHS number
GET /api/ParticipantDemographicDataService?$filter=NhsNumber eq 1234567890

# Get by postcode prefix
GET /api/ParticipantDemographicDataService?$filter=startswith(Postcode,'SW1')
```

**Location:** ParticipantDemographicDataService.cs:25

---

### 4. ExceptionManagementDataService

**Endpoint:** `/api/ExceptionManagementDataService/{*key}`

**Entity:** `ExceptionManagement`

**Purpose:** Manages system and validation exceptions

**Key Fields:**

- `ExceptionId` - Primary key
- `NhsNumber` - Participant NHS number
- `RuleId` - Rule identifier
- `RuleDescription` - Description of failed rule
- `Category` - Exception category
- `Fatal` - Whether exception is fatal
- `ServiceNowId` - ServiceNow case number

**Common Queries:**

```bash
# Get by NHS number
GET /api/ExceptionManagementDataService?$filter=NhsNumber eq '1234567890'

# Get fatal exceptions
GET /api/ExceptionManagementDataService?$filter=Fatal eq 1

# Get by category
GET /api/ExceptionManagementDataService?$filter=Category eq 1
```

**Location:** ExceptionManagementDataService.cs:25

---

## 5. NemsSubscriptionDataService

**Endpoint:** `/api/NemsSubscriptionDataService/{*key}`

**Entity:** `NemsSubscription`

**Purpose:** Manages NEMS subscription records

**Key Fields:**

- `Id` - Primary key
- `SubscriptionId` - NEMS/MESH subscription ID
- `NhsNumber` - Participant NHS number
- `SubscriptionSource` - Source (NEMS, MESH)
- `RecordInsertDateTime` - When subscription created

**Common Queries:**

```bash
# Get by NHS number
GET /api/NemsSubscriptionDataService?$filter=NhsNumber eq 1234567890

# Get by subscription source
GET /api/NemsSubscriptionDataService?$filter=SubscriptionSource eq 'NEMS'
```

**Location:** NemsSubscriptionDataService.cs:25

---

### 6. BsSelectRequestAuditDataService

**Endpoint:** `/api/BsSelectRequestAuditDataService/{*key}`

**Entity:** `BsSelectRequestAudit`

**Purpose:** Manages BS SELECT request audit records

**Key Fields:**

- `RequestId` - Request identifier (GUID)
- `RequestDateTime` - When request was made
- `StatusCode` - HTTP status code of response
- `RecordCount` - Number of records returned

**Common Queries:**

```bash
# Get by date
GET /api/BsSelectRequestAuditDataService?$filter=RequestDateTime ge 2025-01-01

# Get by status code
GET /api/BsSelectRequestAuditDataService?$filter=StatusCode eq 200
```

**Location:** BsSelectRequestAuditDataService.cs:25

---

### 7. ScreeningLkpDataService

**Endpoint:** `/api/ScreeningLkpDataService/{*key}`

**Entity:** `ScreeningLkp`

**Purpose:** Manages screening service lookup data

**Key Fields:**

- `ScreeningId` - Primary key
- `ScreeningName` - Name of screening service
- `ScreeningWorkflowId` - Workflow identifier
- `ScreeningAcronym` - Service acronym (e.g., "BSS")

**Common Queries:**

```bash
# Get by screening ID
GET /api/ScreeningLkpDataService/1

# Get by acronym
GET /api/ScreeningLkpDataService?$filter=ScreeningAcronym eq 'BSS'
```

**Location:** ScreeningLkpDataService.cs:25

---

### 8. GeneCodeLkpDataService

**Endpoint:** `/api/GeneCodeLkpDataService/{*key}`

**Entity:** `GeneCodeLkp`

**Purpose:** Manages gene code lookup data for genetic screening

**Key Fields:**

- `GeneCode` - Gene code identifier
- `Description` - Gene description

**Common Queries:**

```bash
# Get all gene codes
GET /api/GeneCodeLkpDataService

# Get specific gene code
GET /api/GeneCodeLkpDataService?$filter=GeneCode eq 'BRCA1'
```

**Location:** GeneCodeLkpDataService.cs:25

---

### 9. HigherRiskReferralReasonLkpDataService

**Endpoint:** `/api/HigherRiskReferralReasonLkpDataService/{*key}`

**Entity:** `HigherRiskReferralReasonLkp`

**Purpose:** Manages higher risk referral reason lookup data

**Key Fields:**

- `ReasonCode` - Reason code identifier
- `Description` - Reason description

**Common Queries:**

```bash
# Get all referral reasons
GET /api/HigherRiskReferralReasonLkpDataService

# Get specific reason
GET /api/HigherRiskReferralReasonLkpDataService/{reasonCode}
```

**Location:** HigherRiskReferralReasonLkpDataService.cs:25

---

### 10. ServiceNowCasesDataService

**Endpoint:** `/api/ServiceNowCasesDataService/{*key}`

**Entity:** `ServicenowCase`

**Purpose:** Manages ServiceNow case tracking records

**Key Fields:**

- `Id` - Primary key (GUID)
- `ServicenowId` - ServiceNow case number
- `NhsNumber` - Participant NHS number
- `Status` - Case status (New, Complete)
- `RecordInsertDatetime` - When case created
- `RecordUpdateDatetime` - When case updated

**Common Queries:**

```bash
# Get by NHS number
GET /api/ServiceNowCasesDataService?$filter=NhsNumber eq 1234567890

# Get new cases
GET /api/ServiceNowCasesDataService?$filter=Status eq 'New'

# Get by ServiceNow case number
GET /api/ServiceNowCasesDataService?$filter=ServicenowId eq 'CASE123456'
```

**Location:** ServiceNowCasesDataService.cs:25

---

## 11. ReferenceDataService

**Endpoint:** `/api/ReferenceDataService/{*key}`

**Entity:** Multiple reference data entities

**Purpose:** Provides access to various reference data tables

**Common Queries:**

```bash
# Query reference data
GET /api/ReferenceDataService
```

**Location:** ReferenceDataService.cs:25

---

## Custom Data Services

### GetValidationExceptions Service

**Endpoint Base:** `/api/GetValidationExceptions` and `/api/UpdateExceptionServiceNowId`

#### **GET** `/api/GetValidationExceptions`

**Purpose:** Retrieves validation exceptions with advanced filtering, pagination, and reporting capabilities

**HTTP Method:** GET

**Query Parameters:**

- `exceptionId` (int, optional) - Get specific exception by ID
- `page` (int, optional) - Page number for pagination
- `pageSize` (int, optional) - Number of records per page
- `exceptionStatus` (enum, optional) - Filter by status (All, Open, Closed)
- `sortOrder` (enum, optional) - Sort order (Ascending, Descending)
- `exceptionCategory` (enum, optional) - Filter by category (NBO, TransformExecuted, etc.)
- `reportDate` (datetime, optional) - Date for report generation
- `isReport` (bool, optional) - Whether to generate report format

**Query Modes:**

#### 1. Get Single Exception

```bash
GET /api/GetValidationExceptions?exceptionId=123
```

**Response (200 OK):**

```json
{
  "exceptionId": 123,
  "nhsNumber": "1234567890",
  "ruleId": 42,
  "ruleDescription": "Invalid postcode format",
  "category": 1,
  "fatal": 0,
  "serviceNowId": "CASE123456"
}
```

**Response (204 No Content):** Exception not found

---

#### 2. Get Filtered List

```bash
GET /api/GetValidationExceptions?exceptionStatus=Open&exceptionCategory=NBO&sortOrder=Descending&page=1&pageSize=50
```

**Response (200 OK):**

```json
{
  "currentPage": 1,
  "pageSize": 50,
  "totalCount": 250,
  "totalPages": 5,
  "hasPreviousPage": false,
  "hasNextPage": true,
  "data": [
    {
      "exceptionId": 456,
      "nhsNumber": "9876543210",
      "ruleId": 15,
      "ruleDescription": "Date of birth invalid"
    }
  ]
}
```

**Response Headers:**

- `X-Pagination` - Pagination metadata JSON
- `Link` - Navigation links (first, previous, next, last)

---

#### 3. Generate Report

```bash
GET /api/GetValidationExceptions?isReport=true&reportDate=2025-01-15&exceptionCategory=NBO
```

**Validation:**

- Report date cannot be in the future
- Returns 400 Bad Request if date is future

**Response (200 OK):** Paginated list of exceptions for report date

**Response (400 Bad Request):**

```text
Report date cannot be in the future.
```

---

**Response Codes:**

- `200 OK` - Exceptions retrieved successfully
- `204 No Content` - No exceptions found
- `400 Bad Request` - Invalid parameters (e.g., future report date)
- `500 Internal Server Error` - Server error

**Pagination:**

- Supports server-side pagination
- Navigation headers included in response
- OData-style pagination with $top and $skip

**Location:** GetValidationExceptions.cs:46

---

#### **PUT** `/api/UpdateExceptionServiceNowId`

**Purpose:** Updates the ServiceNow case number for a validation exception

**HTTP Method:** PUT

**Request Body:**

```json
{
  "exceptionId": 123,
  "serviceNowId": "CASE123456"
}
```

**Request Fields:**

- `exceptionId` (int, required) - Exception record ID
- `serviceNowId` (string, optional) - ServiceNow case number (can be null to clear)

**Response Codes:**

- `200 OK` - ServiceNowId updated successfully
- `400 Bad Request` - Invalid request (empty body, missing exceptionId)
- `404 Not Found` - Exception not found
- `500 Internal Server Error` - Server error

**Response Body (200 OK):**

```text
ServiceNowId updated successfully
```

**Response Body (400 Bad Request):**

```text
Request body cannot be empty.
```

or

```text
Invalid request. ExceptionId and ServiceNowId is required.
```

**Location:** GetValidationExceptions.cs:100

---

## Sample curl Commands

### Generic CRUD Operations

```bash
# GET all records
curl -X GET "https://env-uks-cohort-distribution-data-service.azurewebsites.net/api/CohortDistributionDataService" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# GET single record by ID
curl -X GET "https://env-uks-participant-management-data-service.azurewebsites.net/api/ParticipantManagementDataService/123" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# GET with OData filter
curl -X GET "https://env-uks-participant-demographic-data-service.azurewebsites.net/api/ParticipantDemographicDataService?\$filter=NhsNumber eq 1234567890" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# GET with pagination
curl -X GET "https://env-uks-exception-management-data-service.azurewebsites.net/api/ExceptionManagementDataService?\$top=50&\$skip=100" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# POST create new record
curl -X POST "https://env-uks-manage-caas-subscription.azurewebsites.net/api/NemsSubscriptionDataService" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "subscriptionId": "sub-123",
    "nhsNumber": 1234567890,
    "subscriptionSource": "NEMS"
  }'

# PUT update record
curl -X PUT "https://env-uks-participant-management-data-service.azurewebsites.net/api/ParticipantManagementDataService/123" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "participantId": 123,
    "blockedFlag": 1
  }'

# DELETE record
curl -X DELETE "https://env-uks-cohort-distribution-data-service.azurewebsites.net/api/CohortDistributionDataService/456" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"
```

### GetValidationExceptions

```bash
# Get single exception
curl -X GET "https://env-uks-get-validation-exceptions.azurewebsites.net/api/GetValidationExceptions?exceptionId=123" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Get filtered list with pagination
curl -X GET "https://env-uks-get-validation-exceptions.azurewebsites.net/api/GetValidationExceptions?exceptionStatus=Open&page=1&pageSize=50" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Generate report
curl -X GET "https://env-uks-get-validation-exceptions.azurewebsites.net/api/GetValidationExceptions?isReport=true&reportDate=2025-01-15" \
  -H "Ocp-Apim-Subscription-Key: your-key-here"

# Update ServiceNow ID
curl -X PUT "https://env-uks-get-validation-exceptions.azurewebsites.net/api/UpdateExceptionServiceNowId" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "exceptionId": 123,
    "serviceNowId": "CASE123456"
  }'

# Pretty-printed with headers
curl -i -X GET "https://env-uks-get-validation-exceptions.azurewebsites.net/api/GetValidationExceptions?page=1&pageSize=10" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" | jq .
```

---

## OData Query Support

All generic data services support OData query parameters:

### $filter (Filtering)

```bash
# Equal
$filter=FieldName eq 'value'
$filter=NumericField eq 123

# Not equal
$filter=FieldName ne 'value'

# Greater than / Less than
$filter=DateField gt 2025-01-01
$filter=NumericField lt 100

# And / Or
$filter=Field1 eq 'value' and Field2 eq 'value'
$filter=Field1 eq 'value1' or Field1 eq 'value2'

# String functions
$filter=startswith(FieldName,'prefix')
$filter=endswith(FieldName,'suffix')
$filter=contains(FieldName,'substring')
```

### $select (Field Selection)

```bash
# Select specific fields
$select=Field1,Field2,Field3
```

### $orderby (Sorting)

```bash
# Ascending (default)
$orderby=FieldName

# Descending
$orderby=FieldName desc

# Multiple fields
$orderby=Field1 desc,Field2 asc
```

### $top and $skip (Pagination)

```bash
# Take first 10 records
$top=10

# Skip first 20 records
$skip=20

# Combined (page 3 with page size 10)
$top=10&$skip=20
```

### $count (Count)

```bash
# Include total count in response
$count=true
```

---

## Notes

### OData Support

- Partial OData v4 support
- Not all OData functions may be supported
- Complex queries may have limitations
- Test queries before production use

### Pagination

- Server-side pagination recommended for large datasets
- Navigation headers provided in responses
- Page numbers are 1-based
- Default page sizes vary by service

### Data Consistency

- All data services use Entity Framework Core
- Database transactions ensure consistency
- Optimistic concurrency control used where applicable
- Timestamp fields track record changes

### Security

- All endpoints use Anonymous authorization at function level
- API Management handles authentication/authorization
- Subscription keys required for external access
- Rate limiting applied at API Management level
