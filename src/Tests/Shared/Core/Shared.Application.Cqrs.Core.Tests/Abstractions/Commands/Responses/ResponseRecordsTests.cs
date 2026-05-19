using Microsoft.AspNetCore.Http;
using Shared.Application.Cqrs.Core.Abstractions.Commands.Responses;

namespace Shared.Application.Cqrs.Core.Tests.Abstractions.Commands.Responses;

public sealed class ResponseRecordsTests
{
    [Fact]
    public void CreateResponse_InitProperties_AreAssigned()
    {
        var id = Guid.NewGuid();
        var payload = new { Name = "test" };

        var response = new CreateResponse<object>
        {
            Id = id,
            Payload = payload,
            StatusCode = StatusCodes.Status201Created,
        };

        response.Id.Should().Be(id);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status201Created);
    }

    [Fact]
    public void UpdateResponse_InitProperties_AreAssigned()
    {
        var key = Guid.NewGuid();
        var payload = new { Name = "updated" };

        var response = new UpdateResponse<object>
        {
            Key = key,
            Payload = payload,
            StatusCode = StatusCodes.Status200OK,
        };

        response.Key.Should().Be(key);
        response.Payload.Should().Be(payload);
        response.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void Responses_AreRecords_AndSupportWithExpression()
    {
        var original = new CreateResponse<object>
        {
            Id = Guid.NewGuid(),
            Payload = new { Value = 1 },
            StatusCode = 201,
        };

        var modified = original with { StatusCode = 500 };

        modified.Id.Should().Be(original.Id);
        modified.Payload.Should().Be(original.Payload);
        modified.StatusCode.Should().Be(500);
    }

    [Fact]
    public void Response_StatusCodeDefault()
    {
        var createResponse = new CreateResponse<object>();
        var updateResponse = new UpdateResponse<object>();

        createResponse.StatusCode.Should().Be(0);
        updateResponse.StatusCode.Should().Be(0);
    }
}
