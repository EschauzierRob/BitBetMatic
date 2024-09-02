using System;
using System.Collections.Generic;
using System.Linq;

public static class VolatilityCalculator
{
    public static List<double> CalculateLogReturns(List<double> prices)
    {
        var logReturns = new List<double>();

        for (int i = 1; i < prices.Count; i++)
        {
            logReturns.Add(Math.Log(prices[i] / prices[i - 1]));
        }

        return logReturns;
    }

    public static double CalculateStandardDeviation(List<double> values)
    {
        if (values.Count == 0) return 0;

        double average = values.Average();
        double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
        double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / (values.Count - 1));

        return standardDeviation;
    }

    public static (double cur, double min, double max) CalculateVolatilityMetrics(List<double> prices, int windowSize = 30)
    {
        var logReturns = CalculateLogReturns(prices);
        var volatilities = new List<double>();

        for (int i = 0; i <= logReturns.Count - windowSize; i++)
        {
            var window = logReturns.Skip(i).Take(windowSize).ToList();
            volatilities.Add(CalculateStandardDeviation(window));
        }

        double currentVolatility = CalculateStandardDeviation(logReturns.TakeLast(windowSize).ToList());
        double minVolatility = volatilities.Min();
        double maxVolatility = volatilities.Max();

        Console.WriteLine($"Current Volatility: {currentVolatility}");
        Console.WriteLine($"Minimum Volatility: {minVolatility}");
        Console.WriteLine($"Maximum Volatility: {maxVolatility}");

        return (currentVolatility, minVolatility, maxVolatility);
    }



    public static double CalculateDecayRate(List<double> prices, int windowSize = 30, double minDecayRate = 0.01, double maxDecayRate = 0.1)
    {
        var (currentVolatility, minVolatility, maxVolatility) = CalculateVolatilityMetrics(prices, windowSize);
        // Normalize the volatility
        double normalizedVolatility = (currentVolatility - minVolatility) / (maxVolatility - minVolatility);

        // Map normalized volatility to decay rate
        double decayRate = minDecayRate + (maxDecayRate - minDecayRate) * normalizedVolatility;

        return Math.Clamp(decayRate, minDecayRate, maxDecayRate); // Ensure decay rate stays within bounds
    }
}