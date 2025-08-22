# Use a base image with PowerShell already installed.
FROM mcr.microsoft.com/powershell:latest

# Update package lists and install wget and unzip.
RUN apt-get update && apt-get install -y wget unzip

# Install the Az PowerShell module for Azure authentication
RUN pwsh -Command "Install-Module -Name Az.Accounts -RequiredVersion 2.12.0 -Force -Scope AllUsers"

# Install SqlPackage.
RUN wget -O sqlpackage.zip https://aka.ms/sqlpackage-linux \
    && mkdir /opt/sqlpackage \
    && unzip sqlpackage.zip -d /opt/sqlpackage \
    && rm sqlpackage.zip \
    && chmod +x /opt/sqlpackage/sqlpackage

# Set the working directory.
WORKDIR /app

# Copy the PowerShell script into the container.
COPY export-using-identity.ps1 .

# Define the command to run when the container starts.
CMD ["pwsh", "./export-using-identity.ps1"]
