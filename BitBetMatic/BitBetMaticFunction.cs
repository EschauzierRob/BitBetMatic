using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;
using System.Text;

namespace BitBetMatic
{
    public static class BitBetMaticFunctionAtWill
    {
        private static ILogger Log;
        [FunctionName("BitBetMaticFunctionAtWill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            Log=log;
            Log.LogInformation("C# HTTP trigger function processed a request.");
            var sb = new StringBuilder();
            BitBetMaticProcessor bitBetMaticProcessor = new BitBetMaticProcessor();
            var result = await DoBacktesting(sb, bitBetMaticProcessor);
            // var result = await new BitBetMaticProcessor().Process(false);
            Log.LogInformation(result);

            // return result;
            return new OkObjectResult(result);
        }

        private static async Task<string> DoBacktesting(StringBuilder sb, BitBetMaticProcessor bitBetMaticProcessor)
        {
            sb.AppendLine("BTC backtesting:");
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new ModerateStrategy(), BitBetMaticProcessor.BtcMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new AgressiveStrategy(), BitBetMaticProcessor.BtcMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new ScoredStrategy(), BitBetMaticProcessor.BtcMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new StoplossStrategy(), BitBetMaticProcessor.BtcMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new AdvancedStrategy(), BitBetMaticProcessor.BtcMarket));

            sb.AppendLine("ETH backtesting:");
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new ModerateStrategy(), BitBetMaticProcessor.EthMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new AgressiveStrategy(), BitBetMaticProcessor.EthMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new ScoredStrategy(), BitBetMaticProcessor.EthMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new StoplossStrategy(), BitBetMaticProcessor.EthMarket));
            sb.AppendLine(await bitBetMaticProcessor.RunBacktest(new AdvancedStrategy(), BitBetMaticProcessor.EthMarket));
            string result = sb.ToString();
            Console.Write(result);
            return result;
        }
    }

    public static class BitBetMaticFunction
    {
        [FunctionName("BitBetMaticFunction")]
        public static async Task Run(
            //12-07-2024 ~11:55
            [TimerTrigger("0 */15 * * * *")] TimerInfo timer,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var result = await new BitBetMaticProcessor().Process();
            log.LogInformation(result);
        }
    }
}