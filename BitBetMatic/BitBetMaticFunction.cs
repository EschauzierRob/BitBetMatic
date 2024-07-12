using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System;

namespace BitBetMatic
{
    public static class BitBetMaticFunctionAtWill
    {
        [FunctionName("BitBetMaticFunctionAtWill")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var result = await new BitBetMaticProcessor().Process(false);
            log.LogInformation(result.ToString());

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
            var resultContent = (result as OkObjectResult)!.Value as string;
            log.LogInformation(resultContent);
        }
    }
}