# Repository Template

[![CI/CD Pull Request](https://github.com/nhs-england-tools/repository-template/actions/workflows/cicd-1-pull-request.yaml/badge.svg)](https://github.com/nhs-england-tools/repository-template/actions/workflows/cicd-1-pull-request.yaml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=repository-template&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=repository-template)

Start with an overview or a brief description of what the project is about and what it does. For example -

Welcome to our repository template designed to streamline your project setup! This robust template provides a reliable starting point for your new projects, covering an essential tech stack and encouraging best practices in documenting.

This repository template aims to foster a user-friendly development environment by ensuring that every included file is concise and adequately self-documented. By adhering to this standard, we can promote increased clarity and maintainability throughout your project's lifecycle. Bundled within this template are resources that pave the way for seamless repository creation. Currently supported technologies are:

- Terraform
- Docker

Make use of this repository template to expedite your project setup and enhance your productivity right from the get-go. Enjoy the advantage of having a well-structured, self-documented project that reduces overhead and increases focus on what truly matters - coding!

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

## Setup

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

### Configuration

Copy the .env.example file, rename it to just ".env", and follow the instructions inside the file to add the variables.

> **Note:** For existing users, make sure you replace where it says 127.0.0.1 in the azurite connection string and replace it with "azurite"

## Usage

After a successful installation, please follow this [User Guide](./docs/user-guides/user_guide.md) to use the application.

### Testing

There are `make` tasks for you to configure to run your tests. Run `make test` to see how they work. You should be able to use the same entry points for local development as in your CI pipeline.

[Functions Testing Guide using Playwright Test Framework](tests/playwright-tests/README.md)

## Design

### Diagrams

The [C4 model](https://c4model.com/) is a simple and intuitive way to create software architecture diagrams that are clear, consistent, scalable and most importantly collaborative. This should result in documenting all the system interfaces, external dependencies and integration points.

![Repository Template](./docs/diagrams/Repository_Template_GitHub_Generic.png)

### Modularity

Most of the projects are built with customisability and extendability in mind. At a minimum, this can be achieved by implementing service level configuration options and settings. The intention of this section is to show how this can be used. If the system processes data, you could mention here for example how the input is prepared for testing - anonymised, synthetic or live data.

## Contributing

Describe or link templates on how to raise an issue, feature request or make a contribution to the codebase. Reference the other documentation files, like

- Environment setup for contribution, i.e. `CONTRIBUTING.md`
- Coding standards, branching, linting, practices for development and testing
- Release process, versioning, changelog
- Backlog, board, roadmap, ways of working
- High-level requirements, guiding principles, decision records, etc.

## Contacts

Provide a way to contact the owners of this project. It can be a team, an individual or information on the means of getting in touch via active communication channels, e.g. opening a GitHub discussion, raising an issue, etc.

## Licence

> The [LICENCE.md](./LICENCE.md) file will need to be updated with the correct year and owner

Unless stated otherwise, the codebase is released under the MIT License. This covers both the codebase and any sample code in the documentation.

Any HTML or Markdown documentation is [© Crown Copyright](https://www.nationalarchives.gov.uk/information-management/re-using-public-sector-information/uk-government-licensing-framework/crown-copyright/) and available under the terms of the [Open Government Licence v3.0](https://www.nationalarchives.gov.uk/doc/open-government-licence/version/3/).
