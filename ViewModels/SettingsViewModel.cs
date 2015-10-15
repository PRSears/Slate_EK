using Extender.WPF;
using Slate_EK.Properties;
using System.Windows;

namespace Slate_EK.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        public string WindowTitle => $"{Settings.Default.ShortTitle} - Settings";

        public bool ConfirmBeforeClosing
        {
            get { return Settings.Default.ConfirmClose; }
            set
            {
                Settings.Default.ConfirmClose = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(ConfirmBeforeClosing));
            }
        }

        public bool EnableDebug
        {
            get { return Settings.Default.Debug; }
            set
            {
                Settings.Default.Debug = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(EnableDebug));
            }
        }

        public int PropertyRefreshInterval
        {
            get { return Settings.Default.PropertyRefreshInterval; }
            set
            {
                Settings.Default.PropertyRefreshInterval = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PropertyRefreshInterval));
            }
        }

        public string PitchListFilename
        {
            get { return Settings.Default.PitchesFilename; }
            set
            {
                Settings.Default.PitchesFilename = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PitchListFilename));
            }
        }

        public string SizeListFilename
        {
            get { return Settings.Default.SizesFilename; }
            set
            {
                Settings.Default.SizesFilename = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(SizeListFilename));
            }
        }

        public string BomFilenameFormat
        {
            get { return Settings.Default.BomFilenameFormat; }
            set
            {
                Settings.Default.BomFilenameFormat = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(BomFilenameFormat));
            }
        }

        public string AssemblyAutosaveFolder
        {
            get { return Settings.Default.DefaultAssembliesFolder; }
            set
            {
                Settings.Default.DefaultAssembliesFolder = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(AssemblyAutosaveFolder));
            }
        }

        public string DefaultInventoryPath
        {
            get { return Settings.Default.DefaultInventoryPath; }
            set
            {
                Settings.Default.DefaultInventoryPath = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(DefaultInventoryPath));
            }
        }

        public string DebugLogFilename
        {
            get { return Settings.Default.DebugFilename; }
            set
            {
                Settings.Default.DebugFilename = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(DebugLogFilename));
            }
        }

        public string FloatFormatSpecifier
        {
            get { return Settings.Default.FloatFormatSpecifier; }
            set
            {
                Settings.Default.FloatFormatSpecifier = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(FloatFormatSpecifier));
            }
        }

        public Visibility DebugOptionsVisibilty => EnableDebug ? Visibility.Visible : Visibility.Hidden;
    }
}
