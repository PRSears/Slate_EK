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
                case nameof(ConfirmBeforeClosingBlock):
                    ViewModel.StatusBarText = "A pop-up will appear to confirm you want to close everything.";
                    FadeTimer.Stop();
                    break;
                case nameof(ShowDescBlock):
                    ViewModel.StatusBarText = "A tool-tip will appear with the short description of the fastener under the mouse.";
                    FadeTimer.Stop();
                    break;
                case nameof(AlignBomBlock):
                    ViewModel.StatusBarText = "Each property of the fastener will take up a fixed width, effectively aligning them by column in a BOM.";
                    FadeTimer.Stop();
                    break;
                case nameof(AlignPrintBlock):
                    ViewModel.StatusBarText = "Each property of the fastener will take up a fixed width, effectively aligning them by column when printing.";
                    FadeTimer.Stop();
                    break;
                case nameof(AlignExportBlock):
                    ViewModel.StatusBarText = "Each property of the fastener will take up a fixed width, effectively aligning them by column when exporting a BOM to a file.";
                    FadeTimer.Stop();
                    break;
                case nameof(ExportLengthFractionsBlock):
                    ViewModel.StatusBarText = "Converts imperial lengths to fractions when exporting or printing a BOM.";
                    FadeTimer.Stop();
                    break;
                case nameof(IncludePrintHeadersBlock):
                    ViewModel.StatusBarText = "Header labels will be included at the top of the first page when printing BOMs.";
                    FadeTimer.Stop();
                    break;
                case nameof(DropdownIntervalBlock):
                    ViewModel.StatusBarText = "The time - in milliseconds - between refreshes from xml for the size and pitch drop downs.";
                    FadeTimer.Stop();
                    break;
                case nameof(FormatSpecBlock):
                    ViewModel.StatusBarText = "String formatter for displaying decimal values. (Search 'MSDN Standard Numeric Format Strings')";
                    FadeTimer.Stop();
                    break;
                case nameof(PitchNameBlock):
                    ViewModel.StatusBarText = "The name of the xml file to load fastener pitch options from. Don't include the path - it must be located in Lists folder.";
                    FadeTimer.Stop();
                    break;
                case nameof(SizeNameBlock):
                    ViewModel.StatusBarText = "The name of the xml file to load fastener size options from. Don't include the path - it must be located in Lists folder.";
                    FadeTimer.Stop();
                    break;
                case nameof(BomNameBlock):
                    ViewModel.StatusBarText = @"String format for individual BOM file names -- '{0}' is replaced with the assembly number.";
                    FadeTimer.Stop();
                    break;
                case nameof(BomFolderBlock):
                    ViewModel.StatusBarText = "Path to the folder where BOMs are stored, relative to this application's root folder.";
                    FadeTimer.Stop();
                    break;
                case nameof(InvPathBlock):
                    ViewModel.StatusBarText = "Full path (including file name and extension) to the inventory database that gets loaded by default.";
                    FadeTimer.Stop();
                    break;
                case nameof(FontFamilyBlock):
                    ViewModel.StatusBarText = "The name of the font to use for printing.";
                    FadeTimer.Stop();
                    break;
                case nameof(FontSizeBlock):
                    ViewModel.StatusBarText = "The size of the font to use for printing.";
                    FadeTimer.Stop();
                    break;
                case nameof(FontLineHeightBlock):
                    ViewModel.StatusBarText = "The height of each line used for printing. Higher values put more whitespace between the lines of text.";
                    FadeTimer.Stop();
                    break;
                case nameof(PrintPaddingBlock):
                    ViewModel.StatusBarText = "Border padding (in pixels) for printing.";
                    FadeTimer.Stop();
                    break;
                case nameof(PrintPageHeightBlock):
                    ViewModel.StatusBarText = "Height of the paper used for printing, in inches.";
                    FadeTimer.Stop();
                    break;
                case nameof(PrintPageWidthBlock):
                    ViewModel.StatusBarText = "Width of the paper used for printing, in inches.";
                    FadeTimer.Stop();
                    break;
                case nameof(PrintNumColumnsBlock):
                    ViewModel.StatusBarText = "The number of columns to include on each page of the printed BOM.";
                    FadeTimer.Stop();
                    break;
                case nameof(PrintDpiBlock):
                    ViewModel.StatusBarText = "The print resolution used by your printer.";
                    FadeTimer.Stop();
                    break;
                case nameof(LowStockThreshBlock):
                    ViewModel.StatusBarText = "The stock quantity threshold at or below which an alert should be generated to notify of low stock.";
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
