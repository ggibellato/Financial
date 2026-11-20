using System.Net;
using System.Text.RegularExpressions;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Financial.Api.Tests;

[Trait("Category", "Integration")]
public partial class EmptyBodyEndpointsTests : ApiEndpointTests
{
    private static readonly HashSet<string> BodyMethods = ["POST", "PUT", "PATCH", "DELETE"];

    [GeneratedRegex(@"\{(?<name>\w+)[^}]*\}")]
    private static partial Regex RouteParameter();

    [Fact]
    public async Task EveryEndpointWithARequestBody_EmptyBody_ReturnsBadRequest()
    {
        var endpoints = Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(HasRequestBody)
            .ToList();
        endpoints.Should().NotBeEmpty();

        var offenders = new List<string>();
        foreach (var endpoint in endpoints)
        {
            var method = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods.Single(BodyMethods.Contains);
            var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>()!;
            var url = "/" + RouteParameter().Replace(endpoint.RoutePattern.RawText!, parameter => PlaceholderFor(parameter, action)).TrimStart('/');
            using var request = new HttpRequestMessage(new HttpMethod(method), url)
            {
                Content = new StringContent(string.Empty, System.Text.Encoding.UTF8, "application/json")
            };

            var response = await Client.SendAsync(request);

            if (response.StatusCode != HttpStatusCode.BadRequest || !await IsRejectedBeforeAnyServiceAsync(response, endpoint))
            {
                offenders.Add($"{method} {url} -> {(int)response.StatusCode}");
            }
        }

        offenders.Should().BeEmpty("an empty body must be rejected with 400 rather than reach a service");
    }

    private static async Task<bool> IsRejectedBeforeAnyServiceAsync(HttpResponseMessage response, RouteEndpoint endpoint)
    {
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        var routeParameters = endpoint.RoutePattern.Parameters.Select(p => p.Name);
        return problem!.Detail is null
            && !problem.Errors.Keys.Any(key => routeParameters.Contains(key, StringComparer.OrdinalIgnoreCase));
    }

    private static bool HasRequestBody(RouteEndpoint endpoint)
    {
        var action = endpoint.Metadata.GetMetadata<ControllerActionDescriptor>();
        var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods;
        return action is not null
            && methods is not null
            && methods.Any(BodyMethods.Contains)
            && action.Parameters.Any(p => p.BindingInfo?.BindingSource == BindingSource.Body);
    }

    private static string PlaceholderFor(Match parameter, ControllerActionDescriptor action)
    {
        var name = parameter.Groups["name"].Value;
        var type = action.Parameters.FirstOrDefault(p => p.Name == name)?.ParameterType;
        return type == typeof(Guid) ? Guid.NewGuid().ToString()
            : type == typeof(int) || type == typeof(long) || name == "version" ? "1"
            : "x";
    }
}
