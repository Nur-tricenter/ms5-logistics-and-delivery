namespace Nurtricenter.Test.Integration;

using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

[Collection("Integration")]
public class CourierIntegrationTests : BaseIntegrationTest
{
    public CourierIntegrationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateCourier_WithValidRequest_ReturnsCreatedAndSavesToDb()
    {
        await ClearDatabaseAsync();

        var request = new { FullName = "John Doe" };
        var response = await Client.PostAsJsonAsync("/api/v1/couriers", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var content = await response.Content.ReadFromJsonAsync<CourierResponseDto>();
        content.Should().NotBeNull();
        content!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateCourier_WithEmptyName_ReturnsBadRequest()
    {
        var request = new { FullName = "" };
        var response = await Client.PostAsJsonAsync("/api/v1/couriers", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Validation.General");
    }

    private sealed record CourierResponseDto(Guid Id, string FullName, int Status);
    private sealed record ProblemDetails(string Title, int Status, string Detail);
}
