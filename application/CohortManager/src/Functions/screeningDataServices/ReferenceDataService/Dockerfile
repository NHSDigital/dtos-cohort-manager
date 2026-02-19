ARG BASE_IMAGE
FROM ${BASE_IMAGE} AS function

COPY ./screeningDataServices/ReferenceDataService /app/src/dotnet-function-app
WORKDIR /app/src/dotnet-function-app

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=function ["/home/site/wwwroot", "/home/site/wwwroot"]
