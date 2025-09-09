# PowerShell script to import an Azure SQL Database from a BACPAC file in Azure Blob Storage using a Managed Identity, and restore it.
# This script is intended to run inside a Docker container with the necessary tools installed (sqlpackage, Az PowerShell module).
# It requires the following environment variables to be set:
# - SQL_SERVER_NAME: The name of the Azure SQL Server (without .database.windows.net)
# - SQL_DATABASE_NAME: The name of the database to restore the import as
# - STORAGE_ACCOUNT_NAME: The name of the Azure Storage Account
# - STORAGE_CONTAINER_NAME: The name of the Blob container to download the BACPAC file from
# - BACKUP_FILE_NAME: The name of the BACPAC file to import (including .bacpac extension)
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
$BackupFileName = $env:BACKUP_FILE_NAME
$ManagedIdentityClientId = $env:MANAGED_IDENTITY_CLIENT_ID
$TargetSubscriptionId = $env:TARGET_SUBSCRIPTION_ID

# Check if environment variables are set
if (-not $ServerName -or -not $DatabaseName) {
    Write-Error "Error: SQL_SERVER_NAME or SQL_DATABASE_NAME environment variable is not set."
    exit 1
}
elseif (-not $StorageAccountName -or -not $ContainerName -or -not $BackupFileName) {
    Write-Error "Error: STORAGE_ACCOUNT_NAME or STORAGE_CONTAINER_NAME or BACKUP_FILE_NAME environment variable is not set."
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

# Download file from Blob storage

# Download the blob:
# Cannot use Get-AzStorageBlobContent with System Assigned Managed Identity as it is not possible to pass in the correct identity to use.
# Therefore we need to obtain a token manually and then push the file using a web request..

# Define the temporary local file path
$localFilePath = "/tmp/$BackupFileName"

try {
    # Get the access token using the managed identity endpoint...
    # The IDENTITY_ENDPOINT and IDENTITY_HEADER environment variables are injected by Azure.
    $resource = "https://storage.azure.com/"
    $headers = @{
        "X-IDENTITY-HEADER" = $env:IDENTITY_HEADER
    }

    $apiVersion = "2019-08-01"
    $IdentityEndpointUri = "$($env:IDENTITY_ENDPOINT)?resource=$resource&client_id=$ManagedIdentityClientId&api-version=$apiVersion"

    # Use -SkipHttpErrorCheck to ensure the response object is not disposed, even on failure - we will check the status code ourselves.
    $tokenResponse = Invoke-WebRequest -Method GET -Uri $IdentityEndpointUri -Headers $headers -SkipHttpErrorCheck
    
    if ($tokenResponse.StatusCode -ne 200) {
        # An HTTP error occurred. Read the response content for details.
        $responseBody = $tokenResponse.Content | ConvertFrom-Json
        Write-Error "Failed to get access token from managed identity endpoint."
        Write-Error "HTTP Status Code: $($tokenResponse.StatusCode)"
        Write-Error "Response Body: $($responseBody | ConvertTo-Json -Compress)"
        exit 1
    }

    # If the status code is 200, parse the content to get the token.
    $responseContent = $tokenResponse.Content | ConvertFrom-Json
    $accessToken = $responseContent.access_token
    Write-Output "Successfully retrieved storage account access token."

    # Prepare the blob download
    $blobUrl = "https://$StorageAccountName.blob.core.windows.net/$ContainerName/$BackupFileName"
    Write-Output "Downloading $blobUrl to $localFilePath"

    # Define the headers for the PUT request to Azure Storage
    $downloadHeaders = @{
        "Authorization" = "Bearer $accessToken"
        "x-ms-version" = "2021-08-06"
    }

    # Download the blob via REST API
    try {
        Invoke-WebRequest -Method GET -Uri $blobUrl -Headers $downloadHeaders -OutFile $localFilePath -TimeoutSec 300

        if (-not (Test-Path $localFilePath) -or ((Get-Item $localFilePath).Length -eq 0)) {
            throw "Downloaded file does not exist or is empty: $localFilePath"
        }
        Write-Output "Download complete: $localFilePath"
    }
    catch {
        Write-Error "Failed to download blob from Azure Storage. Error: $($_.Exception.Message)"
        exit 1
    }
} catch {
    # This catch block will only be triggered for network-level errors such as DNS lookup failure or a connection timeout, not for HTTP status errors.
    Write-Error "Error: An unexpected network or system error occurred downloading backup file: $($_.Exception.Message)"
    exit 1
}

# Check that he file was downloaded successfully
if (-not (Test-Path $localFilePath) -or ((Get-Item $localFilePath).Length -eq 0)) {
    Write-Error "Error: Downloaded file does not exist or is empty: $localFilePath"
    exit 1
}
else {
    Write-Output "Verified that the backup file was downloaded successfully. File size: $((Get-Item $localFilePath).Length) bytes."
}

# Do the restore using sqlpackage:
# Define SQL Package path here:
$SqlPackagePath = "/opt/sqlpackage/sqlpackage"

# Use a connection string as this appears(after much trial and error) to be the only way to get sqlpackage to work with the correct User Assigned Managed Identity.
# Unfortunately the string parameters vary slightly compared to those used by the db-management container so it's easiest to compose it here rather than in the Terraform code.
$ConnectionString = "Server=$ServerName.database.windows.net;Authentication=Active Directory Managed Identity;Encrypt=True;User Id=$ManagedIdentityClientId;Initial Catalog=$DatabaseName"

# Configuration Parameters for new database:
$ServiceObjective = "S12"
$MaxSizeGB = "30"

$Arguments = @(
    "/Action:Import"
    "/SourceFile:$localFilePath"
    "/TargetConnectionString:$ConnectionString"
    "/p:DatabaseServiceObjective=$ServiceObjective"
    "/p:DatabaseMaximumSize=$MaxSizeGB"
)

# Execute SQLPackage
try {
    Write-Output "Starting database restore to $DatabaseName [sku: $ServiceObjective, size: $MaxSizeGB] using file: $localFilePath..."

    $sqlpackageOutput = & $SqlPackagePath $Arguments *>&1
    Write-Output "sqlpackage output: $sqlpackageOutput"

    # If output contains error string, throw an error
    if ($sqlpackageOutput -match "Error:") {
        throw "sqlpackage reported an error during import: $sqlpackageOutput"
    }
    Write-Output "Completed database restore successfully."

    # Clean up the local file after successful download
    Remove-Item "$localFilePath" -Force
    Write-Output "Cleaned up local temporary file."
}
catch {
    Write-Error "Error: Failed to create SQL Backup File. Error: $($_.Exception.Message)"
    exit 1
}

# SQL permissions required:
# CREATE USER [mi-prod-uks-cohman-db-backup] FROM EXTERNAL PROVIDER;
# ALTER ROLE db_datareader ADD MEMBER [mi-prod-uks-cohman-db-backup];
# GRANT VIEW DEFINITION TO [mi-prod-uks-cohman-db-backup];
# GRANT VIEW DATABASE STATE TO [mi-prod-uks-cohman-db-backup];
