using HouseMonitor.Core.Clients;
using HouseMonitor.Core.Configuration;
using HouseMonitor.Core.Presentation;

var secretPath = SecretConfigurationLoader.ResolveSecretFilePath();
var settings = await SecretConfigurationLoader.LoadFromFileAsync(secretPath);

using var httpClient = new HttpClient();
var client = new PowerStatusClient(httpClient, settings);

var status = await client.GetPowerStatusAsync();
var lines = StatusDisplayFormatter.FormatSummaryLines(status);
var position = OverlayPositionCalculator.CalculateBottomRight(
    screenWidth: 1920,
    screenHeight: 1080,
    windowWidth: 320,
    windowHeight: 140);

Console.WriteLine($"[HouseMonitor Overlay @ Bottom-Right ({position.X}, {position.Y})]");
Console.WriteLine(new string('-', 52));
foreach (var line in lines)
{
    Console.WriteLine(line);
}
Console.WriteLine(new string('-', 52));
