using Extender;
using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    //TODO  allow input to be in either inches or mm and handle all appropriate conversions in the background
    public class BomViewModel : ViewModel
    {
        protected Timer PropertyRefreshTimer;

        #region commands
        public ICommand AddToListCommand            { get; private set; }
        public ICommand RemoveItemCommand           { get; private set; }
        public ICommand ListChangeQuantityCommand   { get; private set; }
        public ICommand ListDeleteItemCommand       { get; private set; }
        public ICommand SaveAsCommand               { get; private set; }
        public ICommand ShortcutCtrlK               { get; private set; }
        public ICommand ShortcutCtrlS               { get; private set; }
        public ICommand ShortcutCtrlE               { get; private set; }
        public ICommand ShortcutCtrlD               { get; private set; }
        public ICommand ShortcutCtrlA               { get; private set; }
        public ICommand ShortcutCtrlP               { get; private set; }

        public event ShortcutEventHandler ShortcutPressedCtrlK;
        public event ShortcutEventHandler ShortcutPressedCtrlS;
        #endregion

        public string WindowTitle => string.Format
            (
                "Assembly #{1} [{2}] - {0}",
                Properties.Settings.Default.ShortTitle,
                Bom.AssemblyNumber,
                Bom.SourceList != null ? Bom.SourceList.Length.ToString() : "0"
            );

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
        public Material[] MaterialsList => Material.Materials;

        public string[] FastenerTypesList
        {
            get
            {
                return FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();
            }
        }

        public HoleType[] HoleTypesList => HoleType.HoleTypes;

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
            Bom                    = new Bom(assemblyNumber); // TODO fix saving when assembly # changes // Should it 'move' or just copy?
            WorkingFastener        = new Fastener(assemblyNumber);
            ObservableFasteners    = Bom.SourceList != null ? new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList)) :
                                                                   new ObservableCollection<FastenerControl>();

            Bom.PropertyChanged += (s, e) =>
            {
                if (Bom.SourceList != null && e.PropertyName.Equals(nameof(Bom.SourceList)))
                {
                    ObservableFasteners = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList));

                    // Hook up context menu for FastenerControls
                    foreach (FastenerControl control in ObservableFasteners)
                    {
                        control.RequestingRemoval += sender => Bom.Remove((sender as FastenerControl).Fastener, Int32.MaxValue);
                        control.RequestingQuantityChange += sender => ChangeQuantity(sender);
                    }
                }

                OnPropertyChanged(nameof(WindowTitle)); // do this regardless of which property changed
            };

            WorkingFastener.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Fastener.Material))       ||
                    e.PropertyName.Equals(nameof(Fastener.Size))           ||
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
                dialog.Owner = FindThisWindow();
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
                dialog.Owner = FindThisWindow();
                dialog.ShowDialog();                

                if (dialog.Success)
                {
                    if (dialog.Value > 0)
                        (sender as FastenerControl).Fastener.Quantity = dialog.Value;
                    else
                        Bom.Remove(((FastenerControl)sender).Fastener, Int32.MaxValue);
                }
            }
        }

        private Window FindThisWindow()
        {
            // Yeah... this is bad.
            Window mainWindow = Application.Current.MainWindow;
            return (mainWindow as Views.MainView)?.FindBomWindow(Bom.AssemblyNumber);
        }

        public override void Initialize()
        {
            base.Initialize();

            // Init shortcuts
            ShortcutCtrlK = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressedCtrlK)
            );

            ShortcutCtrlS = new RelayCommand
            (
                () => NullsafeHandleShortcut(ShortcutPressedCtrlS)
            );

            ShortcutCtrlE = new RelayCommand
            (
                () =>
                {
                    System.Diagnostics.Process editorProcess = new System.Diagnostics.Process();
                    editorProcess.StartInfo.FileName         = Properties.Settings.Default.DefaultPropertiesFolder;
                    editorProcess.StartInfo.UseShellExecute  = true;
                    editorProcess.Start();
                }
            );

            ShortcutCtrlD = new RelayCommand
            (
                () =>
                {
                    foreach (FastenerControl item in ObservableFasteners.Where(c => c.IsSelected))
                        item.IsSelected = false;
                }
            );

            ShortcutCtrlA = new RelayCommand
            (
                () =>
                {
                    foreach (FastenerControl item in ObservableFasteners)
                        item.IsSelected = true;
                }
            );

            ShortcutCtrlP = new RelayCommand
            (
                () =>
                {
                    MessageBox.Show("Print not yet implemented.");
                }
            );

            ShortcutPressedCtrlS += () =>
            {
                SaveAs();
            };

            // ICommands

            AddToListCommand = new RelayCommand
            (
                () =>
                {
                    WorkingFastener.GetNewId();
                    Bom.Add(WorkingFastener.Copy<Fastener>());
                }
            );

            RemoveItemCommand = new RelayCommand
            (
                () =>
                {
                    FastenerControl[] selected = ObservableFasteners.Where(c => c.IsSelected).ToArray();
                    bool removeAll = (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift));

                    foreach (FastenerControl fc in selected)
                    {
                        Bom.Remove(fc.Fastener, removeAll ? (Int32.MaxValue) : 1);
                    }

                    // re-select what was selected before we replaced the ObservableCollection
                    foreach (FastenerControl item in ObservableFasteners.Where(of => selected.Count(s => s.Fastener.Id.Equals(of.Fastener.Id)) > 0))
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
                    return (ObservableFasteners.Count(f => f.IsSelected) <= 1) ? true : false;
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
                control.RequestingRemoval           += sender => Bom.Remove((sender as FastenerControl).Fastener, Int32.MaxValue);
                control.RequestingQuantityChange    += sender => ChangeQuantity(sender);
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

                GC.Collect();
            };

            PropertyRefreshTimer.Start();
        }

        protected bool SaveAs()
        {
            string savePath;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title        = "Save a copy of the BOM as...",
                DefaultExt   = ".xml",
                Filter       = @"(*.xml)
|*.xml|(*.csv)|*.csv|All files (*.*)|*.*",
                AddExtension = true,
                FileName     = Path.GetFileName(Bom.FilePath)
            };

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

                    MessageBox.Show
                    (
                        "Encountered an exception while copying BOM:\n" + e.Message,
                        "Exception",
                        MessageBoxButton.OK
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

}
