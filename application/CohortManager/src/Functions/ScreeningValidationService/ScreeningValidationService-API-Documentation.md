# ScreeningValidationService API Documentation

This documentation covers three Azure Functions services that validate participant screening data using static rules, database lookups, and exception management.

---

## 1. StaticValidation Service

### StaticValidation Overview

Service that validates participant records against static business rules defined in JSON rule files.

**Function App URL:**  <https://env-uks-static-validation.azurewebsites.net>

### StaticValidation Endpoints

#### **GET/POST** `/api/StaticValidation`

**Purpose:** Validates participant records against static business rules

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "Participant": {
    "ScreeningName": "Breast Screening",
    "RecordType": "Add",
    "ReferralFlag": "false",
    "NhsNumber": "1234567890",
    "FirstName": "Jane",
    "FamilyName": "Doe",
    "Postcode": "SW1A 1AA",
    "DateOfBirth": "1980-01-15"
  }
}
```

**Request Fields:**

- `Participant` (object, required) - Participant record to validate
  - `ScreeningName` (string, required) - Name of screening service (e.g., "Breast Screening")
  - `RecordType` (string, required) - Type of record (Add, Amend, Remove, etc.)
  - `ReferralFlag` (string, required) - "true" for referred participants, "false" for routine
  - `NhsNumber` (string) - Participant NHS number
  - Other participant fields as required by validation rules

**Validation Logic:**

1. Loads rules from `{ScreeningName}_staticRules.json` file
   - Example: `Breast_Screening_staticRules.json`
2. Executes "Common" workflow rules for all non-removed records
3. If routine participant (`ReferralFlag == "false"`), executes "Routine_Common" rules
4. If `RecordType` matches a registered workflow, executes those specific rules

**Response Codes:**

- `200 OK` - Validation errors found, returns array of validation errors
- `204 No Content` - Validation passed, no errors
- `500 Internal Server Error` - Exception occurred

**Response Body (200 OK - Validation Errors):**

```json
[
  {
    "RuleName": "49.InterpreterCheck.NBO.NonFatal",
    "RuleDescription": "Interpreter required does not contain a valid value",
    "ExceptionMessage": "Error while executing rule : 49.InterpreterCheck.NBO.NonFatal - Value cannot be null. (Parameter 's')"
  },
  {
    "RuleName": "3.PrimaryCareProviderAndReasonForRemoval.NBO.NonFatal",
    "RuleDescription": "GP practice code and Reason for Removal fields contain incompatible values",
    "ExceptionMessage": ""
  },
  {
    "RuleName": "8.RecordType.CaaS.NonFatal",
    "RuleDescription": "Incorrect record type",
    "ExceptionMessage": ""
  }
]
```

**Response Body (204 No Content):**

```text
(Empty response body - validation passed)
```

**Rule Files:**

- Located in function app directory
- Named pattern: `{ScreeningName}_staticRules.json`
- Contains workflow definitions with validation rules
- Rules use RulesEngine format

**Location:** StaticValidation.cs:36

---

## 2. LookupValidation Service

### LookupValidation Overview

Service that validates participant records against database lookup data and reference tables.

**Function App URL:**  <https://env-uks-lookup-validation.azurewebsites.net>

### LookupValidation Endpoints

#### **GET/POST** `/api/LookupValidation`

**Purpose:** Validates participant records using database lookups and reference data

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "NewParticipant": {
    "ScreeningName": "Breast Screening",
    "RecordType": "Add",
    "NhsNumber": "1234567890",
    "FirstName": "Jane",
    "FamilyName": "Doe",
    "PrimaryCareProvider": "GP001",
    "GeneCode": "BRCA1",
    "ReasonForRemoval": "RDR",
    "HigherRiskReferralReasonCode": "VHR001"
  },
  "ExistingParticipant": {
    "NhsNumber": "1234567890",
    "ScreeningId": 1,
    "RecordType": "Existing"
  },
  "FileName": "batch_001.parquet"
}
```

**Request Fields:**

- `NewParticipant` (object, required) - New/updated participant data
  - `ScreeningName` (string, required) - Screening service name
  - `RecordType` (string, required) - Record action type (Add, Amend, Remove)
  - `NhsNumber` (string) - Participant NHS number
  - `PrimaryCareProvider` (string, optional) - GP practice code
  - `GeneCode` (string, optional) - Gene code for genetic screening
  - `ReasonForRemoval` (string, optional) - Removal reason code
  - `HigherRiskReferralReasonCode` (string, optional) - VHR reason code
- `ExistingParticipant` (object, optional) - Existing participant record for comparison
- `FileName` (string, optional) - Source file name for audit trail

**Validation Logic:**

1. Loads lookup rules from `{ScreeningName}_lookupRules.json` file
   - Example: `Breast_Screening_lookupRules.json`
2. Provides database lookup facade (`dbLookup`) to rules
3. Executes "Common" workflow rules for all non-removed records
4. If `RecordType` matches a registered workflow, executes those specific rules
5. Rules can perform database lookups:
   - GP practice code validation
   - Gene code validation
   - Reason for removal validation
   - Higher risk referral reason validation

**Database Lookups Available:**

- `dbLookup.ValidateGpCode(gpCode)` - Validates GP practice exists
- `dbLookup.ValidateGeneCode(geneCode)` - Validates gene code exists
- `dbLookup.ValidateRemovalReason(reasonCode)` - Validates removal reason
- `dbLookup.ValidateHigherRiskReason(reasonCode)` - Validates VHR reason

**Response Codes:**

- `200 OK` - Validation errors found, returns array of validation errors
- `204 No Content` - Validation passed, no errors
- `400 Bad Request` - Invalid request body or deserialization error
- `500 Internal Server Error` - Exception occurred

**Response Body (200 OK - Validation Errors):**

```json
[
  {
    "ruleName": "Rule_50_GPCodeExists",
    "errorMessage": "GP practice code does not exist in database",
    "ruleId": 50
  },
  {
    "ruleName": "Rule_51_GeneCodeValid",
    "errorMessage": "Gene code is not recognized",
    "ruleId": 51
  }
]
```

**Response Body (204 No Content):**

```text
(Empty response body - validation passed)
```

**Rule Files:**

- Located in function app directory
- Named pattern: `{ScreeningName}_lookupRules.json`
- Contains workflow definitions with lookup validation rules
- Rules can access database via `dbLookup` parameter

**Location:** LookupValidation.cs:38

---

## 3. RemoveValidationException Service

### RemoveValidationException Overview

Service that removes old validation exceptions for participants.

**Function App URL:**  <https://env-uks-remove-validation-exception-data.azurewebsites.net>

### RemoveValidationException Endpoints

#### **GET/POST** `/api/RemoveValidationExceptionData`

**Purpose:** Removes the most recent validation exception for a participant

**HTTP Methods:** GET, POST

**Request Body:**

```json
{
  "nhsNumber": "1234567890",
  "screeningName": "Breast Screening"
}
```

**Request Fields:**

- `nhsNumber` (string, required) - Participant NHS number
- `screeningName` (string, required) - Screening service name

**Processing:**

1. Queries validation exception table for participant
2. Filters by NHS number and screening name
3. Removes the most recent exception record
4. Returns success or failure status

**Response Codes:**

- `201 Created` - Exception removed successfully
- `200 OK` - No exception found to remove (not an error)
- `500 Internal Server Error` - Exception occurred during removal

**Response Body (201 Created):**

```text
(Empty response body)
```

**Response Body (200 OK):**

```text
(Empty response body)
```

**Use Cases:**

- Clearing resolved exceptions
- Manual exception management
- Re-validation after data fixes

**Location:** RemoveValidationExceptionData.cs:29

---

### Validation Workflow Integration

#### Complete Validation Flow

```text

1. Participant data received
   ↓
2. Static Validation (StaticValidation)
   ├─ Load static rules for screening service
   ├─ Execute Common workflow rules
   ├─ Execute Routine_Common rules (if routine)
   └─ Execute RecordType-specific rules
   ↓
3. If static validation passes → Continue
   If fails → Return validation errors
   ↓
4. Lookup Validation (LookupValidation)
   ├─ Load lookup rules for screening service
   ├─ Provide database lookup facade
   ├─ Execute Common workflow rules
   └─ Execute RecordType-specific rules
   ↓
5. If lookup validation passes → Continue
   If fails → Return validation errors
   ↓
6. Data Transformation (TransformDataService)
   ↓
7. Participant processed successfully
```

### Exception Handling Flow

```text
1. Validation fails
   ↓
2. Exception created in database
   ↓
3. Participant flagged with exception
   ↓
4. Exception reviewed and resolved
   ↓
5. RemoveValidationException called
   ↓
6. Exception removed from database
   ↓
7. Participant can be re-validated
```

---

## Sample curl Commands

### StaticValidation

```bash
# Validate routine participant (Add)
curl -X POST "https://env-uks-static-validation.azurewebsites.net/api/StaticValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "Participant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Add",
      "ReferralFlag": "false",
      "NhsNumber": "1234567890",
      "FirstName": "Jane",
      "FamilyName": "Doe",
      "Postcode": "SW1A 1AA",
      "DateOfBirth": "1980-01-15"
    }
  }'

# Validate referred participant
curl -X POST "https://env-uks-static-validation.azurewebsites.net/api/StaticValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "Participant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Add",
      "ReferralFlag": "true",
      "NhsNumber": "9876543210",
      "FirstName": "John",
      "FamilyName": "Smith"
    }
  }'

# Validate amendment
curl -X POST "https://env-uks-static-validation.azurewebsites.net/api/StaticValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "Participant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Amend",
      "ReferralFlag": "false",
      "NhsNumber": "1111111111",
      "Postcode": "EC1A 1BB"
    }
  }'

```

### LookupValidation

```bash
# Validate with GP code lookup
curl -X POST "https://env-uks-lookup-validation.azurewebsites.net/api/LookupValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "NewParticipant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Add",
      "NhsNumber": "1234567890",
      "PrimaryCareProvider": "GP001"
    },
    "ExistingParticipant": null,
    "FileName": "batch_001.parquet"
  }'

# Validate VHR participant with gene code
curl -X POST "https://env-uks-lookup-validation.azurewebsites.net/api/LookupValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "NewParticipant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Add",
      "NhsNumber": "9876543210",
      "GeneCode": "BRCA1",
      "HigherRiskReferralReasonCode": "VHR001"
    },
    "ExistingParticipant": null,
    "FileName": "servicenow_case_123.json"
  }'

# Validate removal with reason code lookup
curl -X POST "https://env-uks-lookup-validation.azurewebsites.net/api/LookupValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "NewParticipant": {
      "ScreeningName": "Breast Screening",
      "RecordType": "Remove",
      "NhsNumber": "1111111111",
      "ReasonForRemoval": "RDR"
    },
    "ExistingParticipant": {
      "NhsNumber": "1111111111",
      "ScreeningId": 1
    },
    "FileName": "batch_002.parquet"
  }'

# Include response headers
curl -i -X POST "https://env-uks-lookup-validation.azurewebsites.net/api/LookupValidation" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @participant-lookup.json
```

### RemoveValidationException

```bash
# Remove validation exception for participant
curl -X POST "https://env-uks-remove-validation-exception-data.azurewebsites.net/api/RemoveValidationExceptionData" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "1234567890",
    "screeningName": "Breast Screening"
  }'

# Remove exception with response headers
curl -i -X POST "https://env-uks-remove-validation-exception-data.azurewebsites.net/api/RemoveValidationExceptionData" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d '{
    "nhsNumber": "9876543210",
    "screeningName": "Breast Screening"
  }'

# Using file input
curl -X POST "https://env-uks-remove-validation-exception-data.azurewebsites.net/api/RemoveValidationExceptionData" \
  -H "Content-Type: application/json" \
  -H "Ocp-Apim-Subscription-Key: your-key-here" \
  -d @remove-exception.json
```

---

## Validation Rules

### Rule Engine

All validation services use RulesEngine to execute rules:

- JSON-based rule definitions
- Workflow-based execution
- Support for custom actions and parameters
- Rules can be success-based or failure-based

### Static Rules

**Common Rules:**

- Apply to all non-removed participants
- Basic data quality checks
- Format validations
- Mandatory field checks

**Routine_Common Rules:**

- Apply only to routine participants (`ReferralFlag == "false"`)
- Routine-specific validations
- Additional quality checks

**RecordType-Specific Rules:**

- Apply based on RecordType (Add, Amend, etc.)
- Action-specific validations
- Context-aware checks

### Lookup Rules

**Database Validation:**

- GP practice code existence
- Gene code validity
- Removal reason code validity
- Higher risk referral reason validity

**Reference Data:**

- Validates codes against lookup tables
- Ensures referential integrity
- Checks data currency

---

## Configuration Summary

### StaticValidation Summary

```json
{
  "RuleFilesDirectory": "/home/site/wwwroot",
  "Breast_Screening_staticRules.json": "static-rules-file-path"
}
```

### LookupValidation Summary

```json
{
  "RuleFilesDirectory": "/home/site/wwwroot",
  "Breast_Screening_lookupRules.json": "lookup-rules-file-path",
  "DatabaseConnection": "database-connection-string"
}
```

### RemoveValidationException Summary

```json
{
  "ValidationExceptionDataServiceURL": "https://exception-service-url",
  "DatabaseConnection": "database-connection-string"
}
```

---

## Notes

### Validation Order

1. Static validation runs first (no database dependencies)
2. Lookup validation runs second (requires database)
3. Both must pass for participant to proceed
4. Failures create exceptions in database

### Rule File Location

- Rule files stored in function app directory
- Loaded at runtime
- Named by screening service
- JSON format using RulesEngine schema

### Validation Errors

- Array of validation error objects
- Each error contains rule name and message
- Multiple errors can be returned
- Errors indicate which validation check failed

### Exception Removal

- Only removes most recent exception
- Does not delete all exceptions for participant
- Used for resolving temporary data issues
- Re-validation recommended after removal
