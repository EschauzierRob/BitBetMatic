using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text;
using Microsoft.EntityFrameworkCore;
using BitBetMatic.API;
using System.Collections.Generic;
using Skender.Stock.Indicators;

namespace BitBetMatic
{
    public static class BitBetMaticFunctionAtWill
    {
        [FunctionName("BitBetMaticFunctionAtWill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            log.LogInformation("C# HTTP trigger function processed a request.");
            var sb = new StringBuilder();
            BitBetMaticProcessor bitBetMaticProcessor = new BitBetMaticProcessor();
            var backtestResult = await bitBetMaticProcessor.RunBacktesting(sb);
            var chosenStrategies = $"backtestResult: btcStrategy: {backtestResult.strategyBtc.GetType()}, ethStrategy: {backtestResult.strategyEth.GetType()}";
            Console.WriteLine(chosenStrategies);
            var processResult = await bitBetMaticProcessor.RunStrategies(backtestResult.strategyBtc, backtestResult.strategyEth, false);
            log.LogInformation(backtestResult.result);

            // return result;
            return new OkObjectResult(backtestResult + "\n\n" + chosenStrategies + "\n\n" + processResult);
        }
    }
    public static class BitBetMaticBackTesting
    {
        [FunctionName("BitBetMaticBackTesting")]
        public static async Task Run([TimerTrigger("0 */15 * * * *")] TimerInfo timer, ILogger log)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            log.LogInformation("C# HTTP trigger function processed a request.");
            await BitBetMaticBackTestingOnDemand.BackTestVariants();
            // await FindPatterns();

        }
    }
    public static class BitBetMaticBackTestingOnDemand
    {
        [FunctionName("BitBetMaticBackTestingOnDemand")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            log.LogInformation("C# HTTP trigger function processed a request.");
            await BackTestVariants();
            // await FindPatterns();

            return new OkObjectResult("");
        }

        private static async Task FindPatterns()
        {
            // Voorbeeldlijst van Quotes
            var dataLoader = new DataLoader(new BitvavoApi());

            var start = DateTime.Today.AddDays(-2);
            var end = DateTime.Today;
            var historicalData = await dataLoader.LoadHistoricalData("ETH-EUR", "1h", 1440, start, end);

            List<Quote> quotes = historicalData;

            int trendWindowSize = 2; // Aantal Quotes om te analyseren voor trends
            decimal trendThreshold = 5.0m; // Drempel voor procentuele verandering

            var patterns = CandleExtensions.ClassifyPatterns(quotes, trendWindowSize, trendThreshold);

            // // Print patronen
            Console.WriteLine("Geïdentificeerde Patronen:");
            foreach (var pattern in patterns)
            {
                Console.WriteLine($"Patroon: {pattern.PatternType}, Datum: {pattern.PatternDate}");
                var behaviors = CandleExtensions.AnalyzePreReversalCandles(pattern.PreReversalCandles);
                foreach (var behavior in behaviors)
                {
                    Console.WriteLine($" - {behavior}");
                }

            }
            // ReversalAnalysis.AnalyzeReversalsWithIndicators(quotes);
        }

        public static async Task BackTestVariants()
        {
            var sb = new StringBuilder();
            var numberOfVariants = 3;

            var tasks = new List<Task<(TradingStrategyBase strategy, string result)>>{

                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<ModerateStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<ModerateStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<ScoredStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<ScoredStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<StoplossStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<StoplossStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestDeepTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                // new BackTesting(new BitvavoApi()).DoBacktestTuning<SimpleMAStrategy>(sb, BitBetMaticProcessor.BtcMarket, 0),
                // new BackTesting(new BitvavoApi()).DoBacktestTuning<SimpleMAStrategy>(sb, BitBetMaticProcessor.EthMarket, 0),
            };

            await Task.WhenAll(tasks);
        }
    }

    public static class ComparePerformance
    {
        [FunctionName("ComparePerformance")]
        public static async Task<ContentResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req, ILogger log)
        {
            log.LogInformation("Fetching data from Bitvavo...");
            return new BitvavoPerformanceProcessor(new BitvavoApi()).ProcessPerformance(BitBetMaticProcessor.BtcMarket);
        }
    }


    public static class BitBetMaticFunction
    {
        [FunctionName("BitBetMaticFunction")]
        public static async Task Run(
            [TimerTrigger("0 */15 * * * *")] TimerInfo timer,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            var sb = new StringBuilder();
            BitBetMaticProcessor bitBetMaticProcessor = new BitBetMaticProcessor();
            var backtestResult = await bitBetMaticProcessor.RunBacktesting(sb);

            Console.WriteLine($"backtestResult: btcStrategy: {backtestResult.strategyBtc.GetType()}, ethStrategy: {backtestResult.strategyEth.GetType()}");
            var processResult = await bitBetMaticProcessor.RunStrategies(backtestResult.strategyBtc, backtestResult.strategyEth);
            log.LogInformation(backtestResult.result);
            log.LogInformation(processResult);
        }
    }
}