# Cohort Manager

[![CI/CD Pull Request](https://github.com/nhs-england-tools/repository-template/actions/workflows/cicd-1-pull-request.yaml/badge.svg)](https://github.com/nhs-england-tools/repository-template/actions/workflows/cicd-1-pull-request.yaml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=repository-template&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=repository-template)

A service for identifying and managing cohorts of citizens. Users can select individuals to be part of a cohort, based on rules which are set by central bodies,\
 or they can select individuals on an ad-hoc basis when planning their capacity. Rules for selecting individuals can be based on both demographic and medical criteria.  

## Table of Contents

- [Repository Template](#repository-template)
  - [Table of Contents](#table-of-contents)
  - [Setup](#setup)
    - [Prerequisites](#prerequisites)
    - [Configuration](#configuration)
  - [Usage](#usage)
    - [Testing](#testing)
  - [Design](#design)
    - [Diagrams](#diagrams)
    - [Modularity](#modularity)
  - [Contributing](#contributing)
  - [Contacts](#contacts)
  - [Licence](#licence)

## Set-up
### 1. Prerequisites

- Visual Studio Code
- Git
- HomeBrew (Mac Only): \
    `/bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/master/install.sh)"`
- [.NET SDK (8.0)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Signed Git commits (if you would like to contribute): \
    [Using 1Password](https://developer.1password.com/docs/ssh/git-commit-signing/) is the easiest option if you have it, otherwise use the below link for instructions \
    <https://github.com/NHSDigital/software-engineering-quality-framework/blob/main/practices/guides/commit-signing.md>
- Added the git submodule:
  `git submodule update --init --recursive`

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

### Configuration

Copy the .env.example file, rename it to just ".env", and follow the instructions inside the file to add the variables.

> **Note:** For existing users, make sure you replace where it says 127.0.0.1 in the azurite connection string and replace it with "azurite"

## Usage

After a successful installation, please follow this [User Guide](./docs/user-guides/user_guide.md) to use the application.

### Testing

There are `make` tasks for you to configure to run your tests. Run `make test` to see how they work. You should be able to use the same entry points for local development as in your CI pipeline.

[Functions Testing Guide using Playwright Test Framework](tests/playwright-tests/README.md)

## Contributing

[Contributing](CONTRIBUTING.md)

## Contacts

Provide a way to contact the owners of this project. It can be a team, an individual or information on the means of getting in touch via active communication channels, e.g. opening a GitHub discussion, raising an issue, etc.

## Licence

> The [LICENCE.md](./LICENCE.md) file will need to be updated with the correct year and owner

Unless stated otherwise, the codebase is released under the MIT License. This covers both the codebase and any sample code in the documentation.

Any HTML or Markdown documentation is [© Crown Copyright](https://www.nationalarchives.gov.uk/information-management/re-using-public-sector-information/uk-government-licensing-framework/crown-copyright/) and available under the terms of the [Open Government Licence v3.0](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/).
