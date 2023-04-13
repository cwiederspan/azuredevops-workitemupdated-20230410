using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MyFunctions {

    public class WorkItemService {

        private readonly string PatToken;

        private readonly string OrgName;

        private readonly string ProjectName;

        private readonly HttpClient Client;

        public WorkItemService(
            string orgName,
            string projectName,
            string patToken
        ) {
            this.OrgName = orgName;
            this.ProjectName = projectName;
            this.PatToken = patToken;

            this.Client = new HttpClient();
            this.Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", this.PatToken))));
            this.Client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async Task UpdateAllWorkItemDaysSinceCreationAsync() {

            this.Log($"WorkItemService.UpdateWorkItems starting execution at {DateTime.Now}...");

            // Get the list of WorkItems that should be processed
            var workItemIds = await this.GetWorkItemIdsAsync("Select [System.Id], [System.Title], [System.State], [System.CreatedDate] From WorkItems Where [System.WorkItemType] = 'Product Backlog Item' AND [State] <> 'Closed' AND [State] <> 'Removed' order by [System.Id]");
            /*
            workItemIds.ForEach(id => {

                var workItem = this.GetWorkItemAsync(id);



            });

                    // Deserialize the response content
                    dynamic responseContent = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());

                    // Get the list of workitems from the response content
                    var workItems = responseContent.value.ToObject<List<dynamic>>();

                    // Loop through each workitem and update the custom field
                    foreach (dynamic workItem in workItems) {

                        // Get the ID and custom field value of the workitem
                        int workItemId = workItem.id;
                        string customFieldValue = workItem.fields.Custom.Field.Name;

                        // Define the JSON payload to update the custom field
                        dynamic payload = new {
                            op = "add",
                            path = "/fields/Custom.Field.Name",
                            value = "99"
                        };

                        // Serialize the JSON payload
                        string jsonPayload = JsonConvert.SerializeObject(payload);

                        // Set the URL for the Azure DevOps REST API to update the custom field for the workitem
                        string updateUrl = $"https://dev.azure.com/{this.OrgName}/{this.ProjectName}/_apis/wit/workitems/{workItemId}?api-version=6.0";

                        // Create a new HTTP request
                        HttpRequestMessage updateRequest = new HttpRequestMessage(new HttpMethod("PATCH"), updateUrl) {
                            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json-patch+json")
                        };

                        // Send the HTTP request
                        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);

                        // Log the response
                        this.Log($"Workitem {workItemId}: Custom field value updated from '{customFieldValue}' to 'New Value'");
                    }
            */
        }

        private async Task<List<int>> GetWorkItemIdsAsync(string query) {

            // Send the HTTP request
            string wiqlUrl = $"https://dev.azure.com/{this.OrgName}/{this.ProjectName}/_apis/wit/wiql?api-version=7.0";

            // Setup the query for the API call
            var body = new { query = query };

            // Setup the request
            var queryContent = new StringContent(body.ToString(), Encoding.UTF8, "application/json");

            // Post the API call
            var result = await this.Client.PostAsync(wiqlUrl, queryContent);

            // Read the response
            dynamic responseContent = JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync());

            // Get the list of workitems from the response content
            List<dynamic> workItems = responseContent.value.ToObject<List<dynamic>>();

            // Strip the work item IDs out of the response and put into a list
            var workItemIds = workItems.Select(w => (int)w.Id).ToList();

            // Return the result
            return workItemIds;
        }

        private async Task<List<int>> GetWorkItemAsync(int id) {

            // Send the HTTP request
            string url = $"https://dev.azure.com/{this.OrgName}/{this.ProjectName}/_apis/workitems/{id}?api-version=7.0";

            // Post the API call
            var result = await this.Client.GetAsync(url);

            // Read the response
            dynamic responseContent = JsonConvert.DeserializeObject(await result.Content.ReadAsStringAsync());

            // Get the list of workitems from the response content
            List<dynamic> workItems = responseContent.value.ToObject<List<dynamic>>();

            // Strip the work item IDs out of the response and put into a list
            var workItemIds = workItems.Select(w => (int)w.Id).ToList();

            // Return the result
            return workItemIds;
        }

        private void Log(string content) {

            // TODO: Rewrite this for proper logging
            // NullOp for now
            System.Console.WriteLine(content);
        }
    }
}