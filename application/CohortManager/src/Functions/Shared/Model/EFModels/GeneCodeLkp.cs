namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class GeneCodeLkp
{
    [Key]
    [Column("GENE_CODE_ID")]
    public Int32 GeneCodeId { get; set; }

    [Column("GENE_CODE")]
    public string GeneCode { get; set; }

    [Column("GENE_CODE_DESCRIPTION")]
    public string GeneCodeDescription { get; set; }
}
