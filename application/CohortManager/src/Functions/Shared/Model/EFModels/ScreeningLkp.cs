namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ScreeningLkp
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("SCREENING_ID")]
    public Int64 ScreeningId {get; set;}
    
    [Column("SCREENING_NAME")]
    public string ScreeningName {get; set;}
    
    [Column("SCREENING_TYPE")]
    public string ScreeningType {get; set;}
    
    [Column("SCREENING_ACRONYM")]
    public string ScreeningAcronym {get; set;}
    
    [Column("SCREENING_WORKFLOW_ID")]
    public string ScreeningWorkflowId {get; set;}
}
