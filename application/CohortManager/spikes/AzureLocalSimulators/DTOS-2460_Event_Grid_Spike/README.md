# SPIKE: Event Grid and Event Hub

Both Event Grid and Event Hub don't have an emulator that we can use to develop locally, at least there's nothing that is directly supported by Azure Services. As a workaround, we need to simulate the behaviour instead but using tools and libraries that mimic the functionalities.

## Overview of Azure Event Grid and Azure Event Hub

### Azure Event Grid

This is like a ticketing system for events. Event Grid is a highly scalable, fully managed Pub-sub message distribution service that offers flexible message consumption patterns using the MQTT and HTTP Protocols. It is used to react to events and trigger actions in response to them, making it great for real-time notifications and automation.

To test this locally, you will need 1) Azure Function that has Event Grid Trigger that will act as a Event Handler, or acts as a subscriber to the events, and 2) Event Grid Simulator, that triggers these events.

#### Set-up: Event Handler using Azure Function with Event Grid Trigger

Create a new project for the new Function by running the command below on the Terminal/command line. (Note: you have to be in the directory where you would liked to add the new project).

    func init [NAME_OF_FUNCTION]

It will prompt you to select some options. Enter the following values:

| QUESTION                               | OPTION | DESCRIPTION               |
|----------------------------------------|--------|---------------------------|
| **Select a number for worker runtime** | 2      | dotnet (isolated process) |
| **Select a number for language**       | 1      | c#-isolated               |

This will create a new directory called `[NAME_OF_FUNCTION]` and will contain a bunch of files, particularly [NAME_OF_FUNCTION].csproj and `local.settings.json`

Open the `local.settings.json` and udpate this file and add this snippet below at the end of the file to change the port number to use and save the file..

    "Host": {
        "LocalHttpPort": [PORT_NUMBER]
    }

Create a new Function by running the commands on the Terminal/Command Line:

    cd [NAME_OF_FUNCTION]
    func new --name [NAME_OF_FUNCTION_TRIGGER] --templateName

It will prompt you to select some options. Enter the following values:

| QUESTION                               | OPTION | DESCRIPTION               |
|----------------------------------------|--------|---------------------------|
| **Select a number for worker runtime** | 2      | dotnet (isolated process) |
| **Select a number for language**       | 1      | c#-isolated               |
| **Select a number for template**       | 8      | EventGridTrigger          |

It will create the [NAME_OF_FUNCTION_TRIGGER].cs file inside the `[NAME_OF_FUNCTION]` directory. Open up the [NAME_OF_FUNCTION_TRIGGER].cs file and copy and paste this code (or add the code for an actual function that handles an Event Grid Event).

    TODO: Add .cs File Code

Save your changes. Then in the Terminal/Command line, run the function using the following command:

    func start

#### Set-up: Event Grid Simulator Using PodMan

#### NuGet Packages

Install the following NuGet packages and run the restore, clean and build.

    dotnet add package Newtonsoft.Json
    dotnet add package Microsoft.Azure.WebJobs
    dotnet add package Microsoft.Azure.Functions.Worker
    dotnet add package Microsoft.Azure.Functions.Worker.Extensions.EventGrid
    dotnet restore && dotnet clean && dotnet build


#### Useful Resources

* [Test your Event Grid Handler Locally](https://learn.microsoft.com/en-us/azure/communication-services/how-tos/event-grid/local-testing-event-grid)
* [Azure Event Grid Trigger for Azure Function](https://learn.microsoft.com/en-us/azure/azure-functions/functions-bindings-event-grid-trigger?tabs=python-v2%2Cin-process%2Cnodejs-v4%2Cextensionv3&pivots=programming-language-csharp)

---

### Azure Event Hub

This is like the Parking Lot for events. It's used for collecting and storing a large volume of events, especially when you need to process them later or in batches.

Similarly to Event Grid, Event Hub has no official emulator, or local development environment. Few options to do is to:

* Use Azure Event Hubs in the cloud
* Local Emulation of AMQP (Advanced Message Queuing Protocol) that emulates the Event Hubs AMQP interface locally.
* Simulate Event Hub Triggered Functions, similar to the Event Grid, but the Azure Function will have Event Hub trigger instead
