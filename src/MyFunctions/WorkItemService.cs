using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MyFunctions {

    public class WorkItemService {

        private readonly string PatToken;

        private readonly string OrgName;

        private readonly string ProjectName;

        public WorkItemService(
            string orgName,
            string projectName,
            string patToken
        ) {
            this.OrgName = orgName;
            this.ProjectName = projectName;
            this.PatToken = patToken;
        }

        public async Task UpdateAllWorkItemDaysSinceCreationAsync() {

            // Execute the query and get the work item IDs
            using (var client = new HttpClient()) {

                // Build the query URL
                string baseUrl = $"https://dev.azure.com/{this.OrgName}/{this.ProjectName}/_apis";

                // Add the personal access token to the headers
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($":{this.PatToken}")));
                var query = new { query = "Select [System.Id], [System.Title], [System.State], [System.CreatedDate] From WorkItems Where [System.WorkItemType] = 'Product Backlog Item' AND [State] <> 'Closed' AND [State] <> 'Removed' order by [System.Id]" };
                string queryUrl = $"{baseUrl}/wit/wiql?api-version=7.0";

                var response = await client.PostAsJsonAsync(queryUrl, query);
                var result = await response.Content.ReadAsAsync<Models.Wiql.Result>();

                // Update each work item with the DaysSinceCreation value
                var updateTasks = result.workItems.Select(async wi => {

                    // Get the ID
                    var id = wi.id;

                    // Build the update URL
                    string updateUrl = $"{baseUrl}/wit/workitems/{id}?api-version=7.0";

                    // Get the work item details
                    string detailsUrl = $"{updateUrl}&$expand=all";
                    string detailsJson = await client.GetStringAsync(detailsUrl);
                    dynamic workItem = JsonConvert.DeserializeObject(detailsJson);

                    // Get the created date and calculate the number of days since creation
                    DateTime createdDate = DateTime.Parse(workItem.fields["System.CreatedDate"].ToString());
                    int daysSinceCreation = (DateTime.UtcNow - createdDate).Days;

                    // Build the JSON payload
                    var payload = new {
                        op = "add",
                        path = "/fields/Custom.DaysSinceCreation",
                        value = daysSinceCreation
                    };

                    // Convert the payload to JSON and send the update request
                    var payloadWrapper = new [] { payload };
                    string updatePayload = JsonConvert.SerializeObject(payloadWrapper);
                    var content = new StringContent(updatePayload, Encoding.UTF8, "application/json-patch+json");
                    var responseMessage = await client.PatchAsync(updateUrl, content);

                    if (!responseMessage.IsSuccessStatusCode) {
                        throw new Exception($"Failed to update work item {id}: {responseMessage.ReasonPhrase}");
                    }
                }).ToList();

                await Task.WhenAll(updateTasks);
            }
        }
    }
}