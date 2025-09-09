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
    $sqlpackageOutput = & $SqlPackagePath $Arguments *>&1
    # Uncomment for debugging
    # Write-Output "sqlpackage output: $sqlpackageOutput"

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

# Upload file to Blob storage
Write-Output "Uploading $localFilePath to Blob Storage container '$ContainerName' in storage account '$StorageAccountName'."

# Upload the blob:
# Cannot use Set-AzStorageBlobContent with System Assigned Managed Identity as it is not possible to pass in the correct identity to use.
# Therefore we need to obtain a token manually and then push the file using a web request..

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

    # Prepare the blob upload
    $blobUrl = "https://$StorageAccountName.blob.core.windows.net/$ContainerName/$BackupFileName"
    Write-Output "Uploading to: $blobUrl"

    # Define the headers for the PUT request to Azure Storage
    $uploadHeaders = @{
        "Authorization" = "Bearer $accessToken"
        "x-ms-blob-type" = "BlockBlob"
        "x-ms-version" = "2021-08-06"
    }

    try {
        # Upload the blob via REST API
        $uploadResult = Invoke-WebRequest -Method PUT -Uri $blobUrl -Headers $uploadHeaders -InFile $localFilePath -ContentType "application/octet-stream" -TimeoutSec 300 -SkipHttpErrorCheck
    
        if ($uploadResult.StatusCode -ne 201) {
            throw "Failed to upload blob to Azure Storage. HTTP Status Code: $($uploadResult.StatusCode). Response Body: $($uploadResult.Content)"
        }
    }
    catch {
        throw "Failed to upload blob to Azure Storage. Error: $($_.Exception.Message)"
    }

    Write-Output "Upload complete: $blobUrl"
} catch {
    # This catch block will only be triggered for network-level errors such as DNS lookup failure or a connection timeout, not for HTTP status errors.
    Write-Error "Error: An unexpected network or system error occurred: $($_.Exception.Message)"
    exit 1
}

# Clean up the local file after successful upload
Remove-Item "$localFilePath" -Force
Write-Output "Cleaned up local temporary file."

# SQL permissions required:
# CREATE USER [mi-prod-uks-cohman-db-backup] FROM EXTERNAL PROVIDER;
# ALTER ROLE db_datareader ADD MEMBER [mi-prod-uks-cohman-db-backup];
# GRANT VIEW DEFINITION TO [mi-prod-uks-cohman-db-backup];
# GRANT VIEW DATABASE STATE TO [mi-prod-uks-cohman-db-backup];
