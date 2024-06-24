namespace BitBetMatic
{
    
    public class Decision
    {
        public Decision()
        {
            Outcome = BuySellHold.Inconclusive;
            Text = "";
        }
        public BuySellHold Outcome { get; set; }
        public string Text { get; set; }
    }
}