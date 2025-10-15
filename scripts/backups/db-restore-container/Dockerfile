# Use a base image with PowerShell already installed.
FROM mcr.microsoft.com/powershell:latest

# Create a non-root user and group
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Update package lists, install unzip, and clean up to reduce image size.
RUN apt-get update && apt-get --no-install-recommends install -y unzip
RUN rm -rf /var/lib/apt/lists/*

# Install the Az PowerShell module for Azure authentication
RUN pwsh -Command "Install-Module -Name Az.Accounts -RequiredVersion 2.12.0 -Force -Scope AllUsers"

# Install SqlPackage.
ADD https://aka.ms/sqlpackage-linux /tmp/sqlpackage.zip
RUN mkdir /opt/sqlpackage \
    && unzip /tmp/sqlpackage.zip -d /opt/sqlpackage \
    && rm /tmp/sqlpackage.zip \
    && chmod +x /opt/sqlpackage/sqlpackage

# Switch to the non-root user
USER appuser

# Set the working directory for the non-root user
WORKDIR /app

# Copy the PowerShell script into the container.
COPY import-using-identity.ps1 .

# Define the command to run when the container starts.
CMD ["pwsh", "./import-using-identity.ps1"]
