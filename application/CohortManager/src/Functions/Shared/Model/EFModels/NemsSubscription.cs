namespace Model;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class NemsSubscription
{
    [Key]
    [Column("SUBSCRIPTION_ID", TypeName = "nvarchar(450)")]
    public required string SubscriptionId { get; set; }

    [Required]
    [Column("NHS_NUMBER", TypeName = "bigint")]
    public long NhsNumber { get; set; }

    [Column("RECORD_INSERT_DATETIME", TypeName = "datetime")]
    public DateTime? RecordInsertDateTime { get; set; }

    [Column("RECORD_UPDATE_DATETIME", TypeName = "datetime")]
    public DateTime? RecordUpdateDateTime { get; set; }
}
