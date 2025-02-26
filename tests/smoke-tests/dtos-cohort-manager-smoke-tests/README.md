# SpecFlow Test Automation Framework

## Overview

This test automation framework utilizes SpecFlow, a behavior-driven development (BDD) tool, to create and execute smoke tests for the Cohort Manager application. SpecFlow allows defining test scenarios using a natural language syntax, making tests more readable and maintainable.

## Table of Contents

- [SpecFlow Test Automation Framework](#specflow-test-automation-framework)
  - [Overview](#overview)
  - [Table of Contents](#table-of-contents)
  - [Prerequisites](#prerequisites)
  - [Configuration](#configuration)
  - [Running Tests](#running-tests)
    - [Visual Studio](#visual-studio)
    - [Command Line](#command-line)
  - [Best Practices](#best-practices)
  - [Contributing](#contributing)

## Prerequisites

To set up and run this test automation framework, ensure you have the following prerequisites installed:

* . NET Core SDK (version X. X or higher)
* Visual Studio 2022 or Visual Studio Code
* SpecFlow Visual Studio Extension
* SQL Server (for database testing)
* Azure Storage Explorer (for blob storage tests)

## Configuration

Create a new file named `appsettings-local.json` in the Config directory of the smoke tests project
Copy the template below into the file:

```json
"AppSettings": {
   // please add your local DB connection string below
    "ConnectionStrings": {
      "DtOsDatabaseConnectionString": ""
    },
    // config to switch between local and cloud setup
    "AzureSettings": {
    "IsCloudEnvironment": true
  },
   // Test data file paths
 "FilePaths": {
  "Add": "TestFiles/add/",
  "Amended": "TestFiles/amended/",
  "Remove": "TestFiles/remove/"
},
    //Blob storage connection string
    "CloudFileStorageConnectionString": "",
    "BlobContainerName": "inbound",
    "ManagedIdentityClientId": ""
  }
```

Replace the placeholders:

`DtOsDatabaseConnectionString` : Your local SQL Server connection string
`CloudFileStorageConnectionString` : For local development, use Azure Storage Emulator (UseDevelopmentStorage=true) or a connection string to your development storage account
For local testing, keep IsCloudEnvironment set to false to avoid managed identity authentication

## Running Tests

### Visual Studio

To execute the tests using Visual Studio:

1. Open the solution in Visual Studio.
2. Build the solution to ensure all dependencies are resolved.
3. Open the Test Explorer window (Test > Test Explorer).
4. Click on the "Run All" button to run all the tests, or select specific tests to run individually.

### Command Line

Alternatively, you can run the tests from the command line using the following command:

```shell
dotnet test
```

This command will discover and execute all the tests in the solution.

## Best Practices

1. Keep scenarios independent for each journey and organize them in respective folders. This promotes modularity and easier maintenance.
2. Use story reference numbers as tags for test categorization. This allows easy identification and selection of tests based on specific stories or requirements.
3. Maintain consistent file indentation across the solution for improved readability.
4. Utilize common steps for repeated actions to avoid duplication and improve reusability.
5. Implement proper cleanup mechanisms after each test to ensure a clean state for subsequent tests.

## Contributing

When contributing to this test automation framework, please follow these guidelines:

1. Follow the existing naming conventions for files, variables, and methods to maintain consistency.
2. Provide comments and documentation for any complex or non-obvious code segments.
3. Update the README file with any relevant changes or additions made to the framework.
