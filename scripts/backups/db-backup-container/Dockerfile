# Use a base image with PowerShell already installed.
FROM mcr.microsoft.com/powershell:latest

# Create a non-root user and group
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Update package lists and install required tools.
# 'lsb-release' is added to fix the 'lsb_release: not found' error.
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    unzip \
    gnupg \
    lsb-release \
    && rm -rf /var/lib/apt/lists/*

# Install the Az PowerShell module for Azure authentication.
RUN pwsh -Command "Install-Module -Name Az.Accounts -RequiredVersion 2.12.0 -Force -Scope AllUsers"

# Install SqlPackage by downloading the binary.
RUN curl --proto "=https" --tlsv1.2 -sSfL -o sqlpackage.zip https://aka.ms/sqlpackage-linux \
    && mkdir /opt/sqlpackage \
    && unzip sqlpackage.zip -d /opt/sqlpackage \
    && rm sqlpackage.zip \
    && chmod +x /opt/sqlpackage/sqlpackage

# Install AzCopy from the Microsoft repository.
# The 'sh -c' command is fixed to correctly interpret 'lsb_release -cs'.
# We also ensure the apt cache is updated before installing AzCopy.
RUN curl --proto "=https" --tlsv1.2 -sSfL https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor > packages.microsoft.gpg \
    && install -o root -g root -m 644 packages.microsoft.gpg /etc/apt/trusted.gpg.d/ \
    && sh -c "echo 'deb [arch=amd64,arm64,armhf] https://packages.microsoft.com/ubuntu/22.04/prod $(lsb_release -cs) main' > /etc/apt/sources.list.d/microsoft-prod.list" \
    && rm packages.microsoft.gpg \
    && apt-get update \
    && apt-get install -y --no-install-recommends azcopy

# Switch to the non-root user
USER appuser

# Set the working directory for the non-root user
WORKDIR /app

# Copy the PowerShell script into the container.
COPY export-using-identity.ps1 .

# Define the command to run when the container starts.
CMD ["pwsh", "./export-using-identity.ps1"]
