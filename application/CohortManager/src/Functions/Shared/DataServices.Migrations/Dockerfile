ARG BASE_IMAGE
FROM ${BASE_IMAGE} AS base


WORKDIR /app/Shared/DataServices.Migrations
RUN dotnet restore \
&& dotnet publish -o /out
WORKDIR /out

ENTRYPOINT ["dotnet", "DataServices.Migrations.dll"]
