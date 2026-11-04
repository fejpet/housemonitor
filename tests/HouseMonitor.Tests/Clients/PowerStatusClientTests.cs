using System.Globalization;
using System.Net;
using System.Text;
using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;

namespace HouseMonitor.Tests.Clients;

public class PowerStatusClientTests
{
    private const string SampleResponseJson = """
        {
          "battery": { "soc": 65.0, "state": "discharging" },
          "generator": { "state": "stopped", "today": 0 },
          "solar": { "production": 0.0, "today": 17.98 },
          "house": { "consumption": 415.0, "today_consumtion": 13.38 },
          "timestamp": "2026-11-04T22:20:35.014353+01:00"
        }
        """;

    [Fact]
    public async Task GetPowerStatusAsync_SendsGetRequestWithAuthHeaderAndParsesResponse()
    {
        // Arrange (REQ-002, REQ-008)
        HttpRequestMessage? capturedRequest = null;
        var handler = new FakeHttpMessageHandler(request =>
        {
            capturedRequest = request;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SampleResponseJson, Encoding.UTF8, "application/json")
            };
        });

        using var httpClient = new HttpClient(handler);
        var settings = new ApiSettings
        {
            EndpointUrl = "https://api.example.test/power/battery",
            AuthHeaderName = "X-Auth-Key",
            AuthKey = "test-secret-api-key"
        };

        var client = new PowerStatusClient(httpClient, settings);

        // Act
        var result = await client.GetPowerStatusAsync();

        // Assert - HTTP request (REQ-002, REQ-008)
        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest.Method);
        Assert.Equal(
            "https://api.example.test/power/battery",
            capturedRequest.RequestUri?.ToString());
        Assert.True(capturedRequest.Headers.TryGetValues("X-Auth-Key", out var headerValues));
        Assert.Equal("test-secret-api-key", Assert.Single(headerValues));

        // Assert - Parsed Domain Model (REQ-003, REQ-011..REQ-014, REQ-019)
        Assert.Equal(65.0, result.Battery.Soc);
        Assert.Equal("discharging", result.Battery.State);
        Assert.Equal("stopped", result.Generator.State);
        Assert.Equal(0.0, result.Generator.Today);
        Assert.Equal(0.0, result.Solar.Production);
        Assert.Equal(17.98, result.Solar.Today);
        Assert.Equal(415.0, result.House.Consumption);
        Assert.Equal(13.38, result.House.TodayConsumption);
        Assert.Equal(
            DateTimeOffset.Parse("2026-11-04T22:20:35.014353+01:00", CultureInfo.InvariantCulture),
            result.Timestamp);
    }

    [Fact]
    public async Task GetPowerStatusAsync_NonSuccessStatusCode_ThrowsHttpRequestException()
    {
        // Arrange (REQ-002)
        var handler = new FakeHttpMessageHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.Unauthorized));

        using var httpClient = new HttpClient(handler);
        var settings = new ApiSettings
        {
            EndpointUrl = "https://api.example.test/power/battery",
            AuthHeaderName = "X-Auth-Key",
            AuthKey = "invalid-key"
        };

        var client = new PowerStatusClient(httpClient, settings);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetPowerStatusAsync());
    }

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responder(request));
        }
    }
}
