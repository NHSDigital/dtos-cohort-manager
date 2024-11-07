namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class CurrentPosting
{
    [Key]
    [Column("POSTING")]
    public string Posting {get;set;}
    [Column("IN_USE")]
    public string InUse{get;set;}
    [Column("INCLUDED_IN_COHORT")]
    public string IncludedInCohort {get;set;}
    [Column("POSTING_CATEGORY")]
    public string postingCategory {get;set;}
}
