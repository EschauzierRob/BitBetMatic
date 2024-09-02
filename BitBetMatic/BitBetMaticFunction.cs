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
            var numberOfVariants = 30;

            var tasks = new List<Task<(TradingStrategyBase strategy, string result)>>{

                new BackTesting(new BitvavoApi()).DoBacktestTuning<ModerateStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestTuning<ModerateStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestTuning<AgressiveStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestTuning<ScoredStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestTuning<ScoredStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestTuning<StoplossStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestTuning<StoplossStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),

                new BackTesting(new BitvavoApi()).DoBacktestTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.BtcMarket, numberOfVariants),
                new BackTesting(new BitvavoApi()).DoBacktestTuning<AdvancedStrategy>(sb, BitBetMaticProcessor.EthMarket, numberOfVariants),
            };

                await Task.WhenAll(tasks);

            return new OkObjectResult("");
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