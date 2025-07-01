
# 🧪 Testing `ManageNemsSubscription` Locally

This guide outlines how to run and test the `ManageNemsSubscription` Azure Function locally, using either Docker Compose or a direct function host (`func start`). It also explains how to configure certificates, prepare settings, and send manual requests to NEMS.

---

## ⚙️ 1. Prerequisites

Before testing locally, ensure you have:

- [.NET SDK 8+](https://dotnet.microsoft.com/en-us/download)
- [Azure Functions Core Tools](https://learn.microsoft.com/en-us/azure/azure-functions/functions-run-local)
- Docker or Podman (depending on your OS)
- A valid `.p12` certificate for NEMS/Spine integration
- A valid `local.settings.json` file (see below)
- The NEMS FHIR endpoint: `https://msg.intspineservices.nhs.uk/STU3`

---

## 🏗️ 2. Project Setup

You can run the project in one of two ways:

### 🐳 A. Run via Docker Compose

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
      - NemsLocalCertPath=/certs/nhs_signed_client.p12
      - NemsLocalCertPassword=Spine2Int
      ...
```

Place your `.p12` certificate at `./certs/nhs_signed_client.p12`.

> Ensure the cert is readable by Docker (e.g. `chmod a+rx certs && chmod a+r certs/*.p12`)

### 💻 B. Run Locally with `func start`

1. Navigate to the project directory:
   ```bash
   cd application/CohortManager/src/Functions/DemographicServices/ManageNemsSubscription
   ```

2. Add a valid `local.settings.json` (see below)

3. Add the `nhs_signed_client.p12` file to the root of the ManageNemsSubscription project

4. Run the function app:
   ```bash
   func start --verbose
   ```

---

## 🧾 3. Configuration – `local.settings.json`

> ⚠️ Azure Functions **does not support arrays or nested config** in `local.settings.json`. Use flat key-value strings.

### ❌ Invalid

```json
"DefaultEventTypes": ["pds-record-change-1"]
```

### ✅ Valid

```json
"DefaultEventTypes": "pds-record-change-1"
```

### ✅ Example file

```json
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ASPNETCORE_URLS": "http://*:9081",
    "NemsFhirEndpoint": "https://msg.intspineservices.nhs.uk/STU3",
    "FromAsid": "200000002527",
    "ToAsid": "200000002527",
    "OdsCode": "T8T9T",
    "MeshMailboxId": "T8T9TOT001",
    "NemsLocalCertPath": "../nhs_signed_client.p12",
    "NemsLocalCertPassword": "Spine2Int",
    "SubscriptionProfile": "https://fhir.nhs.uk/STU3/StructureDefinition/EMS-Subscription-1",
    "SubscriptionCriteria": "https://fhir.nhs.uk/Id/nhs-number",
    "DefaultEventTypes": "pds-record-change-1"
  },
  "Host": {
    "LocalHttpPort": 9081
  }
}
```

---

## 🔐 4. Certificate Setup

- Use a `.p12` client cert signed for Spine integration
- Set the path in `NemsLocalCertPath`
- Make sure Docker or your host has read access
- To debug loading:
  ```csharp
  _logger.LogInformation("Loaded cert: {0}", cert.Subject);
  ```

---

## 📤 5. Manually Calling NEMS with `curl`

### 🧪 Constructing a JWT (no alg!)

NEMS requires a very specific format:

**Header**

```json
{ "alg": "none", "typ": "JWT" }
```

**Payload**

```json
{
  "iss": "https://nems.nhs.uk",
  "sub": "https://fhir.nhs.uk/Id/accredited-system|200000002527",
  "aud": "https://msg.intspineservices.nhs.uk",
  "exp": 1750421408,
  "iat": 1750417808,
  "reason_for_request": "directcare",
  "scope": "patient/Subscription.write",
  "requesting_system": "https://fhir.nhs.uk/Id/accredited-system|200000002527"
}
```

You can base64-encode the header and payload manually (no signature).

---

### 📬 Sending the request

```bash
curl -X POST "https://msg.intspineservices.nhs.uk/STU3/Subscription"   --cert nhs_signed_client.crt   --key client.key   --insecure   -H "Authorization: Bearer <your-jwt-here>"   -H "fromASID: 200000002527"   -H "toASID: 200000002527"   -H "InteractionID: urn:nhs:names:services:clinicals-sync:SubscriptionsApiPost"   -H "Content-Type: application/fhir+json"   -d @subscription.json
```

Where `subscription.json` contains a valid FHIR STU3 Subscription resource.

---

## 🧰 6. Debug Tips

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

---

Happy subscribing! 📨

