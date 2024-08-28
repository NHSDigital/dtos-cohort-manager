# Integration Tests

This directory contains the integration tests for the project. These tests verify that the different components in the application work together as expected and in some cases verify the end-to-end functionality of various features as well as interactions.

## Prerequisites

1. **.NET SDK**
2. **Azure Functions Core Tools**: Ensure that the relevant Azure Functions are running before you run your integration tests.
3. **Local Database**: Make sure a local database is set up and accessible.
4. **Azurite**: Install and run Azurite for local Azure Storage emulation.
5. **Test Data**: Place the required CSV (or in future Parquet files) in the `TestFiles` directory.

## Setup

1. **Clone the Repository**:

    ```bash
    git clone https://github.com/NHSDigital/dtos-cohort-manager.git
    cd dtos-cohort-manager
    ```

2. **Install Dependencies**:
    Ensure you have all necessary dependencies installed. E.g Azure Functions Core Tools, Azurite etc.

    ```bash
    dotnet restore
    dotnet clean
    dotnet build
    ```

3. **Set Up Configuration**:
    - Rename the `appsettings.json.template` in config folder to `appsettings.json`.
    - Fill in the required configuration values in `appsettings.json`.

4. **Place Test Files**:
    - Place the required CSV (or in future Parquet) test files in the `TestFiles` directory.

## Running the Tests

1. **Start Azure Functions**:
    Ensure all necessary Azure Functions are running. Cd into all of the required functions folders and run...

    ```bash
    func start
    OR in Visual Studio Code press 'command+shift+p' then search for and click 'Tasks:Run Task' then click 'Run All Functions' to start all functions.
    ```

2. **Run Integration Tests**:
    cd into the IntegrationTests folder and use the `dotnet test` command to run the integration tests. Ensure that only the integration tests are run by specifying the appropriate category.

    ```bash
    dotnet test
    dotnet test --filter TestCategory=Integration // Filter by Test Category if running tests from main tests folder.
    dotnet test --filter E2E_FileUploadAndCreateParticipantTest --logger "console;verbosity=detailed" // Run a specific test with logging
    ```

## Troubleshooting

- **Missing Test Files**:
    Ensure that the CSV files are placed in the `TestFiles` directory and that the file paths in the tests match the actual file locations.

- **Configuration Issues**:
    Double-check the `appsettings.json` file to ensure all configurations are set correctly.

- **Azure Functions Not Running**:
    Ensure all necessary Azure Functions are running locally. Use `func start` to start the Azure Functions.
- **Ensure everything is working manually first**:
    Verifying that everything is working manually first and stepping through each part of the test can help you debug any errors you may encounter.

## Additional Information

For more detailed information on each test and the overall testing strategy, refer to the Test Plan documentation.
