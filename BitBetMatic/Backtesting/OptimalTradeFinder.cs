using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using BitBetMatic;
using BitBetMatic.API;
using Newtonsoft.Json;

public class OptimalTradeFinder
{
    private const decimal SlippageFactor = 0.001m; // 0.1% slippage op buy/sell prijzen
    private const decimal MinProfitPercentage = 1m; // Minimale winstpercentage van 1%

    public const string MODEL_PATH = "C:/Code/Crypto/BitBetMatic/ML_models/trade_seq_model.zip";

    public static (decimal maxProfit, List<(int buy, int sell)>) MaxProfit(int maxTransactions, List<FlaggedQuote> candles)
    {
        if (candles == null || candles.Count < 2 || maxTransactions < 1)
            return (0, new List<(int, int)>());

        int n = candles.Count;
        decimal[,] dp = new decimal[maxTransactions + 1, n];
        int[,] buyIndex = new int[maxTransactions + 1, n];

        for (int k = 1; k <= maxTransactions; k++)
        {
            decimal maxDiff = -GetBuyPrice(candles[0]);
            int tempBuyIndex = 0;
            for (int t = 1; t < n; t++)
            {
                decimal sellPrice = GetSellPrice(candles[t]);
                if (sellPrice + maxDiff > dp[k, t - 1])
                {
                    dp[k, t] = sellPrice + maxDiff;
                    buyIndex[k, t] = tempBuyIndex;
                }
                else
                {
                    dp[k, t] = dp[k, t - 1];
                    buyIndex[k, t] = buyIndex[k, t - 1];
                }

                decimal buyPrice = GetBuyPrice(candles[t]);
                if (dp[k - 1, t] - buyPrice > maxDiff)
                {
                    maxDiff = dp[k - 1, t] - buyPrice;
                    tempBuyIndex = t;
                }
            }
        }

        decimal maxProfit = dp[maxTransactions, n - 1];
        List<(int buy, int sell)> transactions = new List<(int, int)>();

        int remainingTransactions = maxTransactions;
        int tIndex = n - 1;
        while (remainingTransactions > 0 && tIndex > 0)
        {
            int sellIndex = tIndex;
            int buyIdx = buyIndex[remainingTransactions, sellIndex];
            if (buyIdx < sellIndex && ProfitPercentage(GetBuyPrice(candles[buyIdx]), GetSellPrice(candles[sellIndex])) > MinProfitPercentage)
            {
                transactions.Add((buyIdx, sellIndex));
                remainingTransactions--;
            }
            tIndex = buyIdx - 1;
        }

        transactions.Reverse();
        return (maxProfit, transactions);
    }

    private static decimal GetBuyPrice(FlaggedQuote candle)
    {
        return Math.Max(candle.Low, candle.Close * (1 + SlippageFactor));
    }

    private static decimal GetSellPrice(FlaggedQuote candle)
    {
        return Math.Min(candle.High, candle.Close * (1 - SlippageFactor));
    }

    private static decimal ProfitPercentage(decimal buy, decimal sell)
    {
        return ((sell - buy) / buy) * 100;
    }

    public string Run(IApiWrapper api)
    {
        var candles = api.GetCandleData("BTC-EUR", "15m", 1440, new DateTime(2023, 06, 01), new DateTime(2025, 05, 01)).Result.Select(x => new FlaggedQuote(x)).ToList();

        const int lookahead = 20; // aantal candles in de toekomst om naar te kijken
        const decimal minProfitPercentage = 1m; // minimum winstpercentage voor een buy

        bool inPosition = false;
        int lastSellIndex = -1;

        for (int i = 0; i < candles.Count - lookahead; i++)
        {
            if (inPosition || candles[i].TradeAction != BuySellHold.Hold)
                continue;

            var buyPrice = GetBuyPrice(candles[i]);
            int bestSellIndex = -1;
            decimal bestProfit = 0;

            for (int j = 1; j <= lookahead; j++)
            {
                int sellIndex = i + j;
                if (sellIndex >= candles.Count)
                    break;

                if (candles[sellIndex].TradeAction != BuySellHold.Hold)
                    continue;

                var sellPrice = GetSellPrice(candles[sellIndex]);
                var profitPct = ProfitPercentage(buyPrice, sellPrice);

                if (profitPct > bestProfit)
                {
                    bestProfit = profitPct;
                    bestSellIndex = sellIndex;
                }
            }

            if (bestProfit >= minProfitPercentage && bestSellIndex > i)
            {
                candles[i].TradeAction = BuySellHold.Buy;
                candles[bestSellIndex].TradeAction = BuySellHold.Sell;

                // overslaan tot na deze sell
                i = bestSellIndex;
                inPosition = false;
                lastSellIndex = bestSellIndex;
            }
        }

        // optioneel: debug output
        var buys = candles.Count(c => c.TradeAction == BuySellHold.Buy);
        var sells = candles.Count(c => c.TradeAction == BuySellHold.Sell);

        var trainer = new MLTradeModelSequenceTrainer();
        var mlData = trainer.PrepareFromFlaggedQuotes(candles);
        trainer.TrainModel(mlData, MODEL_PATH);

        var serializerSettings = new JsonSerializerSettings()
        {
            StringEscapeHandling = StringEscapeHandling.EscapeHtml
        };

        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"Glijdend venster-labeling: {buys} buys, {sells} sells");
        sb.AppendLine(HttpUtility.HtmlEncode(JsonConvert.SerializeObject(candles, serializerSettings)));

        return sb.ToString();
    }
}