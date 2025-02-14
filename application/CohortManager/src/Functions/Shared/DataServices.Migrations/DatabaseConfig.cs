namespace DataServices.Migrations;

using System.ComponentModel.DataAnnotations;

public class DatabaseConfig
{
    [Required]
    public string ConnectionString {get;set;}
}
