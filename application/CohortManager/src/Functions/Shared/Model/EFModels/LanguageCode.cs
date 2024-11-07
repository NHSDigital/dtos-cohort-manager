namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class LanguageCode
{
    [Key]
    [Column("LANGUAGE_CODE")]
    public string LanguageCodeId {get;set;}
    [Column("LANGUAGE_DESCRIPTION")]
    public string LanguageDescription {get;set;}

}
