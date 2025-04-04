namespace DataServices.Migrations;

using System.ComponentModel.DataAnnotations;

public class DatabaseConfig
{
    [Required]
    public string DtOsDatabaseConnectionString {get;set;}
    public string? SQL_IDENTITY_CLIENT_ID {get;set;}
}
