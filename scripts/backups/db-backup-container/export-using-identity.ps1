# PowerShell script to export an Azure SQL Database to a BACPAC file using a Managed Identity and upload it to Azure Blob Storage.
# This script is intended to run inside a Docker container with the necessary tools installed (sqlpackage, Az PowerShell module).
# It requires the following environment variables to be set:
# - SQL_SERVER_NAME: The name of the Azure SQL Server (without .database.windows.net)
# - SQL_DATABASE_NAME: The name of the database to export
# - STORAGE_ACCOUNT_NAME: The name of the Azure Storage Account
# - STORAGE_CONTAINER_NAME: The name of the Blob container to upload the BACPAC file to
# - MANAGED_IDENTITY_CLIENT_ID: The Client ID of the User Assigned Managed Identity with access to the SQL Database and Storage Account
# - TARGET_SUBSCRIPTION_ID: The Subscription ID where the resources are located

# Check for Az modules
try {
    Import-Module Az.Accounts -ErrorAction Stop
}
catch {
    Write-Error "Failed to import Az modules. Ensure the Az PowerShell module is installed in the container. Error: $($_.Exception.Message)"
    exit 1
}
Write-Output "Imported Az modules successfully."

$ServerName = $env:SQL_SERVER_NAME
$DatabaseName = $env:SQL_DATABASE_NAME
$StorageAccountName = $env:STORAGE_ACCOUNT_NAME
$ContainerName = $env:STORAGE_CONTAINER_NAME
$ManagedIdentityClientId = $env:MANAGED_IDENTITY_CLIENT_ID
$TargetSubscriptionId = $env:TARGET_SUBSCRIPTION_ID

# Check if environment variables are set
if (-not $ServerName -or -not $DatabaseName) {
    Write-Error "Error: SQL_SERVER_NAME or SQL_DATABASE_NAME environment variable is not set."
    exit 1
}
elseif (-not $StorageAccountName -or -not $ContainerName) {
    Write-Error "Error: STORAGE_ACCOUNT_NAME or STORAGE_CONTAINER_NAME environment variable is not set."
    exit 1
}
elseif (-not $ManagedIdentityClientId) {
    Write-Error "Error: MANAGED_IDENTITY_CLIENT_ID environment variable is not set."
    exit 1
}
elseif (-not $TargetSubscriptionId) {
    Write-Error "Error: TARGET_SUBSCRIPTION_ID environment variable is not set."
    exit 1
}
else {
    Write-Output "Environment variables are set correctly."
}

# Connect using Managed Identity
try {
    # For System Assigned Managed Identity
    Connect-AzAccount -Identity -AccountId $ManagedIdentityClientId -ErrorAction Stop
    Select-AzSubscription -SubscriptionId $TargetSubscriptionId -ErrorAction Stop
} catch {
    Write-Error "Failed to connect to Azure using Managed Identity. Error: $($_.Exception.Message)"
    exit 1
}

# Define SQL Package path here:
$SqlPackagePath = "/opt/sqlpackage/sqlpackage"

# Define the backup file name with timestamp
$BackupFileName = "${ServerName}-${DatabaseName}_$(Get-Date -Format 'yyyy-MM-dd_HHmmss').bacpac"
$localFilePath = "/tmp/$BackupFileName"
Write-Output "Backup file will be named: $BackupFileName"

# Use a connection string as this appears(after much trial and error) to be the only way to get sqlpackage to work with the correct User Assigned Managed Identity.
# Unfortunately the string parameters vary slightly compared to those used by the db-management container so it's easiest to compose it here rather than in the Terraform code.
$ConnectionString = "Server=$ServerName.database.windows.net;Authentication=Active Directory Managed Identity; Encrypt=True; User Id=$ManagedIdentityClientId; Database=$DatabaseName"

$Arguments = @(
    "/Action:Export"
    "/TargetFile:$localFilePath"
    "/SourceConnectionString:$ConnectionString"
)

# Execute SQLPackage
try {
    Write-Output "Executing sqlpackage to export database $DatabaseName..."

    & $SqlPackagePath $Arguments 2>&1

    # Now, check for the exit code of the last command
    if ($LASTEXITCODE -ne 0) {
        throw "sqlpackage reported a non-zero exit code."
    }

    # Check file was written by getting its size
    $fileInfo = Get-Item -Path "$localFilePath" -ErrorAction Stop
    if ($fileInfo.Length -eq 0) {
        throw "The backup file was created but is empty."
    }
    Write-Output "Created backup file: $localFilePath with size $($fileInfo.Length) bytes."
}
catch {
    Write-Error "Error: Failed to create SQL Backup File. Error: $($_.Exception.Message)"
    exit 1
}


# Upload the blob:
# Cannot use Set-AzStorageBlobContent with System Assigned Managed Identity as it is not possible to pass in the correct identity to use.
# Also earlier approach of using REST api failed as the upload file was larger than the max size for a single PUT operation (5GiB).
# However, AzCopy appears to work fine with Managed Identity so we will use that.

# Construct the AzCopy destination URI.
$destinationUrl = "https://$StorageAccountName.blob.core.windows.net/$ContainerName/$BackupFileName"

# Upload file to Blob storage
Write-Output "Uploading backup file to Azure Blob Storage: $destinationUrl"

try {
    # Set up required environment variables for AzCopy
    $env:AZCOPY_AUTO_LOGIN_TYPE = "MSI"
    $env:AZCOPY_MSI_CLIENT_ID = $ManagedIdentityClientId
    $env:AZCOPY_LOG_LOCATION = "/tmp"
    $env:AZCOPY_JOB_PLAN_LOCATION = "/tmp"

    # Use the `copy` command with the `--identity` and `--identity-client-id` flags.
    azcopy copy $localFilePath $destinationUrl --log-level=INFO *>&1

    # Check the exit code of the last command (AzCopy).
    if ($LASTEXITCODE -ne 0) {
        throw "AzCopy failed to upload the file. Exit code: $LASTEXITCODE."
    }

    Write-Output "AzCopy upload complete: $destinationUrl"
}
catch {
    Write-Error "Error: Failed to upload file using AzCopy. Error: $($_.Exception.Message)"
    exit 1
}
finally {
    # Ensure the local file is removed even if the upload fails
    Remove-Item "$localFilePath" -Force
    Write-Output "Cleaned up local temporary file."
}
