FROM cohort-manager-base:latest AS base

COPY ./ParticipantManagementServices/updateParticipant /src/dotnet-function-app

RUN dotnet publish *.csproj --no-dependencies --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=base ["/home/site/wwwroot", "/home/site/wwwroot"]
