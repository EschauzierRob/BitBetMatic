using System.Collections.Generic;
using System.Linq;

namespace BitBetMatic
{
    public class Decisions : Dictionary<string, Decision>
    {
        public const string RsiAndMomentum = "RsiAndMomentum";
        public const string MeanReversionAndMomentum = "MeanReversionAndMomentum";
        public const string Macd = "Macd";
        public const string Bollinger = "Bollinger";
        public Decisions()
        {
            Add(RsiAndMomentum, new Decision());
            Add(MeanReversionAndMomentum, new Decision());
            Add(Macd, new Decision());
            Add(Bollinger, new Decision());
        }

        public List<Decision> GetOutcomeArguments()
        {
            int buysCount = Buys().Count;
            int sellsCount = Sells().Count;
            int holdsCount = Holds().Count;

            if (buysCount > sellsCount && buysCount > holdsCount) { return Buys(); }
            else if (sellsCount > buysCount && sellsCount > holdsCount) { return Sells(); }
            return Holds();
        }

        public BuySellHold GetOutcome() => GetOutcomeArguments().First().Outcome;

        public List<Decision> Buys() => this.Where(x => x.Value.Outcome == BuySellHold.Buy).Select(x => x.Value).ToList();


        public List<Decision> Sells() => this.Where(x => x.Value.Outcome == BuySellHold.Sell).Select(x => x.Value).ToList();

        public List<Decision> Holds() => this.Where(x => x.Value.Outcome == BuySellHold.Hold).Select(x => x.Value).ToList();
    }
}