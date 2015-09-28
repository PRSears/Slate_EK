﻿using Extender.WPF;
using Slate_EK.Views;
using System;
using System.IO;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;

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
        public ICommand CloseAllBomWindows          { get; private set; }
        public ICommand ExitAllCommand              { get; private set; }
        public ICommand TestHarnessCommand          { get; private set; }
        public ICommand FileDroppedCommand          { get; private set; }

        public bool WindowsMenuEnabled
        {
            get
            {
                return this.WindowManager.ChildOpen();
            }
        }
        
        public WindowManager WindowManager;

        private string _AssemblyNumber;

        public MainViewModel()
        {
            this.WindowManager = new Extender.WPF.WindowManager();

            this.WindowManager.WindowOpened += (s, w) => OnPropertyChanged("WindowsMenuEnabled");
            this.WindowManager.WindowClosed += (s, w) => OnPropertyChanged("WindowsMenuEnabled");

            TestHarnessCommand = new RelayCommand
            (
                () => { throw new NotImplementedException(); }
            );

            LoadExistingCommand = new RelayCommand
            (
                () =>
                {
                    Microsoft.Win32.OpenFileDialog dialog = new Microsoft.Win32.OpenFileDialog();
                    dialog.DefaultExt = ".xml";
                    dialog.Filter = @"XML documents (*.txt, *.xml)
                |*.txt;*.xml|All files (*.*)|*.*";
                    dialog.CheckFileExists = true;

                    if (dialog.ShowDialog() == true)
                        LoadExisting(dialog.FileName);
                }
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

            CloseAllBomWindows = new RelayCommand
            (
                () => WindowManager.CloseAll()
            );

            FileDroppedCommand = new RelayFunction
            (
                (file) =>
                {
                    return LoadExisting(file.ToString());
                }
            );

            this.AssemblyNumber = string.Empty;

            this.CheckXML();
        }

        public void CheckXML()
        {
            Slate_EK.Models.IO.Sizes    xmlSizes    = new Models.IO.Sizes();
            Slate_EK.Models.IO.Pitches  xmlPitches  = new Models.IO.Pitches();

            if(!System.IO.File.Exists(xmlSizes.FilePath))
            {
                xmlSizes.Add(new Models.Size(DefaultSize));
            }

            if(!System.IO.File.Exists(xmlPitches.FilePath))
            {
                xmlPitches.Add(new Models.Pitch(DefaultPitch));
            }
        }

        protected bool LoadExisting(string file)
        {
            FileInfo f = new FileInfo(file);

            if (!f.Exists || !f.Extension.ToLower().EndsWith("xml"))
                return false;

            using(FileStream stream = new FileStream(
                file, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.ReadWrite))
            {
                XmlReader xml = XmlReader.Create(stream);
                xml.MoveToContent();

                if(xml.Name.ToLower().Equals("bom"))
                {
                    // seek to the assembly number
                    do    xml.Read();
                    while (!xml.Name.ToLower().Equals("assemblynumber"));

                    string assemblyNumber = (XNode.ReadFrom(xml) as XElement).Value;

                    string debug = Path.GetFullPath(Properties.Settings.Default.DefaultAssembliesFolder).ToLower();

                    // if the file we're importing is not in the default assemblies folder...
                    if(!f.DirectoryName.ToLower().Equals(Path.GetFullPath(Properties.Settings.Default.DefaultAssembliesFolder).ToLower()))
                    {
                        string destName = Path.Combine
                        (
                            Properties.Settings.Default.DefaultAssembliesFolder,
                            string.Format(Properties.Settings.Default.BomFilenameFormat, assemblyNumber)
                        );

                        if(!File.Exists(destName))
                            f.CopyTo(destName, false); // Copy it to the right folder
                    }

                    // now we can open a new BOM window with this one's assembly number
                    this.AssemblyNumber = assemblyNumber;
                    this.CreateNewBomCommand.Execute(null);
                    return true;
                }
            }

            return false;
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
        public string BomFilenameFormat
        {
            get
            {
                return Properties.Settings.Default.BomFilenameFormat;
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
