using Extender.Debugging;
using Extender.WPF;
using Microsoft.WindowsAPICodePack.Dialogs;
using Slate_EK.Properties;
using System;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public class SettingsViewModel : ViewModel
    {
        public string       WindowTitle           => $"{Settings.Default.ShortTitle} - Settings";
        public Visibility   DebugOptionsVisibilty => EnableDebug ? Visibility.Visible : Visibility.Collapsed;

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
                OnPropertyChanged(nameof(DebugOptionsVisibilty));
            }
        }

        public bool ShowInventoryFastenerToolTips
        {
            get { return Settings.Default.ShowInvFastenerToolTip; }
            set
            {
                Settings.Default.ShowInvFastenerToolTip = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(ShowInventoryFastenerToolTips));
            }
        }

        public bool AlignDescriptionsBom
        {
            get { return Settings.Default.AlignDescriptionsBom; }
            set
            {
                Settings.Default.AlignDescriptionsBom = value;
                Settings.Default.Save();
                RestartRequired = true;
                OnPropertyChanged(nameof(AlignDescriptionsBom));
            }
        }

        public bool AlignDescriptionsPrint
        {
            get { return Settings.Default.AlignDescriptionsPrint; }
            set
            {
                Settings.Default.AlignDescriptionsPrint = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(AlignDescriptionsPrint));
            }
        }

        public bool IncludePrintHeaders
        {
            get { return Settings.Default.IncludePrintHeaders; }
            set
            {
                Settings.Default.IncludePrintHeaders = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(IncludePrintHeaders));
            }
        }

        public int PropertyRefreshInterval
        {
            get { return Settings.Default.PropertyRefreshInterval; }
            set
            {
                Settings.Default.PropertyRefreshInterval = value;
                Settings.Default.Save();
                RestartRequired = true;
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
                RestartRequired = true;
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
                RestartRequired = true;
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
                RestartRequired = true;
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

        public bool DebugRedirectConsoleOut
        {
            get { return Settings.Default.DebugRedirectConsoleOut; }
            set
            {
                Settings.Default.DebugRedirectConsoleOut = value;
                Settings.Default.Save();
                RestartRequired = true;
                OnPropertyChanged(nameof(DebugRedirectConsoleOut));
            }
        }

        public string PrintFontFamily
        {
            get { return Settings.Default.PrintFontFamily; }
            set
            {
                Settings.Default.PrintFontFamily = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintFontFamily));
            }
        }

        public int PrintFontSize
        {
            get { return Settings.Default.PrintFontSize; }
            set
            {
                Settings.Default.PrintFontSize = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintFontSize));
            }
        }

        public int PrintLineHeight
        {
            get { return Settings.Default.PrintFontLineHeight; }
            set
            {
                Settings.Default.PrintFontLineHeight = value; 
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintLineHeight));
            }
        }

        public int PrintPagePadding
        {
            get { return Settings.Default.PrintPagePadding; }
            set
            {
                Settings.Default.PrintPagePadding = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintPagePadding));
            }
        }
        public int PrintNumColumns
        {
            get { return Settings.Default.PrintNumColumns; }
            set
            {
                Settings.Default.PrintNumColumns = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintNumColumns));
            }
        }

        public float PrintPageHeight
        {
            get { return Settings.Default.PrintPageHeight; }
            set
            {
                Settings.Default.PrintPageHeight = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintPageHeight));
            }
        }
        public float PrintPageWidth
        {
            get { return Settings.Default.PrintPageWidth; }
            set
            {
                Settings.Default.PrintPageWidth = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintPageWidth));
            }
        }

        public int PrintDpi
        {
            get { return Settings.Default.PrintDpi; }
            set
            {
                Settings.Default.PrintDpi = value;
                Settings.Default.Save();
                OnPropertyChanged(nameof(PrintDpi));
            }
        }

        public string StatusBarText
        {
            get { return _StatusBarText; }
            set
            {
                _StatusBarText = value;
                OnPropertyChanged(nameof(StatusBarText));
            }
        }

        public string IndicatorBarText
        {
            get { return _IndicatorBarText; }
            set
            {
                _IndicatorBarText = value;
                OnPropertyChanged(nameof(IndicatorBarText));
            }
        }

        public bool RestartRequired
        {
            get { return _RestartRequired; }
            set
            {
                _RestartRequired = value;
                OnPropertyChanged(nameof(RestartRequired));
                OnPropertyChanged(nameof(RestartIndicatorVisibility));
            }
        }

        public Visibility RestartIndicatorVisibility => RestartRequired ? Visibility.Visible : Visibility.Hidden;

        private string _StatusBarText;
        private string _IndicatorBarText;
        private bool   _RestartRequired;

        public ICommand BrowseInvFileCommand   { get; private set; }
        public ICommand BrowseBomFolderCommand { get; private set; }

        public SettingsViewModel()
        {
            base.Initialize();
            RestartRequired = false;

            BrowseInvFileCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog()
                    {
                        AddExtension = true,
                        DefaultExt = "mdf",
                        CheckFileExists = false,
                        CheckPathExists = false,
                        InitialDirectory = Path.GetDirectoryName(DefaultInventoryPath)
                    };

                    var result = dialog.ShowDialog();
                    if (!result.HasValue || !result.Value) return;

                    DefaultInventoryPath = dialog.FileName;
                }
            );

            BrowseBomFolderCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new Microsoft.WindowsAPICodePack.Dialogs.CommonOpenFileDialog()
                    {
                        IsFolderPicker   = true,
                        Multiselect      = false,
                        Title            = @"Select BOM autosave folder",
                        InitialDirectory = Path.Combine(Directory.GetCurrentDirectory(), AssemblyAutosaveFolder)
                    };

                    if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        Uri selected = new Uri(AppendSlash(dialog.FileName));
                        Uri cwd      = new Uri(AppendSlash(Directory.GetCurrentDirectory()));
                        Uri rel      = cwd.MakeRelativeUri(selected);

                        AssemblyAutosaveFolder = rel.ToString().Replace('/', Path.DirectorySeparatorChar);

                        Debug.WriteMessage($"Selected: {selected}");
                        Debug.WriteMessage($"Cwd     : {cwd}");
                        Debug.WriteMessage($"Relative: {rel}");
                    }
                }
            );
        }

        private string AppendSlash(string path)
        {
            return $"{path}\\";
        }
    }
}
