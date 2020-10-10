using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using ServerlessFoodDelivery.Models.Models;
using ServerlessFoodDelivery.Shared.Helpers;
using ServerlessFoodDelivery.Shared.Services;

namespace ServerlessFoodDelivery.FunctionApp.Orchestrators
{
    public class OrderOrchestratorTrigger
    {
      
        [FunctionName("PlaceNewOrderQueueTrigger")]
        public static async Task PlaceNewOrderQueueTrigger(
           [DurableClient] IDurableOrchestrationClient context,
           [QueueTrigger("%OrderNewQueue%", Connection = "AzureWebJobsStorage")] Order order,
           ILogger log)
        {
            try
            {
                var instanceId = order.Id;
                await StartInstance(context, order, instanceId, log);
            }
            catch (Exception ex)
            {
                var error = $"StartTripMonitorViaQueueTrigger failed: {ex.Message}";
                log.LogError(error);
            }
        }
        [FunctionName("AcceptedOrderQueueTrigger")]
        public static async Task AcceptedOrderQueueTrigger(
        [DurableClient] IDurableOrchestrationClient context,
        [QueueTrigger("%OrderAcceptedQueue%", Connection = "AzureWebJobsStorage")] Order order,
        ILogger log)
        {
            await context.RaiseEventAsync(order.Id, Constants.RESTAURANT_ORDER_ACCEPT_EVENT);
        }

        [FunctionName("OutForDeliveryOrderQueueTrigger")]
        public static async Task OutForDeliveryOrderQueueTrigger(
       [DurableClient] IDurableOrchestrationClient context,
       [QueueTrigger("%OrderOutForDeliveryQueue%", Connection = "AzureWebJobsStorage")] Order order,
       ILogger log)
        {
            string instanceId = $"{order.Id}-accepted";
            await context.RaiseEventAsync(instanceId, Constants.RESTAURANT_ORDER_OUTFORDELIVERY_EVENT);
        }

        private static async Task StartInstance(IDurableOrchestrationClient context, Order order, string instanceId, ILogger log)
        {
            try
            {
                var reportStatus = await context.GetStatusAsync(instanceId);
                string runningStatus = reportStatus == null ? "NULL" : reportStatus.RuntimeStatus.ToString();
                log.LogInformation($"Instance running status: '{runningStatus}'.");

                if (reportStatus == null || reportStatus.RuntimeStatus != OrchestrationRuntimeStatus.Running)
                {
                    await context.StartNewAsync("OrderPlacedOrchestrator", instanceId, order);
                    log.LogInformation($"Started a new instance = '{instanceId}'.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}