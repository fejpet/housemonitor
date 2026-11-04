using System.Text.Json;
using HouseMonitor.Core.Models;

namespace HouseMonitor.Tests.Models;

public class PowerStatusSerializationTests
{
    private const string SampleJson = """
        {
          "battery": { "soc": 69.0, "state": "discharging" },
          "generator": { "state": "stopped", "today": 0 },
          "solar": { "production": 0.0, "today": 17.98 },
          "house": { "consumption": 414.0, "today_consumtion": 12.84 }
        }
        """;

    [Fact]
    public void Deserialize_ValidSampleJson_MapsRootAndAllSectionsCorrectly()
    {
        // Act (REQ-003, REQ-011, REQ-012, REQ-013, REQ-014)
        var status = PowerStatus.FromJson(SampleJson);

        // Assert - Root (REQ-003)
        Assert.NotNull(status);
        Assert.NotNull(status.Battery);
        Assert.NotNull(status.Generator);
        Assert.NotNull(status.Solar);
        Assert.NotNull(status.House);

        // Assert - Battery (REQ-011)
        Assert.Equal(69.0, status.Battery.Soc);
        Assert.Equal("discharging", status.Battery.State);

        // Assert - Generator (REQ-012)
        Assert.Equal("stopped", status.Generator.State);
        Assert.Equal(0.0, status.Generator.Today);

        // Assert - Solar (REQ-013)
        Assert.Equal(0.0, status.Solar.Production);
        Assert.Equal(17.98, status.Solar.Today);

        // Assert - House (REQ-014: mapped from "today_consumtion")
        Assert.Equal(414.0, status.House.Consumption);
        Assert.Equal(12.84, status.House.TodayConsumption);
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

