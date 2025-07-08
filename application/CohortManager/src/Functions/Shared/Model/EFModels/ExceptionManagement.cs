namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class ExceptionManagement
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("EXCEPTION_ID")]
    public int ExceptionId { get; set; }

    [MaxLength(250)]
    [Column("FILE_NAME")]
    public string? FileName { get; set; }

    [MaxLength(50)]
    [Column("NHS_NUMBER")]
    public string? NhsNumber { get; set; }

    [Column("DATE_CREATED", TypeName = "datetime")]
    public DateTime? DateCreated { get; set; }

    [Column("DATE_RESOLVED", TypeName = "date")]
    public DateTime? DateResolved { get; set; }

    [Column("RULE_ID")]
    public int? RuleId { get; set; }

    [Column("RULE_DESCRIPTION")]
    public string? RuleDescription { get; set; }

    [Column("ERROR_RECORD")]
    public string? ErrorRecord { get; set; }

    [Column("CATEGORY")]
    public int? Category { get; set; }

    [MaxLength(100)]
    [Column("SCREENING_NAME")]
    public string? ScreeningName { get; set; }

    [Column("EXCEPTION_DATE", TypeName = "datetime")]
    public DateTime? ExceptionDate { get; set; }

    [MaxLength(100)]
    [Column("COHORT_NAME")]
    public string? CohortName { get; set; }

    [Column("IS_FATAL")]
    public short? IsFatal { get; set; }

    [MaxLength(50)]
    [Column("SERVICENOW_ID")]
    public string? ServiceNowId { get; set; }

    [Column("SERVICENOW_CREATED_DATE", TypeName = "date")]
    public DateTime? ServiceNowCreatedDate { get; set; }

    [Column("RECORD_UPDATED_DATE", TypeName = "datetime")]
    public DateTime? RecordUpdatedDate { get; set; }

    public ValidationException ToValidationException()
    {
        return new ValidationException
        {
            ExceptionId = ExceptionId,
            FileName = FileName,
            NhsNumber = NhsNumber,
            DateCreated = DateCreated,
            DateResolved = DateResolved,
            RuleId = RuleId,
            RuleDescription = RuleDescription,
            ErrorRecord = ErrorRecord,
            Category = Category,
            ScreeningName = ScreeningName,
            ExceptionDate = ExceptionDate,
            CohortName = CohortName,
            Fatal = IsFatal,
            ServiceNowId = ServiceNowId,
            ServiceNowCreatedDate = ServiceNowCreatedDate,
            RecordUpdatedDate = RecordUpdatedDate
        };
    }

    public ExceptionManagement FromValidationException(ValidationException validationException)
    {
        string input = validationException.Fatal.ToString();
        return new ExceptionManagement
        {
            ExceptionId = validationException.ExceptionId,
            FileName = validationException.FileName,
            NhsNumber = validationException.NhsNumber,
            DateCreated = validationException.DateCreated ?? DateTime.MaxValue,
            DateResolved = validationException.DateResolved ?? DateTime.MaxValue,
            RuleId = validationException.RuleId,
            RuleDescription = validationException.RuleDescription,
            ErrorRecord = validationException.ErrorRecord,
            Category = validationException.Category,
            ScreeningName = validationException.ScreeningName,
            ExceptionDate = validationException.ExceptionDate ?? DateTime.UtcNow,
            CohortName = validationException.CohortName,
            IsFatal = short.TryParse(input, out short result) ? result : new short(),
            ServiceNowId = ServiceNowId,
            ServiceNowCreatedDate = validationException.ServiceNowCreatedDate,
            RecordUpdatedDate = validationException.RecordUpdatedDate
        };
    }
}
