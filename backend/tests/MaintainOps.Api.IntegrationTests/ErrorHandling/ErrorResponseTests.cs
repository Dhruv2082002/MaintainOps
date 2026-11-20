using System.Net;
using System.Text.Json;

namespace MaintainOps.Api.IntegrationTests.ErrorHandling;

public sealed class ErrorResponseTests(ErrorTestApiFactory factory) : IClassFixture<ErrorTestApiFactory>
{
    public static TheoryData<string, HttpStatusCode, string, string> ExpectedFailures => new()
    {
        { "not-found", HttpStatusCode.NotFound, "test.not_found", ErrorTestController.NotFoundMessage },
        { "conflict", HttpStatusCode.Conflict, "test.conflict", ErrorTestController.ConflictMessage },
        { "business-rule", HttpStatusCode.UnprocessableEntity, "test.business_rule", ErrorTestController.BusinessRuleMessage },
    };

    [Theory]
    [MemberData(nameof(ExpectedFailures))]
    public async Task Expected_failure_returns_problem_details_with_its_status_code_and_message(
        string path, HttpStatusCode expectedStatus, string expectedCode, string expectedDetail)
    {
        var response = await factory.CreateClient().GetAsync($"/test/errors/{path}");

        Assert.Equal(expectedStatus, response.StatusCode);
        var problem = await ReadProblemAsync(response);
        Assert.Equal((int)expectedStatus, problem.GetProperty("status").GetInt32());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("title").GetString()));
        Assert.Equal(expectedDetail, problem.GetProperty("detail").GetString());
        Assert.Equal(expectedCode, problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    [Fact]
    public async Task Unexpected_exception_returns_500_problem_details_without_exception_text()
    {
        var response = await factory.CreateClient().GetAsync("/test/errors/crash");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(ErrorTestController.CrashMessage, body);
        Assert.DoesNotContain(nameof(InvalidOperationException), body);

        var problem = await ReadProblemAsync(response);
        Assert.Equal(500, problem.GetProperty("status").GetInt32());
        Assert.Equal("unexpected_error", problem.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.GetProperty("traceId").GetString()));
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}
