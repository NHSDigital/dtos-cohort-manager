
# Dev Containers

Dev Containers functionality is a platform tool that enables developers to use a streamlined and consistent developer environment, no matter the hardware/software platform they are on. It is using Docker containers that are specifically configured to provide a fully featured, ready-to-use environment.

## Prerequisites

The following software packages, or their equivalents, are expected to be installed and configured:

- [Docker](https://www.docker.com/) container runtime or a compatible tool, e.g. [Docker Desktop]([Docker Desktop](https://www.docker.com/products/docker-desktop/)) (recommended if licensing isn't an issue) or [Podman](https://podman.io/)
- [Visual Studio Code](https://code.visualstudio.com/download) source-code editor,
- [Dev Containers](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers) Visual Studio Code extension responsible for running the Dev Container feature

## Setup

To be able to use the Dev Container feature:

1. first make sure that the cloned repository has a folder named `.devcontainer.`, with a file named `devcontainer.json` inside.
2. Run the command palette in Visual Studio Code by pressing `CMD+SHIFT+P` (MacOS) / `CTRL+SHIFT+P` (Windows). In the command palette search for `Rebuild and Reopen in Container`.
3. Visual Studio Code windows should reopen and automatic Docker image download and configuration will start.
After the process is complete, user should be presented with a complete and working development environment. This can be tested by going into the main repository folder (`dtos-landing-zone` - should be there by default) and running below command:

```shell
make config
```

If handled without errors, environment has all needed prerequisites and is ready to work.

## Additional information

Current configuration is based on a [basic Ubuntu OS Docker image](mcr.microsoft.com/devcontainers/base:jammy) and features as follows:

- Apt / Apt-Get packet manager
- NPM packet manager
- Azure CLI
- GitHub CLI
- Terraform
- ASDF manager
- curl

It will also try to install Visual Studio Code extensions listed in file `/.vscode/extensions.json`. If new extensions are put in this file, `devcontainer.json` needs to be updated using the `scripts/config/devcontainer/upd-vscode-ext.sh` bash script.

## Sharing GPG Keys from the parent Operating System

Information on how to share with the Dev Container environment GPG Keys created under the parent Operating System can be found on the proper [Visual Studio Code documentation page](https://code.visualstudio.com/remote/advancedcontainers/sharing-git-credentials#_sharing-gpg-keys).
Proper key can also be created manually inside the container, but they are going to be deleted after each rebuild.

## Temporary information

Until the Dev Container is merged to a main branch, you might have to pull the `feat/dev-containers` branch from the main repository to get the `.devcontainer.` folder and use the Dev Container feature.
