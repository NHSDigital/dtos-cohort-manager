namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ExcludedSMULookup
{
    [Key]
    [Column("GP_PRACTICE_CODE")]
    public string GpPracticeCode {get;set;}
}
