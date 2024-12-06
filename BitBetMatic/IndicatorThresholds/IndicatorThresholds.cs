using System;
using System.ComponentModel.DataAnnotations;

public class IndicatorThresholds
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
    public decimal Highscore { get; set; } = 0;




    
    // Thresholds for RSI
    public int RsiOverbought { get; set; } = 70; // Default overbought threshold
    public int RsiOversold { get; set; } = 30;   // Default oversold threshold

    // Thresholds for MACD
    public decimal MacdSignalLine { get; set; } = 0m; // Default signal line crossing

    // Thresholds for ATR (Average True Range)
    public decimal AtrMultiplier { get; set; } = 1.5m; // Default multiplier for stop-loss

    // Thresholds for Moving Averages
    public int SmaShortTerm { get; set; } = 50;  // Default short-term SMA period
    public int SmaLongTerm { get; set; } = 200;  // Default long-term SMA period

    // Thresholds for Parabolic SAR
    public double ParabolicSarStep { get; set; } = 0.02d; // Default SAR step
    public double ParabolicSarMax { get; set; } = 0.2d;   // Default SAR max value

    // Add more indicators as needed
    // For example, Bollinger Bands
    public int BollingerBandsPeriod { get; set; } = 20;
    public double BollingerBandsDeviation { get; set; } = 2d;

    public double AdxStrongTrend { get; set; } = 25d; // Default multiplier for adx
    public double? StochasticOverbought { get; set; } = 80d;
    public double? StochasticOversold { get; set; } = 20d;
    public int BuyThreshold { get; set; } = 50;
    public int SellThreshold { get; set; } = -50;
    public int RsiPeriod { get; set; } = 144;
    public int AtrPeriod { get; set; } = 14;
    public int StochasticPeriod { get; set; } = 14;
    public int StochasticSignalPeriod { get; set; } = 3;
    public int MacdFastPeriod { get; set; } = 12;
    public int MacdSlowPeriod { get; set; } = 26;
    public int MacdSignalPeriod { get; set; } = 9;
    public int AdxPeriod { get; set; } = 14;
    public int RocPeriod { get; set; } = 14;
    public double? ScoreMultiplier { get; set; } = 1.0d;
}
