using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using TruePath;
using Xunit;
using JsonFileSettingsService = DacPac.UI.ApplicationLayer.Infrastructure.JsonFileSettingsService;

namespace DacPac.UI.Tests.Infrastructure;

[TestSubject(typeof(JsonFileSettingsService))]
public class JsonFileSettingsServiceTest
{
    private static readonly AbsolutePath SettingsFilePath =
        AbsolutePath.Create(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) / "DacPacViewer" / "settings.json";

    [Fact]
    public void EnableBetaUpdates_DefaultsToFalse_WhenNoSettingsFileExists()
    {
        var service = new JsonFileSettingsService(new MockFileSystem(), NullLogger<JsonFileSettingsService>.Instance);

        Assert.False(service.EnableBetaUpdates);
    }

    [Fact]
    public void EnableBetaUpdates_ReturnsPersistedValue_WhenSettingsFileExists()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [SettingsFilePath.Value] = new MockFileData("""{"enableBetaUpdates":true}""")
        });

        var service = new JsonFileSettingsService(fileSystem, NullLogger<JsonFileSettingsService>.Instance);

        Assert.True(service.EnableBetaUpdates);
    }

    [Fact]
    public void EnableBetaUpdates_Setter_PersistsValueAsCamelCaseJson()
    {
        var fileSystem = new MockFileSystem();
        var service = new JsonFileSettingsService(fileSystem, NullLogger<JsonFileSettingsService>.Instance);

        service.EnableBetaUpdates = true;

        Assert.True(SettingsFilePath.FileExists(fileSystem));
        Assert.Equal("""{"enableBetaUpdates":true}""", fileSystem.File.ReadAllText(SettingsFilePath));
    }

    [Fact]
    public void EnableBetaUpdates_Setter_DoesNotWriteFile_WhenValueUnchanged()
    {
        var fileSystem = new MockFileSystem();
        var service = new JsonFileSettingsService(fileSystem, NullLogger<JsonFileSettingsService>.Instance);

        service.EnableBetaUpdates = false;

        Assert.False(SettingsFilePath.FileExists(fileSystem));
    }

    [Fact]
    public void EnableBetaUpdates_FallsBackToDefault_WhenSettingsFileContainsInvalidJson()
    {
        var fileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
        {
            [SettingsFilePath.Value] = new MockFileData("not valid json")
        });
        var logger = new RecordingLogger<JsonFileSettingsService>();

        var service = new JsonFileSettingsService(fileSystem, logger);

        Assert.False(service.EnableBetaUpdates);
        Assert.True(logger.WarningLogged);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public bool WarningLogged { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) WarningLogged = true;
        }
    }
}