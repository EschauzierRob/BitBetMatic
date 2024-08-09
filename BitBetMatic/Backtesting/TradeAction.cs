using System;
using BitBetMatic;

public class TradeAction
{
    public DateTime Timestamp { get; set; }
    public BuySellHold Action { get; set; }
    public decimal AmountInEuro { get; set; }
    public decimal CurrentTokenPrice { get; set; }
    public string Market { get; set; }
}
