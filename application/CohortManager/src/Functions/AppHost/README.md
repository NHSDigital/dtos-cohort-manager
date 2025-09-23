# How to run the Cohort Manager backend solution with .NET Aspire

## Prerequisites

- .NET 8.0 or later
- A container runtime (Docker or Podman)

## Steps

1. Create an `appsettings.json` file in the AppHost directory and provide values for the Parameters

    ```
    {
        "Logging": {
            "LogLevel": {
                "Default": "Information",
                "Microsoft.AspNetCore": "Warning",
                "Aspire.Hosting.Dcp": "Warning"
            }
        },
        "Parameters": {
            "SqlPassword": "",
            "MeshSandboxKeyPasspharse": "",
            "NemsLocalCertPassword": "",
            "PdsClientId": ""
        }
    }
    ```

    `SqlPassword` - The local db password. Must be at least 8 characters long and contain characters from at least three of the following four categories: uppercase letters, lowercase letters, numbers, and non-alphanumeric characters.

    `MeshSandboxKeyPasspharse` - The password for your local MESH Sandbox certificate. Required for the RetrieveMeshFile, ManageCaasSubscription & NemsMeshRetrieval functions. Can be set to any value if using the stubbed versions.

    `NemsLocalCertPassword` - The password for your local NEMS certificate. Required for the ManageNemsSubscription function, can be set to any value if using the stubbed version (default).

    `PdsClientId` - The client id for using PDS. Required for the RetrievePDSDemographic function, can be set to any value if using the stubbed version (default).

2. Install .NET Aspire if you haven't already
    ```
    dotnet tool install -g Aspire.Cli
    ```

3. Verify you have Docker or Podman running

4. Run `dotnet run` from the AppHost directory
