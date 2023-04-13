namespace MyFunctions.Tests;

public class WorkItemServiceTests {

    [Fact]
    public async Task UpdateAllWorkItemDaysSinceCreationAsync() {

        var orgName = Environment.GetEnvironmentVariable("AZUREDEVOPSORGNAME");
        var projectName = Environment.GetEnvironmentVariable("AZUREDEVOPSPROJECTNAME");
        var patToken = Environment.GetEnvironmentVariable("AZUREDEVOPSPATTOKEN");

        if (orgName == null || projectName == null || patToken == null) {
            throw new ApplicationException("The required environment variables are not set");
        }

        var sut = new WorkItemService2(orgName, projectName, patToken);

        await sut.UpdateAllWorkItemDaysSinceCreationAsync();
    }
}