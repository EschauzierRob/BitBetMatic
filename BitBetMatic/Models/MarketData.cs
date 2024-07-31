namespace BitBetMatic
{
    public class Base
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int Decimals { get; set; }
        public double Volume { get; set; }
    }

    public class MarketQuote
    {
        public string Symbol { get; set; }
        public string Name { get; set; }
        public string Slug { get; set; }
        public int Decimals { get; set; }
        public double Volume { get; set; }
    }

    public class MarketData
    {
        public Base Base { get; set; }
        public MarketQuote Quote { get; set; }
        public double Price { get; set; }
        public double Change24h { get; set; }
        public double VolumeQuote { get; set; }
        public int PricePrecision { get; set; }
        public double MinOrderInBaseAsset { get; set; }
        public double MinOrderInQuoteAsset { get; set; }
        public object HighlightedAt { get; set; } // Nullable type, adjust as needed
    }

}