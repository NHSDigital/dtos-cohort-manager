FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

COPY ./Shared /Shared
WORKDIR /Shared

RUN mkdir -p /home/site/wwwroot && \
    dotnet publish ./Common/Common.csproj --output /home/site/wwwroot && \
    dotnet publish ./Model/Model.csproj --output /home/site/wwwroot && \
    dotnet publish ./Data/Data.csproj --output /home/site/wwwroot && \
    dotnet publish ./Utilities/Utilities.csproj --output /home/site/wwwroot && \
    dotnet publish ./DataServices.Client/DataServices.Client.csproj --output /home/site/wwwroot && \
    dotnet publish ./DataServices.Core/DataServices.Core.csproj --output /home/site/wwwroot && \
    dotnet publish ./DataServices.Database/DataServices.Database.csproj --output /home/site/wwwroot

FROM base AS function

COPY ./screeningDataServices/ExceptionManagementDataService /src/dotnet-function-app
WORKDIR /src/dotnet-function-app

RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet publish *.csproj --output /home/site/wwwroot

# To enable ssh & remote debugging on app service change the base image to the one below
# FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0-appservice
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

COPY --from=function ["/home/site/wwwroot", "/home/site/wwwroot"]
