# SpecFlow Test Automation Framework

## Overview
This test automation framework uses SpecFlow for behavior-driven development (BDD) testing.These are smoke tests for Cohort Manager application

## Prerequisites
.NET Core SDK (version X.X or higher)
Visual Studio 2022 or VS code
SpecFlow VS Extension
SQL Server (for database testing)
Azure Storage Explorer (for blob storage tests)

## Configuration
Update the appsettings.json file with your environment-specific settings:
 {

  "ConnectionStrings": {
      "DtOsDatabaseConnectionString": ""
    },
    "caasFileStorage": "",
    "BlobContainerName": "inbound",
    "ManagedIdentityClientId": ""
  }
## Running Tests
To execute the tests:

1.Open the solution in Visual Studio
2.Build the solution
3.Open Test Explorer (Test > Test Explorer)
4.Click "Run All" or select specific tests to run

### For command line execution

-dotnet test

## Best Practices
 1.Keep Scenarios independent for each journey in respective folders
 2.Use jira reference numbers as tags for test categorization
 3.Keep file indentation consistent across the solution
 4.Use common steps for repeated actions
 5.Implement proper clean up after tests

## Contributing
 1.Follow existing naming convention
 2.Comment code if necessary and document it
 3.Update README file accordingly
