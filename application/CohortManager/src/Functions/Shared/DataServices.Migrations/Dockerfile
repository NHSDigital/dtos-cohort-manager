FROM cohortmanager/dotnet-base:0.1 AS base


WORKDIR /app/Shared/DataServices.Migrations
RUN dotnet restore \
&& dotnet publish -o /out
WORKDIR /out

ENTRYPOINT ["dotnet", "DataServices.Migrations.dll"]
