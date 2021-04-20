using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;


namespace DevbootcampDurableChaining
{
    public static class DurableFunctions
    {
        //am including the following Functions:
        // 1) a HTTP trigger to start the Durable Orchestrator
        // 2) An Orchestrator Function
        // 3) 3 Activity Functions

        [FunctionName("Orchestrator_HttpStart")]

        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post")]HttpRequestMessage req,
             [DurableClient] IDurableOrchestrationClient starter, ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("FakeOrchestrator", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        [FunctionName("FakeOrchestrator")]
        public static async Task<string> FakeOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            try
            {
                //Awaits the results from an Activity Function named ActivityFunction1.
                var x = await context.CallActivityAsync<string>("ActivityFunction1", null);
                //Passes in the results from ActivityFunction1 into ActivityFunction2, and awaits the results of ActivityFunction2
                var y = await context.CallActivityAsync<string>("ActivityFunction2", x);
                //Passes in the results from ActivityFunction2 into ActivityFunction3, and awaits & returns the results of ActivityFunction3
                return await context.CallActivityAsync<string>("ActivityFunction3", y);
            }
            catch (Exception ex)
            {
                return "An exception happened when running the orchestrator";
            }
        }

        [FunctionName("ActivityFunction1")]
        public static string ActivityFunction1([ActivityTrigger] string someInput, ILogger log)
        {
            return $"Output1";
        }

        [FunctionName("ActivityFunction2")]
        public static string ActivityFunction2([ActivityTrigger] string InputFromAF1Output, ILogger log)
        {
            //suspend execution for 120 seconds (2 minutes) to demonstrate waiting for durable instance to complete
            Thread.Sleep(120000);
            return InputFromAF1Output + $"Output2";
        }

        [FunctionName("ActivityFunction3")]
        public static string ActivityFunction3([ActivityTrigger] string InputFromAF2Output, ILogger log)
        {
            return InputFromAF2Output + $"Output2";
        }
    }
}