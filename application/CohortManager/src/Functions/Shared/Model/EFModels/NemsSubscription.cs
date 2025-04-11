namespace Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

public class NemsSubscription
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [Column("SUBSCRIPTION_ID")]
    public long SubscriptionId { get; set; }

    [Required]
    [Column("NHS_NUMBER")]
    public long NhsNumber { get; set; }
}