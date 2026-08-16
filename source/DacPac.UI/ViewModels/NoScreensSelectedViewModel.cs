using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DacPac.UI.ApplicationLayer.Infrastructure;
using TruePath;

namespace DacPac.UI.ViewModels;

public interface ICommandButton 
{
    /// <summary>
    /// Gets the text displayed on the button.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets the command executed when the button is clicked.
    /// </summary>
    public ICommand RunCommand { get; }

    /// <summary>
    /// Gets whether the button is displayed.
    /// </summary>
    public bool IsVisible { get; }

    /// <summary>
    /// Gets the button tooltip.
    /// </summary>
    public string? ToolTip { get; }

    /// <summary>
    /// Gets whether the button uses the accent style.
    /// </summary>
    public bool IsAccent { get; }
}

/// <summary>
/// Provides actions to display when no application screens are open.
/// </summary>
public partial class NoScreensSelectedViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;

    /// <summary>
    /// Represents an action button displayed when no screens are open.
    /// </summary>
    public partial class NoScreensSelectedButton : ObservableObject, ICommandButton
    {
        private readonly MainWindowViewModel _root;

        /// <summary>
        /// Initializes a new empty-screen action button.
        /// </summary>
        public NoScreensSelectedButton(RecentDacpacFiles? path, MainWindowViewModel root, bool isVisible = true, string? toolTip = null,
            bool isAccent = false)
        {
            _root = root;
            Text = path?.Title ?? "Open dacpac...";
            Path = path;
            IsVisible = isVisible;
            ToolTip = toolTip;
            IsAccent = isAccent;
        }

        private bool CanExecuteRun()
        {
            return true;
        }
        
        

        [RelayCommand(CanExecute = nameof(CanExecuteRun))]
        private async Task RunIt()
        {
            await _root.OpenDacpacMenuItemCommand.ExecuteAsync(new OpenDacpacMenuItemData(Path, null));
        }
        
        /// <summary>
        /// Gets the text displayed on the button.
        /// </summary>
        public string Text { get; }

        ICommand ICommandButton.RunCommand => RunItCommand;

        public RecentDacpacFiles Path { get; }

        /// <summary>
        /// Gets whether the button is displayed.
        /// </summary>
        public bool IsVisible { get; }

        /// <summary>
        /// Gets the button tooltip.
        /// </summary>
        public string? ToolTip { get; }

        /// <summary>
        /// Gets whether the button uses the accent style.
        /// </summary>
        public bool IsAccent { get; }
    }

    private MainWindowViewModel? _mainWindowViewModel;

    /// <summary>
    /// Initializes a new instance of the <see cref="NoScreensSelectedViewModel"/> class.
    /// </summary>
    public NoScreensSelectedViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Gets the buttons displayed in the empty-screen view.
    /// </summary>
    public ObservableCollection<ICommandButton> Buttons { get; } = [];


    private void UpdateButtons()
    {
        if (_mainWindowViewModel is null)
            return;
        
        Buttons.Clear();        
       Buttons.Add(new NoScreensSelectedButton(null, _mainWindowViewModel, toolTip: "Open a new dacpac"));
       
       foreach (var storedPath in _settingsService.GetStoredPaths())
       {
           Buttons.Add(new NoScreensSelectedButton(new RecentDacpacFiles(storedPath), _mainWindowViewModel));
       }
    }
    
    /// <summary>
    /// Sets the main window that handles empty-screen actions.
    /// </summary>
    public void SetMainWindowViewModel(MainWindowViewModel mainWindowViewModel)
    {
        _mainWindowViewModel = mainWindowViewModel;
        UpdateButtons();
    }

    private bool CanExecuteOpen()
    {
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanExecuteOpen))]
        private async Task Open()
        {
            if (_mainWindowViewModel is not null)
                await _mainWindowViewModel.OpenDacpacCommand.ExecuteAsync(null);
        }
}
