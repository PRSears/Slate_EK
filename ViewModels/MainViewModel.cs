using Extender.WPF;
using Slate_EK.Views;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public class MainViewModel : ViewModel
    {
        public string AssemblyNumber
        {
            get
            {
                return _AssemblyNumber;
            }
            set
            {
                _AssemblyNumber = value;
                OnPropertyChanged("AssemblyNumber");
            }
        }

        public string WindowTitle
        {
            get
            {
                return Properties.Settings.Default.AppTitle;
            }
        }

        public ICommand LoadExistingCommand         { get; private set; }
        public ICommand CreateNewBomCommand         { get; private set; }
        public ICommand OpenSettingsEditorCommand   { get; private set; }
        public ICommand ExitAllCommand              { get; private set; }
        public ICommand TestHarnessCommand          { get; private set; }

        public bool WindowsMenuEnabled
        {
            get
            {
                return this.WindowManager.ChildOpen();
            }
        }
        
        public WindowManager WindowManager;

        private string _AssemblyNumber;

        //TODO Make closing the main window close all children. Make sure to get confirmation from user.

        public MainViewModel()
        {
            this.WindowManager = new Extender.WPF.WindowManager();

            this.WindowManager.WindowOpened += (s, w) => OnPropertyChanged("WindowsMenuEnabled");
            this.WindowManager.WindowClosed += (s, w) => OnPropertyChanged("WindowsMenuEnabled");

            TestHarnessCommand = new RelayCommand(() => Models.Fastener.TestHarness());

            LoadExistingCommand = new RelayCommand
            (
                () => System.Windows.MessageBox.Show("Loading not implemented.")
            );

            CreateNewBomCommand = new RelayCommand
            (
                () =>
                {
                    this.WindowManager.OpenWindow(new BomView(AssemblyNumber), true);
                    this.AssemblyNumber = string.Empty;
                },
                () => !string.IsNullOrWhiteSpace(AssemblyNumber)
            );

            OpenSettingsEditorCommand = new RelayCommand
            (
                () => System.Windows.MessageBox.Show("Settings not implemented.")
            );

            ExitAllCommand = new RelayCommand
            (
                () =>
                {
                    if (Extender.WPF.ConfirmationDialog.Show("Confirm exit", "Are you sure you want to close the application?"))
                    {
                        this.WindowManager.CloseAll();
                        this.CloseCommand.Execute(null);
                    }
                }
            );

            this.AssemblyNumber = string.Empty;

            this.CheckXML();
        }

        public void CheckXML()
        {
            Slate_EK.Models.IO.Sizes xmlSizes = new Models.IO.Sizes();
            Slate_EK.Models.IO.Pitches xmlPitches = new Models.IO.Pitches();

            if(!System.IO.File.Exists(xmlSizes.FilePath))
            {
                xmlSizes.Add(new Models.Size(DefaultSize));
            }

            if(!System.IO.File.Exists(xmlPitches.FilePath))
            {
                xmlPitches.Add(new Models.Pitch(DefaultPitch));
            }
        }

        #region #settings.settings aliases
        public double DefaultSize
        {
            get
            {
                return Properties.Settings.Default.DefaultSize;
            }
        }

        public double DefaultPitch
        {
            get
            {
                return Properties.Settings.Default.DefaultPitch;
            }
        }

        public bool DEBUG
        {
            get
            {
                return Properties.Settings.Default.Debug;
            }
        }

        public System.Windows.Visibility DebugControlsVisibility
        {
            get
            {
                return DEBUG ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;
            }
        }
        #endregion
    }
}
