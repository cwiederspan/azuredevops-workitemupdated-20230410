using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyFunctions {

    public static class UpdateWorkItemsFunction {

        [FunctionName("UpdateWorkItemsFunction")]
        public static async Task Run(
            [TimerTrigger("0 0 0 * * *")] TimerInfo timer,
            ILogger log
        ) {

            // Get the configuration values
            string orgName = Environment.GetEnvironmentVariable("OrgName");
            string projectName = Environment.GetEnvironmentVariable("ProjectName");
            string patToken = Environment.GetEnvironmentVariable("PatToken");

            // Create a new WorkItemService instance
            var workItemService = new WorkItemService(orgName, projectName, patToken);

            // Update the DaysSinceCreation custom field for all work items that match the query
            try {
                await workItemService.UpdateAllWorkItemDaysSinceCreationAsync();
                log.LogInformation("DaysSinceCreation custom field updated for all work items");
            }
            catch (Exception ex) {
                log.LogError($"Failed to update DaysSinceCreation custom field: {ex.Message}");
                throw;
            }
        }
    }
}