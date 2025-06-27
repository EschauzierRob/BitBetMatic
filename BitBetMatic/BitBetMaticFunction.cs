using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BitBetMatic.API;
using BitBetMatic.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;


namespace BitBetMatic
{
    public class BitBetMaticFunctionAtWill
    {
        private readonly ICandleRepository CandleRepository;
        private readonly IApiWrapper API;

        public BitBetMaticFunctionAtWill(IApiWrapper api, ICandleRepository candleRepository)
        {
            API = api;
            CandleRepository = candleRepository;
        }

        [Function("BitBetMaticFunctionAtWill")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req, FunctionContext funcContext)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            // var _logger = funcContext.GetLogger();

            // Console.WriteLine("C# HTTP trigger function processed a request.");
            var sb = new StringBuilder();
            BitBetMaticProcessor bitBetMaticProcessor = new BitBetMaticProcessor(API, CandleRepository);
            var backtestResult = await bitBetMaticProcessor.RunBacktesting(sb);
            var chosenStrategies = $"backtestResult: btcStrategy: {backtestResult.strategyBtc.GetType()}, ethStrategy: {backtestResult.strategyEth.GetType()}";
            Console.WriteLine(chosenStrategies);
            var processResult = await bitBetMaticProcessor.RunStrategies(backtestResult.strategyBtc, backtestResult.strategyEth, false);
            // Console.WriteLine(backtestResult.result);


            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            // return result;
            await response.WriteStringAsync(JsonSerializer.Serialize(backtestResult + "\n\n" + chosenStrategies + "\n\n" + processResult));
            return response;
        }
    }
    public class BitBetMaticBackTesting
    {
        private readonly ICandleRepository CandleRepository;

        public BitBetMaticBackTesting(ICandleRepository candleRepository)
        {
            CandleRepository = candleRepository;
        }
        [Function("BitBetMaticBackTesting")]
        public async Task RunAsync([TimerTrigger("0 */15 * * * *")] TimerInfo timer)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            Console.WriteLine("C# HTTP trigger function processed a request.");
            await new BitBetMaticBackTestingOnDemand(CandleRepository).BackTestVariants();
            // await FindPatterns();

        }
    }
    public class BitBetMaticBackTestingOnDemand
    {
        private readonly ICandleRepository CandleRepository;

        public BitBetMaticBackTestingOnDemand(ICandleRepository candleRepository)
        {
            CandleRepository = candleRepository;
        }
        [Function("BitBetMaticBackTestingOnDemand")]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequestData req)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            Console.WriteLine("C# HTTP trigger function processed a request.");
            await BackTestVariants();
            // await FindPatterns();

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            // return result;
            await response.WriteStringAsync(JsonSerializer.Serialize(""));
            return response;
        }

        private async Task FindPatterns()
        {
            // Voorbeeldlijst van Quotes
            var dataLoader = new DataLoader(new BitvavoApi(CandleRepository), CandleRepository);

            var start = DateTime.Today.AddDays(-2);
            var end = DateTime.Today;
            var historicalData = await dataLoader.LoadHistoricalData("ETH-EUR", "1h", 1440, start, end);

            int trendWindowSize = 2; // Aantal Quotes om te analyseren voor trends
            decimal trendThreshold = 5.0m; // Drempel voor procentuele verandering

            var patterns = CandleExtensions.ClassifyPatterns(historicalData, trendWindowSize, trendThreshold);

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
            // ReversalAnalysis.AnalyzeReversalsWithIndicators(historicalData);
        }

        public async Task BackTestVariants()
        {
            var sb = new StringBuilder();
            var numberOfVariants = 5;

            var tasksModerateStrategy = new List<Task<(TradingStrategyBase strategy, string result)>>{

                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<ModerateStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<ModerateStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

            await Task.WhenAll(tasksModerateStrategy);

            var tasksAgressiveStrategy = new List<Task<(TradingStrategyBase strategy, string result)>>{
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

            await Task.WhenAll(tasksAgressiveStrategy);

            var tasksScoredStrategy = new List<Task<(TradingStrategyBase strategy, string result)>>{
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<ScoredStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<ScoredStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

            await Task.WhenAll(tasksScoredStrategy);

            var tasksStoplossStrategy = new List<Task<(TradingStrategyBase strategy, string result)>>{
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<StoplossStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<StoplossStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

            await Task.WhenAll(tasksStoplossStrategy);

            var tasksAdvancedStrategy = new List<Task<(TradingStrategyBase strategy, string result)>>{
                new BackTesting(new BitvavoApi(CandleRepository), CandleRepository).DoBacktestDeepTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                // new BackTesting(new BitvavoApi(_candleRepository)).DoBacktestDeepTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

            await Task.WhenAll(tasksAdvancedStrategy);

            // new BackTesting(new BitvavoApi(_candleRepository)).DoBacktestTuning<SimpleMAStrategy>(sb, BitBetMaticProcessor.BtcMarket, 0),
            // new BackTesting(new BitvavoApi(_candleRepository)).DoBacktestTuning<SimpleMAStrategy>(sb, BitBetMaticProcessor.EthMarket, 0),
        }
    }

    // public class ComparePerformance
    // {
    //     [Function("ComparePerformance")]
    //     public async Task<HttpResponseData> Run(
    //         [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
    //     {
    //         log.LogInformation("Fetching data from Bitvavo...");
    //         return new BitvavoPerformanceProcessor(new BitvavoApi(_candleRepository)).ProcessPerformance(BitBetMaticProcessor.BtcMarket);
    //     }
    // }

    public class FindOptimalTrades
    {
        private readonly ICandleRepository CandleRepository;

        public FindOptimalTrades(ICandleRepository candleRepository)
        {
            CandleRepository = candleRepository;
        }
        [Function("FindOptimalTrades")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            Console.WriteLine("Fetching data from Bitvavo...");
            var content = new OptimalTradeFinder().Run(new BitvavoApi(CandleRepository));

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json");
            // return result;
            await response.WriteStringAsync(JsonSerializer.Serialize(content));
            return response;
        }
    }

    public class BitBetMaticFunction
    {
        private readonly ICandleRepository CandleRepository;

        public BitBetMaticFunction(ICandleRepository candleRepository)
        {
            CandleRepository = candleRepository;
        }
        [Function("BitBetMaticFunction")]
        public async Task RunAsync([TimerTrigger("0 */15 * * * *")] TimerInfo timer)
        {
            using (var context = new TradingDbContext())
            {
                context.Database.Migrate();
            }

            Console.WriteLine($"C# Timer trigger function executed at: {DateTime.Now}");
            var sb = new StringBuilder();
            BitBetMaticProcessor bitBetMaticProcessor = new BitBetMaticProcessor(new BitvavoApi(CandleRepository), new CandleRepository());
            var backtestResult = await bitBetMaticProcessor.RunBacktesting(sb);

            Console.WriteLine($"backtestResult: btcStrategy: {backtestResult.strategyBtc.GetType()}, ethStrategy: {backtestResult.strategyEth.GetType()}");
            var processResult = await bitBetMaticProcessor.RunStrategies(backtestResult.strategyBtc, backtestResult.strategyEth);
            Console.WriteLine(backtestResult.result);
            Console.WriteLine(processResult);
        }
    }
}