﻿using Extender;
using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;

namespace Slate_EK.ViewModels
{
    public class BomViewModel : ViewModel
    {
        protected Timer PropertyRefreshTimer;

        #region commands
        public ICommand AddToListCommand    { get; private set; }
        public ICommand RemoveItemCommand   { get; private set; }
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

        public string[] FastenerTypesList
        {
            get
            {
                return FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();
            }
        }

        public HoleType[] HoleTypesList
        {
            get
            {
                return HoleType.HoleTypes;
            }
        }

        public Models.IO.Sizes XmlSizes
        {
            get;
            protected set;
        }

        public Models.IO.Pitches XmlPitches
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
                OnPropertyChanged(nameof(Bom));
            }
        }

        public ObservableCollection<FastenerControl> ObservableFasteners
        {
            get
            {
                return _ObservableFasteners;
            }
            set
            {
                _ObservableFasteners = value;
                OnPropertyChanged(nameof(ObservableFasteners));
            }
        }

        #region boxed properties

        private Bom         _Bom;
        private Fastener    _WorkingFastener;
        private bool        _OverrideLength;

        private ObservableCollection<FastenerControl> _ObservableFasteners;

        #endregion
        
        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            this.Bom                    = new Models.Bom(assemblyNumber); // TODO fix saving when assembly # changes // Should it 'move' or just copy?
            this.WorkingFastener        = new Fastener(assemblyNumber);
            this.ObservableFasteners    = Bom.SourceList != null ? new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList)) :
                                                                   new ObservableCollection<FastenerControl>();

            Bom.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName.Equals("SourceList") && Bom.SourceList != null)
                {
                    this.ObservableFasteners = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList));
                }

                OnPropertyChanged("WindowTitle"); // do this regardless of which property changed
            };

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

            ShortcutPressed_CtrlS += () =>
            {
                SaveAs();
            };

            // ICommands

            AddToListCommand = new RelayCommand
            (
                () =>
                {
                    WorkingFastener.GetNewID();
                    Bom.Add(WorkingFastener.Copy<Fastener>());
                }
            );

            RemoveItemCommand = new RelayCommand
            (
                () =>
                {
                    FastenerControl[] selected = ObservableFasteners.Where(c => c.IsSelected).ToArray();
                    bool removeAll = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

                    foreach(FastenerControl fc in selected)
                    {
                        Bom.Remove(fc.Fastener, removeAll ? (Int32.MaxValue - 1) : 1);
                    }

                    foreach(FastenerControl item in ObservableFasteners.Where(of => selected.Count(s => s.Fastener.ID.Equals(of.Fastener.ID)) > 0))
                    {
                        item.IsSelected = true;
                    }
                }
            );

            SaveAsCommand = new RelayCommand
            (
                () => SaveAs()
            );

            // Lists from XML
            XmlSizes    = new Models.IO.Sizes();
            XmlPitches  = new Models.IO.Pitches();

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

    public class FastenerControl : INotifyPropertyChanged
    {
        public Fastener Fastener
        {
            get;
            set;
        }
        public bool IsSelected
        {
            get
            {
                return _IsSelected;
            }
            set
            {
                _IsSelected = value;
                OnPropertyChanged("IsSelected");
                OnPropertyChanged("Background");
                OnPropertyChanged("AltBackground");
            }
        }

        public SolidColorBrush Background
        {
            get
            {
                if (IsSelected)
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(SelectedColor));
                else
                    return Brushes.Transparent;
            }
        }

        public SolidColorBrush AltBackground
        {
            get
            {
                if (IsSelected)
                    return Background;
                else
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(AltColor));
            }
        }

        #region boxed properties
        private bool _IsSelected;
        #endregion

        public ICommand SelectCommand       { get; private set; }
        public ICommand DeselectCommand     { get; private set; }
        public ICommand ToggleSelectCommand { get; private set; }
        public ICommand EditCommand         { get; private set; }
        
        public event EditControlEventHandler EditingControl;

        public FastenerControl()
        {
            SelectCommand = new RelayCommand
            (
                () => IsSelected = true
            );

            DeselectCommand = new RelayCommand
            (
                () => IsSelected = false
            );

            ToggleSelectCommand = new RelayCommand
            (
                () => IsSelected = !IsSelected
            );

            EditCommand = new RelayCommand
            (
                () => OnEdit(this)
            );
        }

        public FastenerControl(Fastener fastener)
            : this()
        {
            this.Fastener = fastener;
        }

        /// <summary>
        /// Creates an array of FastenerControls from an array of plain Fastener objects.
        /// </summary>
        public static FastenerControl[] FromArray(Fastener[] fasteners)
        {
            FastenerControl[] controls = new FastenerControl[fasteners.Length];
            for (int i = 0; i < fasteners.Length; i++)
                controls[i] = new FastenerControl(fasteners[i]);

            return controls;
        }

        protected void OnEdit(object sender)
        {
            EditControlEventHandler handler = this.EditingControl;

            if (handler != null)
                handler(sender);
        }

        #region Settings.Settings aliases
        protected string SelectedColor
        {
            get
            {
                return Properties.Settings.Default.ItemSelectedBackgroundColor;
            }
        }
        protected string HoverColor
        {
            get
            {
                return Properties.Settings.Default.ItemHoverBackgroundColor;
            }
        }
        protected string NormalColor
        {
            get
            {
                return Properties.Settings.Default.ItemDefaultBackgroundColor;
            }
        }
        protected string AltColor
        {
            get
            {
                return Properties.Settings.Default.ItemAltnernateBackgroundColor;
            }
        }
        public int BomFontSize
        {
            get
            {
                return Properties.Settings.Default.BomListFontSize;
            }
        }
        #endregion
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
    
    public delegate void EditControlEventHandler(object sender);
}
