using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.IO;
using System;
using System.Timers;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public class BomViewModel : ViewModel
    {
        protected Timer PropertyRefreshTimer;

        #region commands
        public ICommand AddToListCommand    { get; private set; }
        public ICommand SaveAsCommand       { get; private set; }
        public ICommand Shortcut_CtrlK      { get; private set; }
        public ICommand Shortcut_CtrlS      { get; private set; }
        public ICommand Shortcut_CtrlE      { get; private set; }

        public event ShortcutEventHandler ShortcutPressed_CtrlK;
        public event ShortcutEventHandler ShortcutPressed_CtrlS;

        #endregion

        public string WindowTitle
        {
            get
            {
                return string.Format
                (
                    "Assembly #{1} [{2}] - {0}",
                    Properties.Settings.Default.ShortTitle,
                    Bom.AssemblyNumber,
                    Bom.SourceList != null ?
                    Bom.SourceList.Length.ToString() : "0"
                );
            }
        }

        public bool OverrideLength
        {
            get
            {
                return _OverrideLength;
            }
            set
            {
                _OverrideLength = value;
                OnPropertyChanged("OverrideLength");
                OnPropertyChanged("LengthOverrideVisibility");
            }
        }

        public Fastener WorkingFastener
        {
            get
            {
                return _WorkingFastener;
            }
            set
            {
                _WorkingFastener = value;
                OnPropertyChanged("WorkingFastener");
            }
        }

        // Dropdown list data sources
        public Material[] MaterialsList
        {
            get
            {
                return Material.Materials;
            }
        }

        public Sizes XmlSizes
        {
            get;
            protected set;
        }

        public Pitches XmlPitches
        {
            get;
            protected set;
        }

        public Bom Bom
        {
            get
            {
                return _Bom;
            }
            set
            {
                _Bom = value;
                OnPropertyChanged("Bom");
            }
        }

        #region boxed properties

        private Bom         _Bom;
        private Fastener    _WorkingFastener;
        private bool        _OverrideLength;
        private string      _Thickness;

        #endregion
        
        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            this.WorkingFastener = new Fastener(assemblyNumber);
            this.Bom             = new Models.Bom(assemblyNumber);

            Bom.PropertyChanged += (s, e) => OnPropertyChanged("WindowTitle");

            Initialize();
        }

        public override void Initialize()
        {
            base.Initialize();

            // Init shortcuts
            Shortcut_CtrlK = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressed_CtrlK)
            );

            Shortcut_CtrlS = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressed_CtrlS)
            );

            Shortcut_CtrlE = new RelayCommand
            (
                () =>
                {
                    System.Diagnostics.Process editorProcess = new System.Diagnostics.Process();
                    editorProcess.StartInfo.FileName         = Properties.Settings.Default.DefaultPropertiesFolder;
                    editorProcess.StartInfo.UseShellExecute  = true;
                    editorProcess.Start();
                }
            );

            AddToListCommand = new RelayCommand
            (
                () =>
                {
                    this.WorkingFastener.RefreshID();
                    Bom.Add(this.WorkingFastener);
                }
            );

            SaveAsCommand = new RelayCommand
            (
                () => SaveAs()
            );

            ShortcutPressed_CtrlS += () =>
            {
                SaveAs();
            };

            // Lists from XML
            XmlSizes    = new Sizes();
            XmlPitches  = new Pitches();

            XmlSizes.Reload();
            XmlPitches.Reload();

            PropertyRefreshTimer = new Timer(Properties.Settings.Default.PropertyRefreshInterval);
            PropertyRefreshTimer.AutoReset = true;
            PropertyRefreshTimer.Elapsed += (s, e) =>
            {
                XmlSizes.Reload();
                XmlPitches.Reload();

                System.GC.Collect();
            };

            PropertyRefreshTimer.Start();
        }

        protected bool SaveAs()
        {
            string savePath;

            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Title = "Save a copy of the BOM as...";
            dialog.DefaultExt = ".xml";
            dialog.FileName = System.IO.Path.GetFileName(Bom.FilePath);

            Nullable<bool> result = dialog.ShowDialog();

            if (result == true)
                savePath = dialog.FileName;
            else
                return false;

            try
            {
                System.IO.File.Copy(Bom.FilePath, savePath);
            }
            catch(Exception e)
            {
                Extender.Debugging.ExceptionTools.WriteExceptionText(e, false);

                System.Windows.MessageBox.Show
                (
                    "Encountered an exception while copying BOM:\n" + e.Message,
                    "Exception",
                    System.Windows.MessageBoxButton.OK
                );
                return false;
            }

            return true;
        }

        private bool NullsafeHandleShortcut(ShortcutEventHandler handler)
        {
            if (handler != null)
            {
                handler();
                return true;
            }
            else return false;
        }
    }

    public delegate void ShortcutEventHandler();
}
