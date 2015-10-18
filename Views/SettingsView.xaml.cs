using Extender;
using Slate_EK.ViewModels;
using System;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        private SettingsViewModel ViewModel
        {
            get
            {
                if (DataContext is SettingsViewModel)
                    return (SettingsViewModel)DataContext;

                return null;
            }
            set { DataContext = value; }
        }

        private Timer FadeTimer;

        public SettingsView()
        {
            InitializeComponent();

            ViewModel = new SettingsViewModel();
            FadeTimer = new Timer(1500) {AutoReset = true};

            FadeTimer.Elapsed += (s, e) =>
            {
                Dispatcher.BeginInvoke(new Action(() => ViewModel.StatusBarText = string.Empty));
                FadeTimer.Enabled = false;
            };
        }

        private void TabControl_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.OriginalSource.IsNotA<FrameworkElement>()) return;

            var hover = e.OriginalSource as FrameworkElement;

            switch (hover?.Name)
            {
                case "ConfirmBeforeClosingBlock":
                    ViewModel.StatusBarText = "A pop-up will appear to confirm you want to close everything.";
                    FadeTimer.Stop();
                    break;
                case "ShowDescBlock":
                    ViewModel.StatusBarText = "A tool-tip will appear with the short description of the fastener under the mouse.";
                    FadeTimer.Stop();
                    break;
                case "DropdownIntervalBlock":
                    ViewModel.StatusBarText = "The time - in milliseconds - between refreshes from xml for the size and pitch drop downs.";
                    FadeTimer.Stop();
                    break;
                case "FormatSpecBlock":
                    ViewModel.StatusBarText = "String formatter for displaying decimal values. (Search 'MSDN Standard Numeric Format Strings')";
                    FadeTimer.Stop();
                    break;
                case "PitchNameBlock":
                    ViewModel.StatusBarText = "The name of the xml file to load fastener pitch options from. Don't include the path - it must be located in Lists folder.";
                    FadeTimer.Stop();
                    break;
                case "SizeNameBlock":
                    ViewModel.StatusBarText = "The name of the xml file to load fastener size options from. Don't include the path - it must be located in Lists folder.";
                    FadeTimer.Stop();
                    break;
                case "BomNameBlock":
                    ViewModel.StatusBarText = @"String format for individual BOM file names -- '{0}' is replaced with the assembly number.";
                    FadeTimer.Stop();
                    break;
                case "BomFolderBlock":
                    ViewModel.StatusBarText = "Path to the folder where BOMs are stored, relative to this application's root folder.";
                    FadeTimer.Stop();
                    break;
                case "InvPathBlock":
                    ViewModel.StatusBarText = "Full path (including file name and extension) to the inventory database that gets loaded by default.";
                    FadeTimer.Stop();
                    break;
                default:
                    if (!FadeTimer.Enabled) FadeTimer.Start();
                    break;
            }
        }

        private void RestartIndicator_MouseEnter(object sender, MouseEventArgs e)
        {
            ViewModel.StatusBarText = "One or more of the settings just changed will likely require the app to be restarted. Usually closing / opening relevant windows will be enough.";
            FadeTimer.Stop();
        }
    }
}
