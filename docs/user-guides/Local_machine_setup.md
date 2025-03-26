# Cohort Manager

## Prerequisites

- Visual Studio Code - VS Code is the chosen editor for the project due to VS for Mac being retired
- Git
- HomeBrew (Mac Only): \
    `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"`
- [.NET SDK (8.0)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Member of the NHSDigital GitHub organisation
- Signed Git commits (if you would like to contribute): \
    [Using 1Password](https://developer.1password.com/docs/ssh/git-commit-signing/) is the easiest option if you have it, otherwise use the below link for instructions \
    <https://github.com/NHSDigital/software-engineering-quality-framework/blob/main/practices/guides/commit-signing.md>
- Added the git submodule:
  `git submodule update --init --recursive`

## Set-up

### 1. Import the NHS DToS Profile

To easily install the required extensions and settings/configuration for VS Code, you can import the profile located in `Set-up/NHS_DToS.code-profile`

On the top toolbar of Visual Studio Code go to *Code > Settings > Profiles > Import Profile > click on Select File...* and select the file **NHS_DToS.code-profile**

### 2. Azure Functions Core Tools

Azure Function Core Tools lets you develop and test your functions on your local computer. To install, press `ctrl/ command + shift + P` and enter `Azure Functions: Install or Update Azure Functions Core Tools`

### 3. Azure Data Studio & Storage Explorer

Azure Data Studio & Storage Explorer are the GUI tools we are using to manually interact with the database & Azure Storage respectively.

- Install [Azure Data Studio](https://learn.microsoft.com/en-us/azure-data-studio/download-azure-data-studio?tabs=wi[…]all%2Credhat-install%2Cwindows-uninstall%2Credhat-uninstall)
- (Optional) Install [Azure Storage Explorer](https://azure.microsoft.com/en-gb/products/storage/storage-explorer)

Use the **Intel Chip/ x64** installer if you have and Intel Chip in your Mac. Otherwise, use the **Apple Silicon/ ARM64** installer.

*Note: to check which version you are using, you can click on the Apple icon of your machine > About this Mac and a new window will appear. You can see the Chip your machine. Intel will have Intel in it, Apple Silicon will have something like Apple M1.*

### 4. Download Docker/ Podman

If you are on Windows, install Docker Engine using [these instructions](https://medium.com/@rom.bruyere/docker-and-wsl2-without-docker-desktop-f529d15d9398)

If you are on Mac, install Podman by running:

```bash
brew install --cask podman
brew install podman-compose

# Allocate sufficient resources to Podman:
podman machine stop
podman machine set --cpus=6 --memory=12288 --disk-size=125
podman machine start
```

## Running the Application

The docker compose has now been split into 4 files due to the size of the application being too large to build in one go. There are now 4 files:

- compose.deps.yaml - contains the database, azurite and setup containers, this must be run before the other files
- compose.core.yaml - contains the core functions
- compose.cohort-distribution.yaml - cohort distribution
- compose.data-services.yaml - contains the data services
- compose.yaml - imports the core and cohort distribution files so they can be interacted with together

First, copy the .env.example file, rename it to just ".env", and follow the instructions inside the file to add the variables.

Several vscode tasks have been made for common docker operations for Windows and Mac, which you can access by pressing ctrl/ cmd + shift + p entering the command Tasks: Run Task, and searching for either Win or Mac to run the commands

> **Note:** Pressing ctrl/ cmd + shift + B will build and run the application automatically in vscode

To build and run the application manually in the terminal, run the following commands in the application/CohortManager directory:\
If you are on Mac, you will need to replace `docker` with `podman`

```bash
# Build the functions
docker compose -f compose.core.yaml build
docker compose -f compose.cohort-distribution.yaml build
docker compose -f compose.data-services.yaml build

docker compose -f compose.deps.yaml up --build -d # Run the deps before the rest of the functions
docker compose up # Run the functions
```

>**Note:** This will take a while the first time

Other useful commands:

```bash
docker compose down    # Stop the functions
docker compose up/ down <service-name>   # Start a particular function or dependency

# Example:
docker compose up receive-caas-file

docker ps -a   # List all of the containers
docker logs <container-name>   # View the logs of the container
```

Alternatively, you can run an individual function locally with `func start`

### Profiles

To make the application more manageable to run, some functions have had [docker compose profiles](https://docs.docker.com/compose/how-tos/profiles/) added to them, which means they will not build and run unless specified

Key of profiles:

- bi-analytics - Functions that are only used by the external BI & Analytics product
- bs-select - Functions that are only used by external requests from BS Select
- ui - only used by the user interface
- non-essential - Functions that are not needed to run the application
- not-implemented - Functions that do not yet have an implementation and are not in use

You can run a specific profile with `docker compose --profile <profile-name> up`

Or, to run the whole application `docker compose --profile "*" up`


## Appendix A: Storage

### The send-sample-file.py script

There is a script in `application/CohortManager/Set-up/azurite` that alllows you to send sample files to azurite.

Before your run the script you must download the sample files from confluence (you can see which files you need to download by running the command without arguments) run the following command: `pip install azure-storage-blob python-dotenv`

Run the file without arguments (`pyhton send-sample-file.py`) to see the help page

### Set-up Azure Storage Explorer

Alternatively, you can use the storage explorer to send files to azurite

Open the Azure Storage Explorer and in the Explorer, you will see **Azurite (Key)**. Expand that and you will see 1) Blob Container 2) Queues and 3) Tables. Right click on the **Blob Container** and click on **Create Blob Container**.

On Azure Storage Explorer, collapse **Emulator & Attached > Storage Accounts > Azurite (Key)** and right click on **Blob containers** and select Create Blob Container and type in `inbound` to create a container with that name.

![inbound blob container](../assets/azure_storage.png)

Once created, use the sample csv files upload it to that new inbound container.

Back in VS Code you should see the logs of the functions running locally, once it's complete, you can refresh the database again to see the changes made by the CSV files.

*Note: Sample Data and Scripts to create the database are provided by the Data team. The latest files can be found in the `dtos-data-modes` repository in the NHS Digital GitHub <https://github.com/NHSDigital/dtos-data-models>
