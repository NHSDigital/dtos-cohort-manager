# Cohort Manage Database Management Guide

`dotnet ef migrations add InitialSetup --project ../DataServices.Database/DataServices.Database.csproj --startup-project ./DataServices.Migrations.csproj `

1 - Create the new tables / objects in your local database
2 - Update the DbContext and Mappings to include the changes
3 - Run the below Script to generate the migration Scripts
