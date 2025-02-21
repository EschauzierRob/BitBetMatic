using System;
using System.Collections.Generic;
using System.Linq;
using BitBetMatic;
public class ResultAnalyzer
{
    private readonly ITradingStrategy tradingStrategy;
    private readonly List<TradeAction> tradeActions;
    private readonly PortfolioManager portfolioManager;

    public ResultAnalyzer(ITradingStrategy tradingStrategy, List<TradeAction> tradeActions, PortfolioManager portfolioManager)
    {
        this.tradingStrategy = tradingStrategy;
        this.tradeActions = tradeActions;
        this.portfolioManager = portfolioManager;
    }

    private Metrics CalculateMetrics()
    {
        var portfolioValues = new List<decimal>();
        var portfolioManager = new PortfolioManager();
        portfolioManager.SetCash(300);

        foreach (var item in tradeActions.Select((action, i) => new { i, action }))
        {
            portfolioManager.ExecuteTrade(item.action);
            portfolioValues.Add(portfolioManager.GetAccountTotal());
        }

        // Sharpe Ratio
        var returns = CalculateDailyReturns(portfolioValues);
        var averageReturn = returns.Average();
        var riskFreeRate = 0.01m / 365; // Jaarlijks 1%, omgerekend naar dagelijks
        var volatility = (decimal)Math.Sqrt((double)CalculateVariance(returns));
        var sharpeRatio = volatility > 0 ? (averageReturn - riskFreeRate) / volatility : 0m;

        // Maximum Drawdown
        var maxDrawdown = CalculateMaximumDrawdown(portfolioValues);

        var profits = CalculateProfit();

        // // Profit Factor
        // decimal totalProfit = tradeActions.Where(t => t.Action == BuySellHold.Sell).Sum(t => t.AmountInEuro);
        // decimal totalLoss = tradeActions.Where(t => t.Action == BuySellHold.Buy).Sum(t => t.AmountInEuro);
        // decimal profitFactor = totalLoss != 0 ? totalProfit / totalLoss : 0;

        // // Win/Loss Ratio
        // int wins = tradeActions.Count(t => t.Action == BuySellHold.Sell);
        // int losses = tradeActions.Count(t => t.Action == BuySellHold.Buy);
        // decimal winLossRatio = losses > 0 ? (decimal)wins / losses : wins;

        return new Metrics
        {
            SharpeRatio = sharpeRatio,
            MaximumDrawdown = maxDrawdown,
            ProfitFactor = profits.profitFactor,
            WinLossRatio = profits.winLossRatio,
        };
    }

    private (decimal profitFactor, decimal winLossRatio) CalculateProfit()
    {
        // Profit and Loss Calculation
        decimal realizedProfit = 0;
        decimal unrealizedProfit = 0;
        decimal totalCost = 0; // Total amount spent on buys
        decimal totalSales = 0; // Total revenue from sells
        decimal heldQuantity = 0; // Total tokens held
        decimal heldValue = 0; // Current value of held tokens
        decimal currentPrice = tradeActions.Last().CurrentTokenPrice; // Replace with your method

        foreach (var action in tradeActions)
        {
            var quantity = action.AmountInEuro / action.CurrentTokenPrice;
            if (action.Action == BuySellHold.Buy)
            {
                // Update cost and quantity for buys
                totalCost += action.AmountInEuro;
                heldQuantity += quantity;
            }
            else if (action.Action == BuySellHold.Sell && heldQuantity > 0)
            {
                // Calculate realized profit for sells
                decimal averageBuyPrice = totalCost / heldQuantity; // Weighted average buy price
                decimal sellProfit = (quantity * action.CurrentTokenPrice) - (quantity * averageBuyPrice);
                realizedProfit += sellProfit;

                // Update total cost and held quantity
                heldQuantity -= quantity;
                totalCost -= quantity * averageBuyPrice;
                totalSales += action.AmountInEuro;
            }
        }

        heldQuantity = heldQuantity != 0 ? heldQuantity : 1;

        // Unrealized profit for open positions
        if (heldQuantity > 0)
        {
            heldValue = heldQuantity * currentPrice;
            unrealizedProfit = heldValue - totalCost;
        }

        // Profit Factor
        decimal totalProfit = realizedProfit + Math.Max(unrealizedProfit, 0);
        decimal totalLoss = Math.Abs(Math.Min(unrealizedProfit, 0));
        decimal profitFactor = totalLoss != 0 ? totalProfit / totalLoss : 0;

        // Win/Loss Ratio
        int wins = tradeActions.Count(t => t.Action == BuySellHold.Sell && t.AmountInEuro / t.CurrentTokenPrice > (totalCost / heldQuantity));
        int losses = tradeActions.Count(t => t.Action == BuySellHold.Sell && t.AmountInEuro / t.CurrentTokenPrice <= (totalCost / heldQuantity));
        decimal winLossRatio = losses > 0 ? (decimal)wins / losses : wins;


        // Debugging logs
        // foreach (var action in tradeActions.Where(a => a.AmountInEuro>0 && (a.Action==BuySellHold.Buy || a.Action==BuySellHold.Sell)))
        // {
        //     Console.WriteLine($"Action: {action.Action}, Quantity: {action.AmountInEuro / action.CurrentTokenPrice}, Price: {action.CurrentTokenPrice}, Amount: {action.AmountInEuro}");
        // }

        // Console.WriteLine($"Held Quantity: {heldQuantity}, Total Cost: {totalCost}, Current Price: {currentPrice}");
        // Console.WriteLine($"Realized Profit: {realizedProfit}, Unrealized Profit: {unrealizedProfit}");
        // Console.WriteLine($"Total Profit: {totalProfit}, Total Loss: {totalLoss}");
        // Console.WriteLine($"Profit Factor: {profitFactor}, Win/Loss Ratio: {winLossRatio}");

        return (profitFactor, winLossRatio);
    }

    private decimal CalculateVariance(List<decimal> dailyReturns)
    {
        if (dailyReturns == null || dailyReturns.Count < 2)
            throw new ArgumentException("Daily returns must contain at least two entries.");

        decimal mean = dailyReturns.Average();
        decimal variance = dailyReturns.Sum(r => (r - mean) * (r - mean)) / dailyReturns.Count;

        return variance;
    }

    private decimal CalculateMaximumDrawdown(List<decimal> portfolioValues)
    {
        decimal maxDrawdown = 0;
        decimal peak = portfolioValues[0];

        foreach (var value in portfolioValues)
        {
            if (value > peak)
            {
                peak = value;
            }
            else
            {
                var drawdown = (peak - value) / peak;
                if (drawdown > maxDrawdown)
                {
                    maxDrawdown = drawdown;
                }
            }
        }

        return maxDrawdown;
    }

    private List<decimal> CalculateDailyReturns(List<decimal> portfolioValues)
    {
        var dailyReturns = new List<decimal>();

        for (int i = 1; i < portfolioValues.Count; i++)
        {
            var dailyReturn = (portfolioValues[i] - portfolioValues[i - 1]) / portfolioValues[i - 1];
            dailyReturns.Add(dailyReturn);
        }

        return dailyReturns;
    }

    public TradeQuality AnalyzeTradeQuality()
    {
        int correctTrades = 0;
        decimal totalDelta = 0;

        var portfolioManager = new PortfolioManager();
        portfolioManager.SetCash(300);
        var trades = tradeActions.Where(t => t.Action == BuySellHold.Buy || t.Action == BuySellHold.Sell).ToList();

        for (int i = 0; i < trades.Count - 1; i++)
        {
            portfolioManager.ExecuteTrade(trades[i]);

            var currentTrade = trades[i];
            var nextTrade = trades[i + 1];

            var currentPortfolioValue = portfolioManager.GetAccountTotal();

            if (currentTrade.Action == BuySellHold.Buy && nextTrade.CurrentTokenPrice > currentTrade.CurrentTokenPrice ||
                currentTrade.Action == BuySellHold.Sell && nextTrade.CurrentTokenPrice < currentTrade.CurrentTokenPrice)
            {
                correctTrades++;
                totalDelta += currentTrade.AmountInEuro / currentPortfolioValue * 100;
            }
        }

        int totalTrades = trades.Count - 1;
        decimal correctTradePercentage = totalTrades > 0 ? (decimal)correctTrades / totalTrades * 100 : 0;
        decimal averageDelta = correctTrades > 0 ? totalDelta / correctTrades : 0;

        return new TradeQuality
        {
            CorrectTradePercentage = correctTradePercentage,
            AverageDelta = averageDelta,
            TotalTrades = totalTrades,
            CorrectTrades = correctTrades
        };
    }


    public (string resultText, Metrics metrics, TradeQuality tradeQuality) Analyze()
    {
        decimal totalPortfolioValue = portfolioManager.GetAccountTotal();

        var metrics = CalculateMetrics();
        var tradeQuality = AnalyzeTradeQuality();

        // Console.WriteLine($"Correct Trade Percentage: {tradeQuality.CorrectTradePercentage:F2}%");
        // Console.WriteLine($"Average Delta on Correct Trades: {tradeQuality.AverageDelta:F2}%");

        return ($@"
        Strategy: {tradingStrategy.GetType().Name}
        Total Portfolio Value: {totalPortfolioValue:F2} EUR
        Sharpe Ratio: {metrics.SharpeRatio:F2}
        Maximum Drawdown: {metrics.MaximumDrawdown:P2}
        Profit Factor: {metrics.ProfitFactor:F2}
        Win/Loss Ratio: {metrics.WinLossRatio:F2}
        ", metrics, tradeQuality);
    }
}

public class MetricsComparer
{
    public static (decimal CombinedScore, TStrat Strat, Metrics Metrics, decimal Result) CompareMetricsWithResult<TStrat>(
    IEnumerable<(TStrat strat, Metrics Metrics, decimal result)> runs,
    decimal metricsWeight = 0.7m,
    decimal resultWeight = 0.3m
) where TStrat : TradingStrategyBase, new()
    {
        // Normalize metrics score and result to 0-1 range to avoid skewed weighting
        var maxMetricsScore = runs.Max(r => r.Metrics.SharpeRatio * 0.4m
                                            - r.Metrics.MaximumDrawdown * 0.2m
                                            + r.Metrics.ProfitFactor * 0.3m
                                            + r.Metrics.WinLossRatio * 0.1m);
        var maxResult = runs.Max(r => r.result);

        var normalizedRuns = runs.Select(run =>
        {

            var normalizedMetricsScore = (run.Metrics.SharpeRatio * 0.4m
                                         - run.Metrics.MaximumDrawdown * 0.2m
                                         + run.Metrics.ProfitFactor * 0.3m
                                         + run.Metrics.WinLossRatio * 0.1m) / maxMetricsScore;

            var normalizedResult = run.result / maxResult;

            return (run.strat, run.Metrics, run.result,
                    NormalizedMetricsScore: normalizedMetricsScore,
                    NormalizedResult: normalizedResult);
        }).ToList();

        // Combine normalized metrics score and result into a weighted score
        var combinedRuns = normalizedRuns.Select(run =>
        {
            var combinedScore = (run.NormalizedMetricsScore * metricsWeight) +
                                (run.NormalizedResult * resultWeight);
            return (CombinedScore: combinedScore, run.strat, run.Metrics, run.result);
        })
        .OrderByDescending(r => r.CombinedScore)
        .ToList();

        // Console.WriteLine("Run Rankings (Combined Scores):");
        // foreach (var rankedRun in combinedRuns)
        // {
        //     Console.WriteLine($"Combined Score: {rankedRun.CombinedScore:F2}, Metrics Score: {rankedRun.Metrics.printableMetrics}, Result: {rankedRun.result}");
        // }

        return combinedRuns.FirstOrDefault();
    }
    public static (TStrat Strategy, decimal Score, TradeQuality TradeQuality, decimal FinalPortfolioValue)
        RankStrategiesByScore<TStrat>(
            IEnumerable<(TStrat Strategy, TradeQuality TradeQuality, decimal FinalPortfolioValue)> strategyResults,
            decimal tradeQualityWeight = 0.45m,
            decimal averageDeltaWeight = 0.05m,
            decimal portfolioValueWeight = 0.5m)
        where TStrat : TradingStrategyBase, new()
    {
        // Bereken de hoogste waarden van TradeQuality en PortfolioValue
        var maxTradeQuality = strategyResults.Max(r => r.TradeQuality.CorrectTradePercentage);
        var maxDelta = strategyResults.Max(r => r.TradeQuality.AverageDelta);
        var maxPortfolioValue = strategyResults.Max(r => r.FinalPortfolioValue);

        // Rangschik strategieën op basis van de gewogen score
        var rankedStrategies = strategyResults.Select(result =>
        {
            // Normaliseer scores
            decimal normalizedTradeQuality = maxTradeQuality > 0
                ? result.TradeQuality.CorrectTradePercentage / maxTradeQuality
                : 0;
            // Normaliseer scores
            decimal normalizedAverageDelta = maxDelta > 0
                ? result.TradeQuality.AverageDelta / maxDelta
                : 0;
            decimal normalizedPortfolioValue = maxPortfolioValue > 0
                ? result.FinalPortfolioValue / maxPortfolioValue
                : 0;

            // Bereken totale score
            decimal totalScore = (normalizedTradeQuality * tradeQualityWeight) +
                                 (normalizedAverageDelta * averageDeltaWeight) +
                                 (normalizedPortfolioValue * portfolioValueWeight);

            return (result.Strategy, Score: totalScore, result.TradeQuality, result.FinalPortfolioValue);
        })
        .OrderByDescending(r => r.Score)
        .ToList();

        Console.WriteLine("Strategy Rankings:");
        foreach (var ranked in rankedStrategies)
        {
            Console.WriteLine($"Strategy: {ranked.Strategy}, Score: {ranked.Score:F2}, " +
                              $"Correct Trades: {ranked.TradeQuality.CorrectTradePercentage:F2}%, " +
                              $"Average Delta: {ranked.TradeQuality.AverageDelta:F2}%, " +
                              $"Final Portfolio: {ranked.FinalPortfolioValue:F2}");
        }

        // Retourneer de best scorende strategie
        return rankedStrategies.FirstOrDefault();
    }

}

public class TradeQuality
{
    public decimal CorrectTradePercentage { get; set; }
    public decimal AverageDelta { get; set; }
    public int TotalTrades { get; set; }
    public int CorrectTrades { get; set; }
}

public class Metrics
{
    public decimal SharpeRatio { get; set; }
    public decimal MaximumDrawdown { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal WinLossRatio { get; set; }

    public string printableMetrics => $@"
        Sharpe Ratio: {SharpeRatio:F2}
        Maximum Drawdown: {MaximumDrawdown:P2}
        Profit Factor: {ProfitFactor:F2}
        Win/Loss Ratio: {WinLossRatio:F2}";
}
