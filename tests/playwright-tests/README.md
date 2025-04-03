# Functions Testing Guide using Playwright Test Framework

## Prerequisites

- Local functions environment running
- Node.js installed
- Git installed
- VS Code or any preferred IDE
- Verify all functions are operational

## Getting Started

1. Clone the repository to get all functions and test files

   ```bash

   git clone https://github.com/NHSDigital/dtos-cohort-manager.git

   ```

2. Follow the setup instructions in the docs located at [Local machine setup](../../docs/user-guides/Local_machine_setup.md) to complete the local setup of functions and dependencies

## Running Playwright Tests

### Local Execution

1. Rename `.env.dev.example` to `.env.dev`.
2. Update the following configuration according to your local settings

```bash

# Azure Storage
AZURE_CONNECTION_STRING=''
CONTAINER_NAME=''

# Azure SQL Database
SQL_HOST=''
SQL_USER=''
SQL_PASSWORD=''
SQL_DATABASE=''
SQL_RETRIES=''
SQL_WAIT_TIME=''

```

3. Run tests

```bash

# Navigate to test framework
cd ../dtos-cohort-manager/tests/playwright-tests

# Install dependencies
npm install

# Run all tests
npm test

# Run specific test file
npx playwright test src/tests/e2e/e2e-with-db.spec.ts

```

### Cloud Execution

>Note: will be updated soon

## Test Flow Overview

1. **File Upload**: Upload test file to storage
2. **Processing**: Local functions process the uploaded file
3. **Validation**: Verify results in cohort using **dynamic** database queries

## Contributing to improve Test Coverage & Framework

- Identify gaps in current test coverage, and or framework
- Create new tests/ utilities following the existing patterns
- Submit pull requests with your improvements
- Document any new test utilities or approaches

## Reusable Components

### File Processor

Upload any parquet file to the system by specifying the filename

```ts

// Example usage
async function processFileViaStorage(fileName: string);

```
All test files should be placed under `playwright-tests/tests/e2e/testfiles`

### Validation Engine

The validation engine accepts a JSON configuration to build dynamic, SQL-injection-protected queries

```ts

// Example usage
      const checkInDatabase = {
        "validations": [{
          "validations": {
            "tableName": "BS_COHORT_DISTRIBUTION",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[0]
          }
        }, {
          "validations": {
            "tableName": "BS_COHORT_DISTRIBUTION",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[1]
          }
        },
        {
          "validations": {
            "tableName": "PARTICIPANT_MANAGEMENT",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[0],
            "columnName2": "EXCEPTION_FLAG",
            "columnValue2": "0"
          }
        }, {
          "validations": {
            "tableName": "PARTICIPANT_MANAGEMENT",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[1],
            "columnName2": "EXCEPTION_FLAG",
            "columnValue2": "0"
          }
        }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);

```



