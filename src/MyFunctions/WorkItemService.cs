using System;
using System.Collections.Generic;
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

        //string url = $"https://dev.azure.com/{orgName}/{projectName}/_apis/wit/workitems?api-version=6.0&$top=1000&$select=System.Id,System.WorkItemType,System.Title,Custom.Field.Name&$expand=Relations";

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

             this.Log($"WorkItemService.UpdateWorkItems starting execution at {DateTime.Now}...");

            string url = $"https://dev.azure.com/{this.OrgName}/{this.ProjectName}/_apis/wit/workitems?api-version=6.0&$top=1000&$select=System.Id,System.WorkItemType,System.Title,Custom.Field.Name&$expand=Relations";
            
            // Create an HTTP client
            using (var client = new HttpClient()) {

                // Set the Authorization header with the PAT
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", "", this.PatToken))));

                // Set the Content-Type header
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Create a new HTTP request
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);

                // Send the HTTP request
                HttpResponseMessage response = await client.SendAsync(request);

                // Check if the response is successful
                if (response.IsSuccessStatusCode) {

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
                }
                else {
                    // Log the response status code and reason phrase
                    this.Log($"ERROR => Response status code: {response.StatusCode}, Reason: {response.ReasonPhrase}");
                }
            }
        }

        private void Log(string content) {

            // TODO: Rewrite this for proper logging
            // NullOp for now
            System.Console.WriteLine(content);
        }
    }
}