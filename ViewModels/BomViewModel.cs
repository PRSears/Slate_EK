using Extender;
using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    //TODO  allow input to be in either inches or mm and handle all appropriate conversions in the background
    //TODO  hook up Bom export to Inventory
    //TODO  change saveas csv to be of print format (or maybe as a third option?)
    //      Qty, callout, mass per * qty, price per, total price

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
        public ICommand ShortcutCtrlC               { get; private set; }
        public ICommand ShortcutCtrlV               { get; private set; }

        public event ShortcutEventHandler ShortcutPressedCtrlK;
        public event ShortcutEventHandler ShortcutPressedCtrlS;
        #endregion

        public string WindowTitle => string.Format
        (
            "Assembly #{1} [{2}] - {0}",
            Properties.Settings.Default.ShortTitle,
            Bom.AssemblyNumber,
            Bom.SourceList?.Length.ToString() ?? "0"
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
            }
        }

        public UnifiedFastener WorkingFastener
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
        public Material[] MaterialsList     => Material.Materials;
        public HoleType[] HoleTypesList     => HoleType.HoleTypes;
        public string[]   SizeOptionsList   => XmlSizes.SourceList?.Select(s => s.ToString()).ToArray();
        public string[]   PitchOptionsList  => XmlPitches.SourceList?.Select(p => p.ToString()).ToArray();
        public string[]   FastenerTypesList => FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();

        private Models.IO.Sizes XmlSizes
        {
            get; set;
        }

        private Models.IO.Pitches XmlPitches
        {
            get; set;
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

        private Bom                _Bom;
        private UnifiedFastener    _WorkingFastener;
        private bool               _OverrideLength;

        private ObservableCollection<FastenerControl> _ObservableFasteners;

        #endregion
        
        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            Bom                    = new Bom(assemblyNumber); // TODO fix saving when assembly # changes // Should it 'move' or just copy?
            WorkingFastener        = new UnifiedFastener();
            ObservableFasteners    = Bom.SourceList != null ? new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList)) :
                                                              new ObservableCollection<FastenerControl>();

            Bom.PropertyChanged += (s, e) =>
            {
                if (Bom.SourceList != null && e.PropertyName.Equals(nameof(Bom.SourceList)))
                {
                    RefreshBom();
                    //ObservableFasteners = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList));

                    //// Hook up context menu for FastenerControls
                    //foreach (FastenerControl control in ObservableFasteners)
                    //{
                    //    control.RequestingRemoval += sender => Bom.Remove((sender as FastenerControl)?.Fastener, Int32.MaxValue);
                    //    control.RequestingQuantityChange += ChangeQuantity;
                    //}
                }

                OnPropertyChanged(nameof(WindowTitle)); // do this regardless of which property changed
            };

            WorkingFastener.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(UnifiedFastener.Material)) ||
                    e.PropertyName.Equals(nameof(UnifiedFastener.Size)))     
                {
                    if (!OverrideLength)
                        WorkingFastener.CalculateLength(true);
                }
            };

            WorkingFastener.PlateInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(PlateInfo.Thickness)) ||
                    e.PropertyName.Equals(nameof(PlateInfo.HoleType)))
                {
                    if (!OverrideLength)
                        WorkingFastener.CalculateLength(true);
                }
            };

            Initialize();
        }

        private void RefreshBom()
        {
            ObservableFasteners.Clear();
            
            Bom.SourceList.ForEach(f => ObservableFasteners.Add(new FastenerControl(f)));
            ObservableFasteners.ForEach
            (
                c =>
                {
                    c.RequestingRemoval         += sender => Bom.Remove((sender as FastenerControl)?.Fastener, Int32.MaxValue);
                    c.RequestingQuantityChange  += ChangeQuantity;
                }
            );
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
                    Bom.Remove(((FastenerControl)sender).Fastener, dialog.Value);
                }
            }
        }

        private void ChangeQuantity(object sender)
        {
            if (!(sender is FastenerControl)) return;

            var dialog = new Views.NumberPickerDialog(((FastenerControl)sender).Fastener.Quantity);

            dialog.Owner = FindThisWindow();
            dialog.ShowDialog();

            if (!dialog.Success) return;

            if (dialog.Value > 0)
            {
                // Edit the value in the source list directly... need to select the fastener control from Bom.SourceList first.
                var match = Bom.SourceList.FirstOrDefault(f => ((FastenerControl)sender).Fastener.UniqueID.Equals(f.UniqueID));
                if (match != null && !match.Equals(default(UnifiedFastener)))
                    match.Quantity = dialog.Value;

                Bom.Save();
            }
            else Bom.Remove(((FastenerControl)sender).Fastener, Int32.MaxValue);

            RefreshBom(); // Make sure the ObservableCollection is updated to reflect the change in Bom.SourceList.
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
                () => ObservableFasteners.ForEach(item => item.IsSelected = false)
            );

            ShortcutCtrlA = new RelayCommand
            (
                () => ObservableFasteners.ForEach(item => item.IsSelected = true)
            );

            ShortcutCtrlP = new RelayCommand
            (
                () =>
                {
                    MessageBox.Show("Print not yet implemented.");
                }
            );

            ShortcutCtrlC = new RelayCommand
            (
                () =>
                {
                    if (!ObservableFasteners.Any(f => f.IsSelected))
                    {
                        // None selected
                        Clipboard.SetText(WorkingFastener.Description);
                    }
                    else
                    {
                        // Has something selected in the BOM list
                        StringBuilder descriptionsBuilder = new StringBuilder();
                        ObservableFasteners.Where  (f => f.IsSelected)
                                           .ForEach(f => descriptionsBuilder.AppendLine(f.Fastener.Description));

                        Clipboard.SetText(descriptionsBuilder.ToString());
                    }
                },
                () => ObservableFasteners.Any(f => f.IsSelected)
            );

            ShortcutCtrlV = new RelayCommand
            (
                () =>
                {
                    StringReader clips  = new StringReader(Clipboard.GetText());
                    var newFasteners    = new List<UnifiedFastener>();
                    while (clips.Peek() > -1)
                    {
                        string line           = clips.ReadLine();
                        var    parsedFastener = UnifiedFastener.FromString(line);

                        if (parsedFastener != null)
                            newFasteners.Add(parsedFastener);
                    }

                    Bom.Add(newFasteners.ToArray());
                },
                () => !string.IsNullOrWhiteSpace(Clipboard.GetText())
            );

            ShortcutPressedCtrlS += () => SaveAs();

            //
            // ICommands

            AddToListCommand = new RelayCommand
            (
                () =>
                {
                    WorkingFastener.ForceNewUniqueID();
                    Bom.Add(WorkingFastener.Copy());
                }
            );

            RemoveItemCommand = new RelayCommand
            (
                () =>
                {
                    var  selected   = ObservableFasteners.Where(c => c.IsSelected).ToArray();
                    bool removeAll  = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

                    foreach (FastenerControl fc in selected)
                    {
                        Bom.Remove(fc.Fastener, removeAll ? (Int32.MaxValue) : 1);
                    }

                    // re-select what was selected before we replaced the ObservableCollection
                    foreach (var prevSelected in selected)
                    {
                        var match = ObservableFasteners.FirstOrDefault(f => f.Fastener.UniqueID.Equals(prevSelected.Fastener.UniqueID));
                        if (match != null) match.IsSelected = true;
                    }
                },
                () => ObservableFasteners.Any(f => f.IsSelected)
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
                    // Cast to array so we aren't modifying the collection as we iterate.
                    ObservableFasteners.Where(control => control.IsSelected)
                                       .ToArray()
                                       .ForEach(f => Bom.Remove(f.Fastener, Int32.MaxValue));
                },
                () => ObservableFasteners.Any(f => f.IsSelected)
            );
            
            SaveAsCommand = new RelayCommand
            (
                () => SaveAs()
            );

            // Hook up context menu commands for FastenerControls
            foreach (FastenerControl control in ObservableFasteners)
            {
                control.RequestingRemoval           += sender => Bom.Remove((sender as FastenerControl)?.Fastener, Int32.MaxValue);
                control.RequestingQuantityChange    += ChangeQuantity;
            }

            // Lists from XML
            XmlSizes    = new Models.IO.Sizes();
            XmlPitches  = new Models.IO.Pitches();

            XmlSizes.Reload();
            XmlPitches.Reload();

            PropertyRefreshTimer           = new Timer(Properties.Settings.Default.PropertyRefreshInterval);
            PropertyRefreshTimer.AutoReset = true;
            PropertyRefreshTimer.Elapsed  += (s, e) =>
            {
                XmlSizes.Reload();
                XmlPitches.Reload();

                OnPropertyChanged(nameof(SizeOptionsList));
                OnPropertyChanged(nameof(PitchOptionsList));

                GC.Collect();
            };

            PropertyRefreshTimer.Start();

            OnPropertyChanged(nameof(SizeOptionsList));
            OnPropertyChanged(nameof(PitchOptionsList));
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
                Extender.IO.CsvSerializer<UnifiedFastener> csv = new Extender.IO.CsvSerializer<UnifiedFastener>();


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
