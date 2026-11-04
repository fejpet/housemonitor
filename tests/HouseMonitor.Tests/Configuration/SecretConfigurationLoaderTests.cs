using HouseMonitor.Core.Configuration;

namespace HouseMonitor.Tests.Configuration;

public class SecretConfigurationLoaderTests
{
    [Fact]
    public void LoadFromJson_ValidConfiguration_ReturnsExpectedApiSettings()
    {
        // Arrange (REQ-009)
        const string json = """
            {
              "ApiSettings": {
                "EndpointUrl": "https://api.example.test/power/battery",
                "AuthHeaderName": "X-Auth-Key",
                "AuthKey": "test-secret-api-key"
              }
            }
            """;

        // Act
        var settings = SecretConfigurationLoader.LoadFromJson(json);

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("https://api.example.test/power/battery", settings.EndpointUrl);
        Assert.Equal("X-Auth-Key", settings.AuthHeaderName);
        Assert.Equal("test-secret-api-key", settings.AuthKey);
    }

    [Fact]
    public void LoadFromJson_PlaceholderEndpointUrl_ThrowsInvalidOperationException()
    {
        // Arrange (REQ-009, REQ-010)
        const string exampleJson = """
            {
              "ApiSettings": {
                "EndpointUrl": "<YOUR_ENDPOINT_URL>",
                "AuthHeaderName": "X-Auth-Key",
                "AuthKey": "test-secret-api-key"
              }
            }
            """;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => SecretConfigurationLoader.LoadFromJson(exampleJson));
    }

    [Fact]
    public void LoadFromJson_PlaceholderAuthKey_ThrowsInvalidOperationException()
    {
        // Arrange (REQ-009, REQ-010)
        const string exampleJson = """
            {
              "ApiSettings": {
                "EndpointUrl": "https://api.example.test/power/battery",
                "AuthHeaderName": "X-Auth-Key",
                "AuthKey": "<YOUR_SECRET_API_KEY>"
              }
            }
            """;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => SecretConfigurationLoader.LoadFromJson(exampleJson));
    }

    [Fact]
    public async Task LoadFromFileAsync_ExistingFile_LoadsApiSettingsSuccessfully()
    {
        // Arrange (REQ-009)
        var tempFile = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(tempFile, """
                {
                  "ApiSettings": {
                    "EndpointUrl": "https://api.example.test/power/battery",
                    "AuthHeaderName": "X-Auth-Key",
                    "AuthKey": "real-secret-key"
                  }
                }
                """);

            // Act
            var settings = await SecretConfigurationLoader.LoadFromFileAsync(tempFile);

            // Assert
            Assert.Equal("https://api.example.test/power/battery", settings.EndpointUrl);
            Assert.Equal("real-secret-key", settings.AuthKey);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task LoadFromFileAsync_MissingFile_ThrowsFileNotFoundException()
    {
        // Act & Assert (REQ-009)
        await Assert.ThrowsAsync<FileNotFoundException>(() =>
            SecretConfigurationLoader.LoadFromFileAsync("/nonexistent/path/secret.json"));
    }

    [Fact]
    public void ResolveSecretFilePath_FindsSecretJsonInAncestorDirectory()
    {
        // Arrange (REQ-009)
        var tempRoot = Path.Combine(Path.GetTempPath(), $"hm-test-{Guid.NewGuid():N}");
        var nestedDir = Path.Combine(tempRoot, "src", "HouseMonitor.App", "bin", "Debug", "net8.0");
        Directory.CreateDirectory(nestedDir);
        var expectedSecretPath = Path.Combine(tempRoot, "secret.json");
        File.WriteAllText(expectedSecretPath, "{}");

        try
        {
            // Act
            var resolved = SecretConfigurationLoader.ResolveSecretFilePath(nestedDir);

            // Assert
            Assert.Equal(expectedSecretPath, resolved);
        }
        finally
        {
            Directory.Delete(tempRoot, recursive: true);
        }
    }
}
