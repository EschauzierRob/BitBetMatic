using System;
using System.ComponentModel.DataAnnotations;

public class IndicatorThresholdsEntity : IndicatorThresholds
{
    [Key]
    public int Id { get; set; }

    // Strategie gerelateerde informatie
    [Required]
    public string Strategy { get; set; }

    [Required]
    public string Market { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Optioneel: versieveld
    public string Version { get; set; }
}
