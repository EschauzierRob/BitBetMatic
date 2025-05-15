using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BitBetMatic.API;
using BitBetMatic;
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

        int maxTransactions = candles.Count / 10;
        var (maxProfit, transactions) = MaxProfit(maxTransactions, candles);

        var initialValue = 300m;
        var summedPercentage = 100m;

        var sb = new StringBuilder();
        sb.AppendLine("Maximum achievable profit: " + maxProfit);
        sb.AppendLine("Optimal transactions:");
        foreach (var (buy, sell) in transactions)
        {
            candles[buy].TradeAction = BuySellHold.Buy;
            candles[sell].TradeAction = BuySellHold.Sell;

            decimal profit = candles[sell].Close - candles[buy].Close;
            decimal percentage = (profit / candles[buy].Close) * 100;

            summedPercentage = summedPercentage *(1+(percentage/100));

            sb.AppendLine($"Buy at index {buy} (price {candles[buy].Close}), Sell at index {sell} (price {candles[sell].Close}) -> {profit} euro ({percentage:F2}%) profit. inzet -> {initialValue*(summedPercentage/100):F2}, {summedPercentage:F2}%");
        }

        
        var trainer = new MLTradeModelSequenceTrainer();
        var mlData = trainer.PrepareFromFlaggedQuotes(candles);

        trainer.TrainModel(mlData, MODEL_PATH);

        return $"{sb}\n\n{JsonConvert.SerializeObject(candles)}";
    }
}
