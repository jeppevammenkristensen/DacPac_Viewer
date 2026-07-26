using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using DacPac.UI.Infrastructure;
using DacPac.UI.Infrastructure.LongRunning;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using TruePath;

namespace DacPac.UI.ViewModels;

public partial class InstallationViewModel : ScreenPage
{
    private readonly IMessenger _messenger;
    private string _title = "Installation";

    public InstallationViewModel(IMessenger messenger)
    {
        _messenger = messenger;
    }
    
    public override string Title => _title;
    public AbsolutePath[]? Paths { get; set; }

    public void SetPackages(AbsolutePath[] messageValue)
    {
        Paths = messageValue;
        DatabaseName = Paths[0].GetFilenameWithoutExtension();
    }

    public override Task OnActivatedAsync()
    {
        return base.OnActivatedAsync();
    }

    [NotifyCanExecuteChangedFor(nameof(TestCommand))]
    [NotifyCanExecuteChangedFor(nameof(InstallCommand))]
    [ObservableProperty]
    public partial string? MasterConnectionString { get; set; }
    
    [ObservableProperty]
    public partial string DatabaseName { get; set; }

    [ObservableProperty] public partial string Status { get; set; } 

    private bool CanExecuteTest()
    {
        return !string.IsNullOrWhiteSpace(MasterConnectionString);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteTest))]
    private async Task Test()
    {
        Status = string.Empty;
        
        try
        {
            var r = new SqlConnectionStringBuilder(MasterConnectionString);
        }
        catch (Exception e)
        {
            Status += $"{e.Message}\r\n";
            return;
        }
        
        var sqlConnection = new SqlConnection(MasterConnectionString);
        try
        {
            await sqlConnection.OpenAsync();
            _messenger.SendSuccess("Successfully established connection");   
        }
        catch (Exception e)
        {
            _messenger.SendError($"Cannot establish connection. {e.Message}");
        }
    }

    private bool CanExecuteInstall()
    {
        return !string.IsNullOrWhiteSpace(MasterConnectionString);
    }

    [RelayCommand(CanExecute = nameof(CanExecuteInstall))]
    private async Task Install()
    {
        Status = string.Empty;

        try
        {
            await Task.Run(() =>
            {
                var services = new Microsoft.SqlServer.Dac.DacServices(MasterConnectionString);
                services.Message += (object? sender, Microsoft.SqlServer.Dac.DacMessageEventArgs eventArgs) =>
                {
                    Dispatcher.UIThread.Post(() => Status += $"{eventArgs.Message}\r\n");
                };

                foreach (var absolutePath in Paths ?? [])
                {
                    var dacpac = DacPackage.Load(absolutePath.Value);
                    //publish options
                    var dacDeployOptions = new DacDeployOptions()
                    {
                        ScriptDatabaseOptions = false, // <------- this is ignored!
                        BlockOnPossibleDataLoss = false, //All these other options are respected. 
                        IgnoreColumnOrder = true,
                        AllowIncompatiblePlatform = true,
                        ExcludeObjectTypes = [ObjectType.Users, ObjectType.RoleMembership, ObjectType.Permissions, ObjectType.Logins
                        ],
                        TreatVerificationErrorsAsWarnings = true
                    };

                    services.Deploy(dacpac, DatabaseName, true, dacDeployOptions);
                    _messenger.SendSuccess("Installed database");
                }
            });
        }
        catch (Exception ex)
        {
            _messenger.SendError($"Failed to install {ex.Message}");
        }
    }
    
}
