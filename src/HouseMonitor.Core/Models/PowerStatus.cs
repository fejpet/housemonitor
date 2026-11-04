using System.Text.Json;
using System.Text.Json.Serialization;

namespace HouseMonitor.Core.Models;

/// <summary>
/// Root domain object representing the power telemetry payload (REQ-003, REQ-019).
/// </summary>
public sealed record PowerStatus(
    [property: JsonPropertyName("battery")] BatteryStatus Battery,
    [property: JsonPropertyName("generator")] GeneratorStatus Generator,
    [property: JsonPropertyName("solar")] SolarStatus Solar,
    [property: JsonPropertyName("house")] HouseStatus House,
    [property: JsonPropertyName("timestamp")] DateTimeOffset Timestamp)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Deserializes and validates a JSON string into a <see cref="PowerStatus"/> instance (REQ-003, REQ-019).
    /// </summary>
    public static PowerStatus FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        var status = JsonSerializer.Deserialize<PowerStatus>(json, SerializerOptions);
        if (status is null ||
            status.Battery is null ||
            status.Generator is null ||
            status.Solar is null ||
            status.House is null ||
            status.Timestamp == default)
        {
            throw new JsonException("Deserialized PowerStatus payload is missing required sections or timestamp.");
        }

        return status;
    }
}

/// <summary>
/// Battery telemetry section (REQ-011).
/// </summary>
public sealed record BatteryStatus(
    [property: JsonPropertyName("soc")] double Soc,
    [property: JsonPropertyName("state")] string State);

/// <summary>
/// Backup generator telemetry section (REQ-012).
/// </summary>
public sealed record GeneratorStatus(
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("today")] double Today);

/// <summary>
/// Solar production telemetry section (REQ-013).
/// </summary>
public sealed record SolarStatus(
    [property: JsonPropertyName("production")] double Production,
    [property: JsonPropertyName("today")] double Today);

/// <summary>
/// House consumption telemetry section (REQ-014).
/// Note: The API JSON uses the property name "today_consumtion".
/// </summary>
public sealed record HouseStatus(
    [property: JsonPropertyName("consumption")] double Consumption,
    [property: JsonPropertyName("today_consumtion")] double TodayConsumption);
