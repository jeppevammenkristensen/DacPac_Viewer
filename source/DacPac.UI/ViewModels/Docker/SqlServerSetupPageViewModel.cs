using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.Core;
using DacPac.UI.Infrastructure;
using FileBasedApp.Toolkit.SimpleExec;

namespace DacPac.UI.ViewModels.Docker;

/// <summary>
/// Provides the input fields and future actions for a local Docker SQL Server setup.
/// </summary>
public partial class SqlServerSetupPageViewModel(
    IDockerService dockerService,
    IConfirmationDialogService confirmationDialogService,
    IClipboardService clipboard,
    IMessenger messenger) : ValidatingScreenPage
{
    /// <summary>
    /// Gets the title displayed by the setup tab.
    /// </summary>
    public override string Title => "Docker SQL Server";

    [ObservableProperty]
    [Required(ErrorMessage = "Container name is required.")]
    [RegularExpression("^[a-z0-9][a-z0-9_.-]*$", ErrorMessage = "Use lowercase letters, numbers, dots, underscores, or hyphens; start with a letter or number.")]
    public partial string ContainerName { get; set; } = "dacpac-sqlserver";

    [ObservableProperty]
    [Range(1, 65535, ErrorMessage = "Host port must be between 1 and 65535.")]
    public partial int HostPort { get; set; } = 1433;

    [ObservableProperty]
    [Required(ErrorMessage = "SA password is required.")]
    [MinLength(8, ErrorMessage = "SA password must be at least 8 characters.")]
    [MaxLength(128, ErrorMessage = "SA password must be no more than 128 characters.")]
    [CustomValidation(typeof(SqlServerSetupPageViewModel), nameof(ValidateSaPassword))]
    public partial string SaPassword { get; set; } = "YourStrong!Password123";

    [ObservableProperty] public partial bool IsPasswordVisible { get; set; }

    [ObservableProperty] public partial bool PersistData { get; set; } = true;

    partial void OnContainerNameChanged(string value) => ValidateProperty(value, nameof(ContainerName));

    partial void OnHostPortChanged(int value) => ValidateProperty(value, nameof(HostPort));

    partial void OnSaPasswordChanged(string value) => ValidateProperty(value, nameof(SaPassword));

    /// <summary>
    /// Records that the container creation workflow has not yet been implemented.
    /// </summary>
    [RelayCommand]
    private async Task StartContainer()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            messenger.SendInformation("Correct the validation errors before creating the container.");
            return;
        }

        var containersList = await dockerService.ListContainers().ToListAsync();
        var existingContainer = containersList.FirstOrDefault(x => x.Names.Contains(ContainerName));
        if (existingContainer is not null)
        {
            var shouldReplace = await confirmationDialogService.ConfirmAsync(
                new ConfirmationDialogRequest(
                    "Replace existing container?",
                    "A Docker container with this name already exists. Replacing it will permanently remove the existing container and its data.",
                    "Replace container"));

            if (!shouldReplace)
            {
                return;
            }

            await RemoveContainer(existingContainer);
            return;
        }

        if (containersList.Any(x => HasPublishedHostPort(x.Ports, HostPort)))
        {
            messenger.SendInformation("Port already in use.");
            return;
        }

        await CreateContainer();
    }

    /// <summary>
    /// Removes an existing Docker container before recreating it with the configured settings.
    /// </summary>
    private async Task RemoveContainer(Containers container)
    {
        await SimpleExecRunner.Init("docker")
            .AddArgument("rm")
            .AddArgument("-f")
            .AddArgument(container.ID)
            .ReadAsync();

        await CreateContainer();
    }

    /// <summary>
    /// Creates the SQL Server Docker container using the configured settings.
    /// </summary>
    private async Task CreateContainer()
    {
        await SimpleExecRunner.Init("docker")
            .AddArgument("run")
            .AddArgumentPair("--name", ContainerName)
            .AddArgumentPair("-e", "ACCEPT_EULA=Y")
            .AddArgumentPair("-e", $"MSSQL_SA_PASSWORD={SaPassword}")
            .AddArgumentPair("-p", $"{HostPort}:1433")
            .AddArgumentPair("-v", $"{ContainerName} not connected yet.")
            .AddArgumentPair("-d", "mcr.microsoft.com/mssql/server:2025-latest")
            .WithEchoPrefix("Docker-Setup")
            .ReadAsync();

        await GenerateAndCopyConnectionstring();
        
        messenger.SendSuccess("Container created successfully.");
    }

    /// <summary>
    /// Generates a connection string for the new local SQL Server container and copies it to the clipboard.
    /// </summary>
    private async Task GenerateAndCopyConnectionstring()
    {
        var connectionString =
            $"Server=localhost,{HostPort};User ID=sa;Password={SaPassword};TrustServerCertificate=True;";

        await clipboard.SetTextAsync(connectionString);
        messenger.SendSuccess("Connection string copied to clipboard.");
    }

    /// <summary>
    /// Validates the SQL Server administrator password policy.
    /// </summary>
    public static ValidationResult? ValidateSaPassword(string? password, ValidationContext _)
    {
        if (string.IsNullOrEmpty(password) || CountPasswordCharacterClasses(password) >= 3)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(
            "SA password must include at least three of: uppercase letters, lowercase letters, numbers, and symbols.");
    }

    /// <summary>
    /// Counts the SQL Server password character classes represented in a password.
    /// </summary>
    private static int CountPasswordCharacterClasses(string password)
    {
        var hasUppercase = password.Any(char.IsUpper);
        var hasLowercase = password.Any(char.IsLower);
        var hasDigit = password.Any(char.IsDigit);
        var hasSymbol = password.Any(character => !char.IsLetterOrDigit(character));

        return (hasUppercase ? 1 : 0)
            + (hasLowercase ? 1 : 0)
            + (hasDigit ? 1 : 0)
            + (hasSymbol ? 1 : 0);
    }

    /// <summary>
    /// Determines whether Docker has published the specified host port in a container's port mapping.
    /// </summary>
    private static bool HasPublishedHostPort(string ports, int hostPort)
    {
        return DockerPublishedPortRegex()
            .Matches(ports)
            .Any(match => match.Groups["port"].Value == hostPort.ToString());
    }

    [GeneratedRegex(@"(?:^|,\s*)(?:\[[^\]]+\]|[^,:]+):(?<port>\d+)->", RegexOptions.CultureInvariant)]
    private static partial Regex DockerPublishedPortRegex();

}
