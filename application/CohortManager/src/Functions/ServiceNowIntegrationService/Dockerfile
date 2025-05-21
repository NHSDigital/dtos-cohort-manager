FROM mcr.microsoft.com/dotnet/sdk:8.0 AS installer-env

COPY ./ServiceNowIntegrationService /dotnet-function-app
COPY ./Shared /Shared
RUN cd /dotnet-function-app && \
    mkdir -p /home/site/wwwroot && \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
# Create a non-root user and set permissions
RUN adduser --disabled-password --gecos '' nonrootuser && \
    chown -R nonrootuser:nonrootuser /home/site/wwwroot
# Switch to non-root user
USER nonrootuser

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=installer-env ["/home/site/wwwroot", "/home/site/wwwroot"]
