# 📦 ManageNemsSubscription

This Azure Function is responsible for subscribing and unsubscribing from the NHS NEMS Spine Event Service.

## 🚀 Features

- Creates and manages FHIR `Subscription` resources for patient demographic changes.
- Persists subscription metadata to a local database.
- Uses client certificate and JWT auth for Spine secure communication.
- Includes `Subscribe` and `Unsubscribe` endpoints.

## 📂 Structure

- `Program.cs` — Entry point and DI setup.
- `ManageNemsSubscription.cs` — Function endpoints.
- `NemsSubscriptionManager.cs` — Core logic.
- `ManageNemsSubscriptionConfig.cs` — Configuration class.
- `local.settings.json` — Local config values.

## 🧪 Testing Locally

This guide outlines how to run and test the `ManageNemsSubscription` Azure Function locally, using either Docker Compose or a direct function host (`func start`). It also explains how to configure certificates, prepare settings, and send manual requests to NEMS.

### ⚙️ Prerequisites

Before testing locally, ensure you have:

- [.NET SDK 8+](https://dotnet.microsoft.com/en-us/download)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Docker or Podman (depending on your OS)
- A valid `.pfx` certificate for NEMS/Spine integration
- A valid `local.settings.json` file (see below)
- The NEMS FHIR endpoint: `https://msg.intspineservices.nhs.uk/STU3`

### 🏗️ Project Setup

You can run the project in one of two ways:

#### 🐳 A. Run via Docker Compose

This service has been added to our `compose.core.yaml` file:

```yaml
  manage-nems-subscription:
    image: cohort-manager-manage-nems-subscription
    build:
      context: ./src/Functions/
      dockerfile: DemographicServices/ManageNemsSubscription/Dockerfile
    volumes:
      - ./certs:/certs
    ports:
      - "9081:9081"
    environment:
      - ASPNETCORE_URLS=http://*:9081
      - FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
      - ManageNemsSubscription__NemsLocalCertPath=/certs/nhs_signed_client.pfx
      - ManageNemsSubscription__NemsLocalCertPassword=${NEMS_CERT_PASSWORD}
      ...
```

Place your `.pfx` certificate at `./certs/nhs_signed_client.pfx`.

> Ensure the cert is readable by Docker (e.g. `chmod a+rx certs && chmod a+r certs/*.pfx`)

#### 💻 B. Run Locally with `func start`

1. Navigate to the project directory:

   ```bash
   cd application/CohortManager/src/Functions/DemographicServices/ManageNemsSubscription
   ```

2. Add a valid `local.settings.json` (see below)

3. Add the `nhs_signed_client.pfx` file to the root of the ManageNemsSubscription project

4. Run the function app:

   ```bash
   func start --verbose
   ```

### 🧾 Configuration – `local.settings.json`

> ⚠️ Azure Functions **does not support arrays or nested config** in `local.settings.json`. Use flat key-value strings.

#### 🔧 Configuration Structure

This function uses a nested configuration structure to clearly separate NEMS-specific settings from other MESH integrations. All NEMS-specific configuration values use the `ManageNemsSubscription__` prefix (double underscore), which maps to the nested `ManageNemsSubscription` object in the configuration class.

**Example mapping:**
- `ManageNemsSubscription__NemsFhirEndpoint` → `config.ManageNemsSubscription.NemsFhirEndpoint`
- `ManageNemsSubscription__MeshMailboxId` → `config.ManageNemsSubscription.MeshMailboxId`

This approach ensures clear separation in Key Vault and makes it obvious which settings belong to ManageNemsSubscription versus other MESH-enabled functions.

#### ❌ Invalid

```json
"DefaultEventTypes": ["pds-record-change-1"]
```

#### ✅ Valid

```json
"DefaultEventTypes": "pds-record-change-1"
```

#### ✅ Example file

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ASPNETCORE_URLS": "http://*:9081",
    "ManageNemsSubscription__NemsFhirEndpoint": "https://msg.intspineservices.nhs.uk/STU3",
    "ManageNemsSubscription__FromAsid": "<your-from-asid>",
    "ManageNemsSubscription__ToAsid": "<your-to-asid>",
    "ManageNemsSubscription__OdsCode": "<your-ods-code>",
    "ManageNemsSubscription__MeshMailboxId": "<your-mesh-mailbox-id>",
    "ManageNemsSubscription__NemsLocalCertPath": "./nhs_signed_client.pfx",
    "ManageNemsSubscription__NemsLocalCertPassword": "<your-certificate-password>",
    "ManageNemsSubscription__SubscriptionProfile": "https://fhir.nhs.uk/STU3/StructureDefinition/EMS-Subscription-1",
    "ManageNemsSubscription__SubscriptionCriteria": "https://fhir.nhs.uk/Id/nhs-number",
    "ManageNemsSubscription__DefaultEventTypes__0": "pds-record-change-1",
    "ManageNemsSubscription__BypassServerCertificateValidation": "true"
  },
  "Host": {
    "LocalHttpPort": 9081
  }
}
```

### 🔐 Certificate Setup

- Use a `.pfx` client cert signed for Spine integration
- Set the path in `NemsLocalCertPath`
- Make sure Docker or your host has read access
- To debug loading:

  ```csharp
  _logger.LogInformation("Loaded cert: {0}", cert.Subject);
  ```

### 📤 Manually Calling NEMS with `curl`

#### 🧪 Constructing a JWT (no alg!)

NEMS requires a very specific format:

##### Header

```json
{ "alg": "none", "typ": "JWT" }
```

##### Payload

```json
{
  "iss": "https://nems.nhs.uk",
  "sub": "https://fhir.nhs.uk/Id/accredited-system|<your-asid>",
  "aud": "https://msg.intspineservices.nhs.uk",
  "exp": 1750421408,
  "iat": 1750417808,
  "reason_for_request": "directcare",
  "scope": "patient/Subscription.write",
  "requesting_system": "https://fhir.nhs.uk/Id/accredited-system|<your-asid>"
}
```

You can base64-encode the header and payload manually (no signature).

#### 📬 Sending the request

```bash
curl -X POST "https://msg.intspineservices.nhs.uk/STU3/Subscription"   --cert nhs_signed_client.crt   --key client.key   --insecure   -H "Authorization: Bearer <your-jwt-here>"   -H "fromASID: <your-from-asid>"   -H "toASID: <your-to-asid>"   -H "InteractionID: urn:nhs:names:services:clinicals-sync:SubscriptionsApiPost"   -H "Content-Type: application/fhir+json"   -d @subscription.json
```

Where `subscription.json` contains a valid FHIR STU3 Subscription resource.

### 🧰 Debug Tips

- If config appears empty:
  - Check for complex types in `local.settings.json`
  - Run `dotnet clean && dotnet build`
- If cert fails to load:
  - Confirm path and file access
  - Log thumbprint or subject to confirm load
- Use verbose logs to verify environment variables in Docker:

  ```dockerfile
  RUN echo $NemsLocalCertPath
  ```

## 🔐 Notes

Ensure you keep `nhs_signed_client.pfx` safe and secure. Do not commit it unless explicitly permitted.

## 🔧 Additional Configuration Notes

### Certificate File Requirements

The function requires the certificate file to be included in the build output:

1. **For local development**: Place `nhs_signed_client.pfx` in the function directory
2. **For Docker**: Copy the certificate to `./certs/nhs_signed_client.pfx` 
3. **Project file**: The `.csproj` must include the certificate for build output:

```xml
<ItemGroup>
  <None Update="nhs_signed_client.pfx">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
</ItemGroup>
```

### Environment Variable Format

- **Local development**: Use double underscores (`__`) in `local.settings.json`
- **Docker**: Use double underscores (`__`) in environment variables
- **Azure Key Vault**: Use double dashes (`--`) for key names (e.g., `ManageNemsSubscription--NemsFhirEndpoint`)
- **Arrays**: Use indexed syntax like `ManageNemsSubscription__DefaultEventTypes__0`

> ⚠️ **Important**: The configuration system automatically maps between Key Vault's double dash (`--`) format and the application's double underscore (`__`) format. Don't mix these formats in the same environment.
