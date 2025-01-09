namespace Model;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class HigherRiskReferralReasonLkp
{
    [Key]
    [Column("HIGHER_RISK_REFERRAL_REASON_ID")]
    public int HigherRiskReferralReasonId { get; set; }

    [Column("HIGHER_RISK_REFERRAL_REASON_CODE")]
    public string? HigherRiskReferralReasonCode { get; set; }

    [Column("HIGHER_RISK_REFERRAL_REASON_CODE_DESCRIPTION")]
    public string? HigherRiskReferralReasonCodeDescription { get; set; }
}
