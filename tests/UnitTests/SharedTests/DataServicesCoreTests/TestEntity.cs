namespace NHS.CohortManager.Tests.Shared;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class TestEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("PARTICIPANT_ID")]
    public Int64 Id { get; set; }
    [Column("NHS_NUMBER")]
    public Int64 NHSNumber { get; set; }
    [MaxLength(10)]
    [Column("RECORD_TYPE")]
    [Required]
    public string RecordType { get; set; }
    [Column("ELIGIBILITY_FLAG")]
    public Int16 EligibilityFlag { get; set; }
    [MaxLength(10)]
    [Column("REASON_FOR_REMOVAL")]
    public string? ReasonForRemoval { get; set; }
    [Column("DATE_OF_BRITH")]
    public DateTime? DateOfBirth { get; set; }
}
