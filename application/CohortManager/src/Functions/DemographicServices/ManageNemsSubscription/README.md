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

## 🧪 Testing

Refer to [`TESTING.md`](./TESTING.md) for setup, configuration, and curl examples.

## 🔐 Notes

Ensure you keep `nhs_signed_client.p12` safe and secure. Do not commit it unless explicitly permitted.
