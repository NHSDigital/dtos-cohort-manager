FROM mcr.microsoft.com/dotnet/sdk:9.0 AS installer-env

COPY ./Shared /Shared
RUN cd /Shared/DataServices.Migrations && \
dotnet restore
RUN cd /Shared/DataServices.Migrations && dotnet publish -o /out
WORKDIR /out

ENTRYPOINT ["dotnet", "DataServices.Migrations.dll"]
