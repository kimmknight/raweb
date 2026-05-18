using System.Text.Json.Serialization;
using RAWeb.Server.Utilities;

namespace RAWeb.Server.Api;

internal static class GetDomainNameEndpoint {
    internal static void Map(IEndpointRouteBuilder app) {
        app.MapGet("/api/domain", Handle);
    }

    private static IResult Handle() {
        var domain = SignIn.GetDomainName();
        return Results.Ok(new DomainResponse(domain));
    }
}

public record DomainResponse([property: JsonPropertyName("domain")] string Domain);
