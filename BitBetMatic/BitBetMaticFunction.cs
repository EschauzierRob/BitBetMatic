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

            return await new BitBetMaticProcessor().Process();
        }
    }

    public static class BitBetMaticFunction
    {
        [FunctionName("BitBetMaticFunction")]
        public static async Task Run(
            [TimerTrigger("0 0 1 * * *")] TimerInfo timer, 
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            await new BitBetMaticProcessor().Process();
        }
    }
}