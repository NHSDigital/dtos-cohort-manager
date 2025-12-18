# CaasIntegration API Documentation

This documentation covers two Azure Functions services that handle CAAS (Clinical Audit & Analytics Service) file integration.

---

## RetrieveMeshFile Service

### RetrieveMeshFile Overview

Service that polls the MESH mailbox for incoming CAAS files and transfers them to Azure Blob Storage for processing.

**Function App URL:** <https://env-uks-retrieve-mesh-file.azurewebsites.net>

### RetrieveMeshFile Functions

#### 1. **Timer Trigger** `RetrieveMeshFile`

**Type:** Background timer-triggered function (not an HTTP endpoint)

**Purpose:** Polls the MESH mailbox every 5 minutes for new files and transfers them to Azure Blob Storage

**Trigger Schedule:** `0 */5 * * * *` (every 5 minutes)

**Behavior:**

- Connects to MESH mailbox using configured credentials and certificates
- Retrieves list of messages from mailbox
- Downloads files from MESH
- Uploads files to Azure Blob Storage container `inbound`
- Files are named: `{MessageId}_-_{WorkflowID}.parquet`
- Performs MESH handshake every 23 hours 54 minutes
- Stores handshake state in blob storage (`config/MeshState.json`)

**Configuration Requirements:**

- `BSSMailBox` - MESH mailbox ID
- `MeshPassword` - MESH mailbox password
- `MeshSharedKey` - MESH shared key
- `MeshApiBaseUrl` - MESH API endpoint
- `caasfolder_STORAGE` - Blob storage connection string
- Certificate authentication (from Key Vault or local file)

**Error Handling:**

- Logs errors if file transfer fails
- Does not throw exceptions (continues on next timer trigger)

**Location:** RetrieveMeshFile.cs:43

---

## ReceiveCaasFile Service

### ReceiveCaasFile Overview

Service that processes CAAS Parquet files from blob storage and loads participant data into the system.

**Function App URL:** <https://env-uks-receive-caas-file.azurewebsites.net>

### ReceiveCaasFile Functions

#### 1. **Blob Trigger** `ReceiveCaasFile`

**Type:** Background blob-triggered function (not an HTTP endpoint)

**Purpose:** Processes Parquet files containing participant data when they arrive in blob storage

**Trigger:** Blob creation in container `inbound` with connection `caasfolder_STORAGE`

**Blob Path Pattern:** `inbound/{name}`

**File Format:** Parquet file with participant records

**File Naming Convention:**

- Must contain screening workflow ID
- Format parsed by `FileNameParser`
- Example: `{MessageId}_-_{WorkflowID}.parquet`

**Processing Flow:**

1. Validates file name format
2. Extracts screening workflow ID from filename
3. Retrieves screening service configuration from database
4. Downloads Parquet file from blob to temp storage
5. Reads Parquet file row groups sequentially
6. Processes records in configurable batch sizes (default: 5000)
7. Batches are processed in parallel (up to processor count)
8. Each batch calls demographic processing functions
9. Cleans up temp files after processing

**Batch Processing:**

- Configurable batch size via `BatchSize` configuration
- Recommended: 5000 records per batch for optimal performance
- Parallel processing: `MaxDegreeOfParallelism = Environment.ProcessorCount`

**Error Handling:**

- Invalid file names throw `ArgumentException`
- Unknown screening workflow IDs throw `ArgumentException`
- System exceptions are logged to exception handler
- Failed files are moved to poison blob container
- Temp files always cleaned up in finally block

**Configuration Requirements:**

- `BatchSize` - Number of records per batch (recommended: 5000)
- `caasfolder_STORAGE` - Blob storage connection string
- `inboundBlobName` - Name of inbound blob container
- `DemographicDataServiceURL` - Demographic data service URL
- `ScreeningLkpDataServiceURL` - Screening lookup data service URL
- `ServiceBusConnectionString_client_internal` - Service Bus connection

**Dependencies:**

- Database connection for screening lookup
- Demographic data service
- Service Bus for queuing
- Blob storage for file operations

**Location:** receiveCaasFile.cs:42

---

## Architecture Flow

### Complete CAAS File Processing Flow

1. **MESH Polling (RetrieveMeshFile)**
   - Timer triggers every 5 minutes
   - Checks MESH mailbox for new messages
   - Downloads Parquet files from MESH
   - Uploads to blob storage `inbound` container

2. **File Processing (receiveCaasFile)**
   - Blob trigger activates on new file in `inbound`
   - Validates file name and extracts screening workflow
   - Downloads file to temp storage
   - Reads Parquet file row groups
   - Splits into configurable batches (5000 records)
   - Processes batches in parallel
   - Sends to demographic processing
   - Cleans up temp files

3. **Error Handling**
   - Invalid files moved to poison container
   - Errors logged to exception handler
   - System continues processing next files

---

## Monitoring & Observability

### Logging

Both services use structured logging with Application Insights integration:

- File processing progress
- MESH handshake timing
- Error details
- Performance metrics

---

## Configuration Summary

### RetrieveMeshFile Configuration

```json
{
  "BSSMailBox": "MESH mailbox ID",
  "MeshPassword": "MESH password",
  "MeshSharedKey": "MESH shared key",
  "MeshApiBaseUrl": "https://mesh-api-url",
  "caasfolder_STORAGE": "blob connection string",
  "KeyVaultConnectionString": "https://keyvault-url",
  "MeshKeyName": "certificate name",
  "MeshCertName": "MESH CA cert name",
  "BypassServerCertificateValidation": false
}
```

### ReceiveCaasFile Configuration

```json
{
  "BatchSize": 5000,
  "caasfolder_STORAGE": "blob connection string",
  "inboundBlobName": "inbound",
  "DemographicDataServiceURL": "https://demographic-service-url",
  "ScreeningLkpDataServiceURL": "https://screening-lookup-url",
  "ServiceBusConnectionString_client_internal": "service bus connection"
}
```
