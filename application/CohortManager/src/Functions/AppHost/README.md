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
            "NemsLocalCertPassword": "",
            "NemsMeshMailboxId": ""
        }
    }
    ```

    `SqlPassword` is the password for your local db. It must be at least 8 characters long and contain characters from three of the following four categories: uppercase letters, lowercase letters, numbers, and non-alphanumeric characters.

    `NemsLocalCertPassword` and `NemsMeshMailboxId` can be any value if using the stubbed ManageNemsSubscription (default).

2. Install .NET Aspire if you haven't already
    ```
    dotnet tool install -g Aspire.Cli
    ```

3. Verify you have Docker or Podman running

4. Run `dotnet run` from the AppHost directory
