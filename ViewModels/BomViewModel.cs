using Extender;
using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows.Input;
using System.Windows.Media;

namespace Slate_EK.ViewModels
{
    // TODO allow input to be in either inches or mm and handle all appropriate conversions in the background
    public class BomViewModel : ViewModel
    {
        protected Timer PropertyRefreshTimer;

        #region commands
        public ICommand AddToListCommand            { get; private set; }
        public ICommand RemoveItemCommand           { get; private set; }
        public ICommand ListChangeQuantityCommand   { get; private set; }
        public ICommand ListDeleteItemCommand       { get; private set; }
        public ICommand SaveAsCommand               { get; private set; }
        public ICommand Shortcut_CtrlK              { get; private set; }
        public ICommand Shortcut_CtrlS              { get; private set; }
        public ICommand Shortcut_CtrlE              { get; private set; }
        public ICommand Shortcut_CtrlD              { get; private set; }
        public ICommand Shortcut_CtrlA              { get; private set; }

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
                    Bom.SourceList != null ? Bom.SourceList.Length.ToString() : "0"
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
                OnPropertyChanged(nameof(OverrideLength));
                //OnPropertyChanged("LengthOverrideVisibility");
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
                OnPropertyChanged(nameof(WorkingFastener));
            }
        }

        // Drop-down list data sources
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
                if (Bom.SourceList != null && e.PropertyName.Equals(nameof(Bom.SourceList)))
                {
                    this.ObservableFasteners = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList));

                    // Hook up context menu for FastenerControls
                    foreach (FastenerControl control in ObservableFasteners)
                    {
                        control.RequestingRemoval += (sender) => Bom.Remove((sender as FastenerControl).Fastener, Int32.MaxValue);
                        control.RequestingQuantityChange += (sender) => ChangeQuantity(sender);
                    }
                }

                OnPropertyChanged(nameof(WindowTitle)); // do this regardless of which property changed
            };

            WorkingFastener.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Fastener.Material)) ||
                    e.PropertyName.Equals(nameof(Fastener.Size)) ||
                    e.PropertyName.Equals(nameof(Fastener.PlateThickness)) ||
                    e.PropertyName.Equals(nameof(Fastener.HoleType)))
                {
                    if (!OverrideLength)
                        WorkingFastener.CalculateDesiredLength();
                }
            };

            Initialize();
        }

        private void RemoveN(object sender)
        {
            if (sender is FastenerControl)
            {
                Views.NumberPickerDialog dialog = new Views.NumberPickerDialog();
                dialog.ShowDialog();

                if (dialog.Value > 0)
                {
                    Bom.Remove((sender as FastenerControl).Fastener, dialog.Value);
                }
            }
        }

        private void ChangeQuantity(object sender)
        {
            if (sender is FastenerControl)
            {
                Views.NumberPickerDialog dialog = new Views.NumberPickerDialog((sender as FastenerControl).Fastener.Quantity);
                dialog.ShowDialog();

                if(dialog.Success)
                {
                    if (dialog.Value > 0)
                        (sender as FastenerControl).Fastener.Quantity = dialog.Value;
                    else
                        Bom.Remove((sender as FastenerControl).Fastener, Int32.MaxValue);
                }
            }
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

            Shortcut_CtrlD = new RelayCommand
            (
                () =>
                {
                    foreach (FastenerControl item in ObservableFasteners.Where(c => c.IsSelected))
                        item.IsSelected = false;
                }
            );

            Shortcut_CtrlA = new RelayCommand
            (
                () =>
                {
                    foreach (FastenerControl item in ObservableFasteners)
                        item.IsSelected = true;
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
                        Bom.Remove(fc.Fastener, removeAll ? (Int32.MaxValue) : 1);
                    }

                    // re-select what was selected before we replaced the ObservableCollection
                    foreach(FastenerControl item in ObservableFasteners.Where(of => selected.Count(s => s.Fastener.ID.Equals(of.Fastener.ID)) > 0))
                    {
                        item.IsSelected = true;
                    }
                }
            );

            ListChangeQuantityCommand = new RelayCommand
            (
                () =>
                {
                    ChangeQuantity(ObservableFasteners.First(f => f.IsSelected));
                },
                () =>
                {
                    // Can't allow edit when multiple fasteners are selected. 
                    // Too messy -- what would we use for initial value? Set all selected to the same value,
                    // or would the user expect to increment?
                    return (ObservableFasteners.Count((f) => f.IsSelected) <= 1) ? true : false;
                }
            );

            ListDeleteItemCommand = new RelayCommand
            (
                () =>
                {
                    FastenerControl[] selected = ObservableFasteners.Where(c => c.IsSelected).ToArray();

                    foreach (FastenerControl fc in selected)
                        Bom.Remove(fc.Fastener, Int32.MaxValue);
                }
            );
            
            SaveAsCommand = new RelayCommand
            (
                () => SaveAs()
            );

            // Hook up context menu commands for FastenerControls
            foreach (FastenerControl control in ObservableFasteners)
            {
                control.RequestingRemoval           += (sender) => Bom.Remove((sender as FastenerControl).Fastener, Int32.MaxValue);
                control.RequestingQuantityChange    += (sender) => ChangeQuantity(sender);
            }

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
            dialog.Filter = @"(*.xml)
|*.xml|(*.csv)|*.csv|All files (*.*)|*.*";
            dialog.AddExtension = true;
            dialog.FileName = System.IO.Path.GetFileName(Bom.FilePath);

            bool? result = dialog.ShowDialog();

            if (result == true)
                savePath = dialog.FileName;
            else return false;

            if (Path.GetExtension(dialog.FileName)
                    .ToLower()
                    .EndsWith("csv"))
            {
                Extender.IO.CsvSerializer<Fastener> csv = new Extender.IO.CsvSerializer<Fastener>();

                using (FileStream stream = new FileStream(dialog.FileName, FileMode.OpenOrCreate,
                                                                           FileAccess.ReadWrite,
                                                                           FileShare.Read))
                {
                    csv.Serialize(stream, Bom.SourceList);
                }
            }
            else
            {
                try
                {
                    File.Copy(Bom.FilePath, savePath);
                }
                catch (Exception e)
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

        public ICommand SelectCommand           { get; private set; }
        public ICommand DeselectCommand         { get; private set; }
        public ICommand ToggleSelectCommand     { get; private set; }
        public ICommand EditCommand             { get; private set; }
        public ICommand RequestRemoval          { get; private set; }
        public ICommand RequestQuantityChange   { get; private set; }

        public event EditControlEventHandler            EditingControl;
        public event RequestRemovalEventHandler         RequestingRemoval;
        public event RequestQuantityChangeEventHandler  RequestingQuantityChange;

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

            RequestRemoval = new RelayCommand
            (
                () =>
                {
                    RequestingRemoval?.Invoke(this);
                }
            );

            RequestQuantityChange = new RelayCommand
            (
                () =>
                {
                    RequestingQuantityChange?.Invoke(this);
                }
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
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
    
    public delegate void EditControlEventHandler(object sender);
    public delegate void RequestRemovalEventHandler(object sender);
    public delegate void RequestQuantityChangeEventHandler(object sender);
}
