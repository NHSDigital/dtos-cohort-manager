# Retrieve Mesh File Function
---

## About

This function is designed to download all files from a given [MESH](https://digital.nhs.uk/services/message-exchange-for-social-care-and-health-mesh) mailbox and transfer them to a provided Azure Blob Storage Container.

This function depends on an external class library [dotnet-mesh-client](https://github.com/NHSDigital/dotnet-mesh-client). Currently this is referenced from within the Common library and is pulled down via a [git submodule](https://git-scm.com/book/en/v2/Git-Tools-Submodules).

## Build requirements

when initially cloned or pulled from the repo please run `git submodule update --init --recursive` this will download the dotnet-mesh-client code.
This should incrementally be run to keep that code up to date.

Azurite or a connection to a hosted Azure Storage Account to be installed and running.

## Local Testing

To test this function locally there are two methods, either you can connect this to a MESH Integration environment mailbox or to a locally run MESH sandbox

Please see the guide to setting up the local Mesh Sandbox [here](https://nhsd-confluence.digital.nhs.uk/display/DTS/Setting+up+local+Mesh-Sandbox+Environment).

For connecting to an Integration environment Mailbox you will require the following:

* Mesh Mailbox Id
* Mesh Mailbox Password
* A Mesh Key in the .pfx format
* the passphrase for the key
* url for the integration environment

We recommend naming the Mesh Key File as `meshpfx.pfx` and sorting it in to root folder of the function as this will prevent additional changes to have to be made to the project file to copy on build.

A generic key is available for connecting to the Integration Environment.

These will need to be added to the environment settings locally as per the example below:
note: when running in azure these values will be stored in key vault.

```json
{
    "IsEncrypted": false,
    "Values": {
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "MeshApiBaseUrl" : "https://localhost:8700/messageexchange",
        "BSSMailBox": "X26ABC1",
        "caasfolder_STORAGE":"UseDevelopmentStorage=true",
        "MeshPassword": "password",
        "MeshSharedKey": "TestKey",
        "MeshKeyPassphrase":"test123"

    }
}
```
