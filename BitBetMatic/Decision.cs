namespace BitBetMatic
{
    public record Decision(BuySellHold Outcome, string Text)
    {
        public Decision() : this(BuySellHold.Inconclusive, "")
        {
        }
    }
}