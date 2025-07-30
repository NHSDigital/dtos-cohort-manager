# 📦 ManageNemsSubscription

This Azure Function is responsible for subscribing and unsubscribing from the NHS NEMS Spine Event Service.

## 🔐 Certificate Setup for Docker

Place your `.pfx` certificate at `./certs/nhs_signed_client.pfx` for Docker or in the project root for local development.

> Ensure the cert is readable by Docker (e.g. `chmod a+rx certs && chmod a+r certs/*.pfx`)


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

