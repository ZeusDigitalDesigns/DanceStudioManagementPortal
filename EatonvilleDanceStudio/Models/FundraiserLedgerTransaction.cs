using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EatonvilleDanceStudio.Models;

namespace EatonvilleDanceStudio.Models;

/// <summary>
/// Immutable audit log entry for a fundraiser credit applied
/// directly to a specific dancer's account balance.
/// </summary>
public class FundraiserLedgerTransaction
{
    [Key]
    public int Id { get; init; }

    [Required]
    public int FundraiserEventId { get; init; }

    [ForeignKey(nameof(FundraiserEventId))]
    public FundraiserEvent? FundraiserEvent { get; init; }

    [Required]
    public int DancerId { get; init; }

    [ForeignKey(nameof(DancerId))]
    public Dancer? Dancer { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CreditAmount { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PreviousBalance { get; init; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal NewBalance { get; init; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    [Required]
    public string RecordedByDirectorId { get; init; } = string.Empty;
}