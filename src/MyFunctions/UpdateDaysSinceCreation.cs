using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MyFunctions
{
    public static class UpdateWorkItemsFunction
    {
        [FunctionName("UpdateWorkItemsFunction")]
        public static async Task Run(
            [TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo, // Trigger the function every 5 minutes
            ILogger log
        ) {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Read the Personal Access Token (PAT) from environment variable
            string pat = Environment.GetEnvironmentVariable("PAT");
            string orgName = Environment.GetEnvironmentVariable("ORG_NAME");
            string projectName = Environment.GetEnvironmentVariable("PROJECT_NAME");
            string url = $"https://dev.azure.com/{orgName}/{projectName}/_apis/wit/workitems?api-version=6.0&$top=1000&$select=System.Id,System.WorkItemType,System.Title,Custom.Field.Name&$expand=Relations";

            // Create an HTTP client
            using (HttpClient client = new HttpClient())
            {
                // Set the Authorization header with the PAT
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", pat))));

                // Set the Content-Type header
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create a new HTTP request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                // Send the HTTP request
                HttpResponseMessage response = await client.SendAsync(request);

                // Check if the response is successful
                if (response.IsSuccessStatusCode)
                {
                    // Deserialize the response content
                    dynamic responseContent = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                    // Get the list of workitems from the response content
                    List<dynamic> workItems = responseContent.value.ToObject<List<dynamic>>();

                    // Loop through each workitem and update the custom field
                    foreach (dynamic workItem in workItems)
                    {
                        // Get the ID and custom field value of the workitem
                        int workItemId = workItem.id;
                        string customFieldValue = workItem.fields.Custom.Field.Name;

                        // Define the JSON payload to update the custom field
                        dynamic payload = new
                        {
                            op = "add",
                            path = "/fields/Custom.Field.Name",
                            value = "99"
                        };

                        // Serialize the JSON payload
                        string jsonPayload = JsonConvert.SerializeObject(payload);

                        // Set the URL for the Azure DevOps REST API to update the custom field for the workitem
                        string updateUrl = $"https://dev.azure.com/{orgName}/{projectName}/_apis/wit/workitems/{workItemId}?api-version=6.0";

                        // Create a new HTTP request
                        HttpRequestMessage updateRequest = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl)
                        {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json-patch+json")
                        };

                        // Send the HTTP request
                        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);

                        // Log the response
                        log.LogInformation($"Workitem {workItemId}: Custom field value updated from '{customFieldValue}' to 'New Value'");
                    }
                }
                else
                {
                    // Log the response status code and reason phrase
                    log.LogError($"Response status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }
    }
}

/*
using System;
using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzDevOpsFunctions {
    
    public static class WorkItemUpdater {

        [FunctionName("WorkItemUpdater")]
        public static async Task Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            string orgUrl = Environment.GetEnvironmentVariable("OrgUrl");
            string projectName = Environment.GetEnvironmentVariable("ProjectName");
            string patToken = Environment.GetEnvironmentVariable("PatToken");

            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            // Create a connection to the Azure DevOps REST API
            var connection = new VssConnection(new Uri(orgUrl), new VssBasicCredential(string.Empty, patToken));
            var witClient = connection.GetClient<WorkItemTrackingHttpClient>();

            // Get a list of all work items in the project
            // var workItems = await witClient.GetWorkItemsAsync(projectName);

            DateTime today = DateTime.Today;

            // Get the WorkItems that we need
            var query = new Wiql() { Query = "SELECT [System.Id], [System.Title], [System.State], [System.CreatedDate] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] <> 'Closed' ORDER BY [System.CreatedDate] DESC" };
            var result = witClient.QueryByWiqlAsync(query, projectName).Result;
            var workItemIds = result.WorkItems.Select(item => item.Id).ToArray();

            // Get a list of all work items in the project
            var fields = new String[] { "" };
            var workItems = await witClient.GetWorkItemsAsync(projectName, workItemIds, fields);

            // Use a LINQ query with the Select() method to update the custom field
            var updatedWorkItems = workItems.Select(async workItem => {

                // Calculate the number of days since the work item was created
                var daysSinceCreation = (int)(DateTime.Today - workItem.CreatedDate).TotalDays;

                // Update the custom field with the new value
                workItem.Fields["Days Since Creation"] = daysSinceCreation;

                // Save the changes to the work item
                await witClient.UpdateWorkItemAsync(workItem, workItem.Id, );

                Console.WriteLine($"Updated work item {workItem.Id} with Days Since Creation = {daysSinceCreation}");

                // Return an anonymous object with the updated work item ID and values
                return new {
                    Id = workItem.Id,
                    DaysSinceCreation = daysSinceCreation
                };
            });
        }
    }
}
*/