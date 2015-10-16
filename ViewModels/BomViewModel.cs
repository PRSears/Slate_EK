using Extender;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.ThreadParameters;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public sealed class BomViewModel : ViewModel
    {
        private Timer _PropertyRefreshTimer;

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
        public Units[]    UnitsList            => (Units[])Enum.GetValues(typeof(Units)); // TODO Check if this is used 
        public Material[] MaterialsList        => Material.Materials;
        public HoleType[] HoleTypesList        => HoleType.HoleTypes;
        public string[]   FastenerTypesList    => FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();
        public string[]   SizeOptionsList
        {
            get { return _SizeOptionsList; }
            private set
            {
                _SizeOptionsList = value;
                OnPropertyChanged(nameof(SizeOptionsList));
            }
        }
        public string[]   PitchOptionsList
        {
            get { return _PitchOptionsList; }
            private set
            {
                _PitchOptionsList = value;
                OnPropertyChanged(nameof(PitchOptionsList));
            }
        } 

        // TODO A NullReferenceException sometimes gets thrown when typing in a value to select items from the drop down. 
        //      (Gets thrown when the dropdown loses focus)
        //      I haven't been able to reproduce this since implementing the ImperialSizes list.

        private Models.IO.Sizes          XmlSizes         { get; set; }
        private Models.IO.Pitches        XmlPitches       { get; set; }
        private Models.IO.ImperialSizes  XmlImperialSizes { get; set; }

        public Bom Bom
        {
            get
            {
                return _Bom;
            }
            private set
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
            private set
            {
                _ObservableFasteners = value;
                OnPropertyChanged(nameof(ObservableFasteners));
            }
        }

        #region boxed properties

        private Bom                _Bom;
        private UnifiedFastener    _WorkingFastener;
        private bool               _OverrideLength;
        private string[]           _SizeOptionsList;
        private string[]           _PitchOptionsList;

        private ObservableCollection<FastenerControl> _ObservableFasteners;

        #endregion

        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            // TODO fix saving when assembly # changes // Should it 'move' or just copy?
            //      I'm thinking copy, that way you can quickly duplicate a bom for similar orders.
            Bom                    = new Bom(assemblyNumber); 
            WorkingFastener        = new UnifiedFastener();
            ObservableFasteners    = Bom.SourceList != null ? new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList)) :
                                                              new ObservableCollection<FastenerControl>();

            Bom.PropertyChanged += (s, e) =>
            {
                if (Bom.SourceList != null && e.PropertyName.Equals(nameof(Bom.SourceList)))
                    RefreshBom();

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
                else if (e.PropertyName.Equals(nameof(UnifiedFastener.Unit)))
                {
                    SetSizesList();
                }
                else if (WorkingFastener.Unit == Units.Inches && e.PropertyName.Equals(nameof(WorkingFastener.SizeDisplay)))
                {

                    float? pitchReset = (float?)Measure.Convert<Inch, Millimeter>(
                        UnifiedThreadStandard.FromMillimeters(WorkingFastener.Size)?.CourseThreadPitch);

                    WorkingFastener.Pitch = pitchReset ?? 0f;

                    SetUstPitch();
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
                    editorProcess.StartInfo.FileName = Properties.Settings.Default.DefaultPropertiesFolder;
                    editorProcess.StartInfo.UseShellExecute = true;
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
                        ObservableFasteners.Where(f => f.IsSelected)
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
                    return ObservableFasteners.Any(f => f.IsSelected);
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
                control.RequestingRemoval += sender => Bom.Remove((sender as FastenerControl)?.Fastener, Int32.MaxValue);
                control.RequestingQuantityChange += ChangeQuantity;
            }

            // Lists from XML
            XmlSizes = new Models.IO.Sizes();
            XmlPitches = new Models.IO.Pitches();
            XmlImperialSizes = new Models.IO.ImperialSizes();

            Task.Run(async () =>
            {
                await XmlSizes.ReloadAsync();
                if (WorkingFastener.Unit == Units.Millimeters)
                    SetSizesList();
            });

            Task.Run(async () =>
            {
                await XmlImperialSizes.ReloadAsync();
                if (WorkingFastener.Unit == Units.Inches)
                    SetSizesList();
            });

            Task.Run(async () =>
            {
                await XmlPitches.ReloadAsync();
                SetSizesList();
            });

            _PropertyRefreshTimer = new Timer(Properties.Settings.Default.PropertyRefreshInterval);
            _PropertyRefreshTimer.AutoReset = true;
            _PropertyRefreshTimer.Elapsed += (s, e) =>
            {
                #pragma warning disable CS4014 // We don't care when the task is completed.
                XmlSizes.ReloadAsync();
                XmlPitches.ReloadAsync();
                XmlImperialSizes.ReloadAsync();
                #pragma warning restore CS4014 // We don't care when the task is completed.

                if (SizeOptionsList == null || PitchOptionsList == null)
                    SetSizesList();
                else
                {
                    OnPropertyChanged(nameof(SizeOptionsList));
                    OnPropertyChanged(nameof(PitchOptionsList));
                }

                GC.Collect();
            };

            _PropertyRefreshTimer.Start();
        }
        
        private void AddToBom(UnifiedFastener fastener)
        {
            // Check if a suitable fastener exists in inventory
            //  -> add that to BOM if it does
            //  -> ?? if it doesn't.
        }

        private void SetSizesList()
        {
            switch (WorkingFastener.Unit)
            {
                case Units.Millimeters:
                    SizeOptionsList  = XmlSizes.SourceList?.Select(  s => s.ToString()).ToArray();
                    PitchOptionsList = XmlPitches.SourceList?.Select(p => p.ToString()).ToArray();
                    break;
                case Units.Inches:
                    SizeOptionsList  = XmlImperialSizes.SourceList?.Select(i => i.Designation).ToArray(); 
                    // This value for PitchOptionsList is only used as a fall-back.
                    // SetUstPitch() will try to generate a more specific list.
                    PitchOptionsList = Enum.GetNames(typeof(ThreadDensity));
                    break;
            }
        }

        private void SetUstPitch()
        {
            var selectedUts = XmlImperialSizes.SourceList?.FirstOrDefault(s => s.Designation.Equals(WorkingFastener.SizeDisplay));    

            if (selectedUts == null || selectedUts.Equals(default(UnifiedThreadStandard)))
                return; // give up

            try
            {
                PitchOptionsList = ((ThreadDensity[])Enum.GetValues(typeof(ThreadDensity)))
                                                         .Select(thread => selectedUts.GetThreadDensityDisplay(thread))
                                                         .ToArray();
            }
            catch (InvalidOperationException)
            {
                PitchOptionsList = Enum.GetNames(typeof(ThreadDensity)); // Fall back on just using the labels
            }
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

                Bom.SaveAsync();
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

        private bool SaveAs()
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
            if (handler == null) return false;

            handler.Invoke();
            return true;
        }
    }

    public delegate void ShortcutEventHandler();

}
