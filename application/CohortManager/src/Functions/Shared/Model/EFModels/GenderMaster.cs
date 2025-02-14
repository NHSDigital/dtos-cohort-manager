namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GenderMaster
{
    [Key]
    [Column("GENDER_CD")]
    [MaxLength(2)]
    public string GenderCd {get;set;}
    [Column("GENDER_DESC")]
    [MaxLength(10)]
    public string GenderDesc {get;set;}
}
