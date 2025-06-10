# Cohort Manage Database Management Guide

1. Create the new tables / objects in your local database
2. Update the DbContext, Mappings and create or amend the relevant classes in the EF Models folder to include the changes.
3. Run the below Script from the DataServices.MigrationsFolder to generate the migration Scripts
`dotnet ef migrations add <MigrationName>`
The name of the migration should be relevant to the changes made
4. This should add two files to the Migrations Folder.
5. Once this is done this can be tested by removing the local changes on your database and running the migration by running `dotnet run` from the dataServices.Migrations Folder
