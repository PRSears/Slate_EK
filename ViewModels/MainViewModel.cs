using System;
using Extender.WPF;
using Slate_EK.Models.ThreadParameters;
using Slate_EK.Views;
using System.IO;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;

namespace Slate_EK.ViewModels
{
    public class MainViewModel : ViewModel
    {
        public string AssemblyNumber
        {
            get { return _AssemblyNumber; }
            set
            {
                _AssemblyNumber = value;
                OnPropertyChanged("AssemblyNumber");
            }
        }

        public string WindowTitle => Properties.Settings.Default.AppTitle;

        public ICommand LoadExistingCommand                { get; private set; }
        public ICommand CreateNewBomCommand                { get; private set; }
        public ICommand OpenInventoryViewCommand           { get; private set; }
        public ICommand OpenSettingsEditorCommand          { get; private set; }
        public ICommand CloseAllBomWindows                 { get; private set; }
        public ICommand ExitAllCommand                     { get; private set; }
        public ICommand TestHarnessCommand                 { get; private set; }
        public ICommand FileDroppedCommand                 { get; private set; }

        public bool WindowsMenuEnabled => WindowManager.ChildOpen();

        public readonly WindowManager WindowManager;

        private string _AssemblyNumber;

        public MainViewModel()
        {
            WindowManager = new WindowManager();

            WindowManager.WindowOpened += (s, w) => OnPropertyChanged(nameof(WindowsMenuEnabled));
            WindowManager.WindowClosed += (s, w) => OnPropertyChanged(nameof(WindowsMenuEnabled));

            TestHarnessCommand = new RelayCommand
            (
                () =>
                {
                    var inv = new Inventory(Properties.Settings.Default.DefaultInventoryPath);

                    foreach (var item in inv.Dump())
                    {
                        Console.WriteLine(item.AlignedPrintDescription);
                    }
                }
            );

            OpenInventoryViewCommand = new RelayCommand
            (
                () => { WindowManager.OpenWindow(new InventoryView()); }
            );

            LoadExistingCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        DefaultExt = ".xml",
                        Filter = @"XML documents (*.xml)
                |*.xml|All files (*.*)|*.*",
                        CheckFileExists = true
                    };

                    if (dialog.ShowDialog() == true)
                        LoadExisting(dialog.FileName);
                }
            ); 

            CreateNewBomCommand = new RelayCommand
            (
                () =>
                {
                    WindowManager.OpenWindow(new BomView(AssemblyNumber), true);
                    AssemblyNumber = string.Empty;
                },
                () => !string.IsNullOrWhiteSpace(AssemblyNumber)
            );

            OpenSettingsEditorCommand = new RelayCommand
            (
                () => WindowManager.OpenWindow(new SettingsView())
            );

            ExitAllCommand = new RelayCommand
            (
                () =>
                {
                    if (!ConfirmClose || ConfirmationDialog.Show("Confirm exit", "Are you sure you want to close the application?"))
                    {
                        WindowManager.CloseAll();
                        CloseCommand.Execute(null);
                    }
                }
            );

            CloseAllBomWindows = new RelayCommand
            (
                () => WindowManager.CloseAll(),
                () => WindowManager.ChildOpen()
            );

            FileDroppedCommand = new RelayFunction
            (
                file => LoadExisting(file.ToString())
            );

            AssemblyNumber = string.Empty;
            ConfirmClose   = Properties.Settings.Default.ConfirmClose;

            CheckXml();
        }

        public void CheckXml()
        {
            var xmlSizes   = new Models.IO.Sizes();
            var xmlPitches = new Models.IO.Pitches();

            if (!File.Exists(xmlSizes.FilePath))
            {
                xmlSizes.Add(new Size(DefaultSize));
            }

            if (!File.Exists(xmlPitches.FilePath))
            {
                xmlPitches.Add(new Pitch(DefaultPitch));
            }
        }

        protected bool LoadExisting(string file)
        {
            FileInfo f = new FileInfo(file);

            if (!f.Exists || !f.Extension.ToLower().EndsWith("xml"))
                return false;

            using (FileStream stream = new FileStream(
                file,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            {
                XmlReader xml = XmlReader.Create(stream);
                xml.MoveToContent();

                if (xml.Name.ToLower().Equals("bom"))
                {
                    // seek to the assembly number
                    do xml.Read(); while (!xml.Name.ToLower().Equals("assemblynumber"));

                    string assemblyNumber = ((XElement)XNode.ReadFrom(xml)).Value;

                    // if the file we're importing is not in the default assemblies folder...
                    if (f.DirectoryName != null && !f.DirectoryName.ToLower().Equals(Path.GetFullPath(Properties.Settings.Default.DefaultAssembliesFolder).ToLower()))
                    {
                        string destName = Path.Combine
                        (
                            Properties.Settings.Default.DefaultAssembliesFolder,
                            string.Format(Properties.Settings.Default.BomFilenameFormat, assemblyNumber)
                        );

                        if (!File.Exists(destName))
                            f.CopyTo(destName, false); // Copy it to the right folder
                    }

                    // now we can open a new BOM window with this one's assembly number
                    AssemblyNumber = assemblyNumber;
                    CreateNewBomCommand.Execute(null);
                    return true;
                }
            }

            return false;
        }

        #region #settings.settings aliases

        public float DefaultSize                                 => Properties.Settings.Default.DefaultSize;
        public float DefaultPitch                                => Properties.Settings.Default.DefaultPitch;
        public string BomFilenameFormat                          => Properties.Settings.Default.BomFilenameFormat;
        public bool Debug                                        => Properties.Settings.Default.Debug;
        public System.Windows.Visibility DebugControlsVisibility => Debug ? System.Windows.Visibility.Visible : System.Windows.Visibility.Hidden;

        #endregion
    }
}