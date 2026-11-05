using System.Globalization;
using System.Text.Json;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Tests.Models;

public class PowerStatusSerializationTests
{
    private const string SampleJson = """
        {
          "battery": { "soc": 65.0, "state": "discharging" },
          "generator": { "state": "stopped", "today": 0 },
          "solar": { "production": 0.0, "today": 17.98 },
          "house": { "consumption": 415.0, "today_consumtion": 13.38 },
          "timestamp": "2026-11-04T22:20:35.014353+01:00"
        }
        """;

    [Fact]
    public void Deserialize_ValidSampleJson_MapsRootAllSectionsAndTimestampCorrectly()
    {
        // Act (REQ-003, REQ-011, REQ-012, REQ-013, REQ-014, REQ-019)
        var status = PowerStatus.FromJson(SampleJson);

        // Assert - Root (REQ-003)
        Assert.NotNull(status);
        Assert.NotNull(status.Battery);
        Assert.NotNull(status.Generator);
        Assert.NotNull(status.Solar);
        Assert.NotNull(status.House);

        // Assert - Battery (REQ-011)
        Assert.Equal(65.0, status.Battery.Soc);
        Assert.Equal("discharging", status.Battery.State);

        // Assert - Generator (REQ-012)
        Assert.Equal("stopped", status.Generator.State);
        Assert.Equal(0.0, status.Generator.Today);

        // Assert - Solar (REQ-013)
        Assert.Equal(0.0, status.Solar.Production);
        Assert.Equal(17.98, status.Solar.Today);

        // Assert - House (REQ-014: mapped from "today_consumtion")
        Assert.Equal(415.0, status.House.Consumption);
        Assert.Equal(13.38, status.House.TodayConsumption);

        // Assert - Timestamp (REQ-019)
        var expectedTimestamp = DateTimeOffset.Parse(
            "2026-11-04T22:20:35.014353+01:00",
            CultureInfo.InvariantCulture);
        Assert.Equal(expectedTimestamp, status.Timestamp);
    }

    [Fact]
    public void Deserialize_MissingTimestamp_ThrowsJsonException()
    {
        // Arrange (REQ-019)
        const string jsonWithoutTimestamp = """
            {
              "battery": { "soc": 65.0, "state": "discharging" },
              "generator": { "state": "stopped", "today": 0 },
              "solar": { "production": 0.0, "today": 17.98 },
              "house": { "consumption": 415.0, "today_consumtion": 13.38 }
            }
            """;

        // Act & Assert
        Assert.Throws<JsonException>(() => PowerStatus.FromJson(jsonWithoutTimestamp));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Deserialize_EmptyOrWhitespaceJson_ThrowsArgumentException(string invalidJson)
    {
        // Act & Assert (REQ-003)
        Assert.ThrowsAny<ArgumentException>(() => PowerStatus.FromJson(invalidJson));
    }

    [Fact]
    public void Deserialize_MalformedJson_ThrowsJsonException()
    {
        // Act & Assert (REQ-003)
        Assert.Throws<JsonException>(() => PowerStatus.FromJson("{ invalid json "));
    }
}
