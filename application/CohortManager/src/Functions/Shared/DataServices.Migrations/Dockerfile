ARG PLATFORM_SUFFIX
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base

COPY ./Shared /Shared
WORKDIR /Shared/DataServices.Migrations
RUN dotnet restore \
&& dotnet publish -o /out
WORKDIR /out

ENTRYPOINT ["dotnet", "DataServices.Migrations.dll"]
