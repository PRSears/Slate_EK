using Extender;
using Extender.Debugging;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
using Slate_EK.Models.IO;
using Slate_EK.Models.ThreadParameters;
using Slate_EK.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Slate_EK.ViewModels
{
    public sealed class BomViewModel : ViewModel
    {
        private Timer _PropertyRefreshTimer;

        #region         commands
        public ICommand AddToListCommand            { get; private set; }
        public ICommand RemoveItemCommand           { get; private set; }
        public ICommand EditCommand                 { get; private set; }
        public ICommand ListChangeQuantityCommand   { get; private set; }
        public ICommand ListDeleteItemCommand       { get; private set; }
        public ICommand SaveAsCommand               { get; private set; }
        public ICommand PrintCommand                { get; private set; }
        public ICommand FinalizeCommand             { get; private set; }
        public ICommand UndoFinalizeCommand         { get; private set; }
        public ICommand ShortcutCtrlK               { get; private set; }
        public ICommand ShortcutCtrlS               { get; private set; }
        public ICommand ShortcutCtrlE               { get; private set; }
        public ICommand ShortcutCtrlD               { get; private set; }
        public ICommand ShortcutCtrlA               { get; private set; }
        public ICommand ShortcutCtrlC               { get; private set; }
        public ICommand ShortcutCtrlV               { get; private set; }

        public event ShortcutEventHandler OnShortcutPressedCtrlK;
        public event ShortcutEventHandler OnWorkingFastenerSubmitted;
        #endregion

        public string WindowTitle => string.Format
        (
            "Assembly #{1} [{2}] - {0}",
            Properties.Settings.Default.ShortTitle,
            Bom.AssemblyNumber,
            Bom.SourceList?.Length.ToString() ?? "0"
        );

        public Visibility AdditionalParameterVisibility
        {
            get { return _AdditionalParameterVisibility; }
            set
            {
                _AdditionalParameterVisibility = value;
                OnPropertyChanged(nameof(AdditionalParameterVisibility));
            }
        }

        public Visibility FinalizeButtonVisibility =>  IsEditable ? Visibility.Visible : Visibility.Collapsed;
        public Visibility UndoButtonVisibility     => !IsEditable ? Visibility.Visible : Visibility.Collapsed;

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

        // Drop-down      list data sources -- it shows no reference, but they're being bound to in the xaml
        public Units[]    UnitsList            => (Units[])Enum.GetValues(typeof(Units));
        public Material[] MaterialsList        => Material.Materials.Where(m => !m.Equals(Material.Unspecified)).ToArray();
        public HoleType[] HoleTypesList        => HoleType.HoleTypes;
        public string[]   FastenerTypesList    => FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();
        public bool       IsEditable           => !Bom.IsFinalized;
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

        private Models.IO.Sizes          XmlSizes         { get; set; }
        private Models.IO.Pitches        XmlPitches       { get; set; }
        private Models.IO.ImperialSizes  XmlImperialSizes { get; set; }

        private const double TOL = 0.0001d; // tolerance to use when comparing floating point numbers

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
        private Visibility         _AdditionalParameterVisibility;

        private ObservableCollection<FastenerControl> _ObservableFasteners;

        #endregion

        public BomViewModel() : this(string.Empty)
        { }

        public BomViewModel(string assemblyNumber)
        {
            Bom                    = new Bom(assemblyNumber); 
            WorkingFastener        = new UnifiedFastener();
            ObservableFasteners    = Bom.SourceList != null ? new ObservableCollection<FastenerControl>(FastenerControl.FromArray(Bom.SourceList)) :
                                                              new ObservableCollection<FastenerControl>();

            Bom.PropertyChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

                if (Bom.SourceList != null && e.PropertyName.Equals(nameof(Bom.SourceList)))
                    RefreshBom();
                else if (e.PropertyName.Equals(nameof(Bom.AssemblyNumber)))
                {
                    Task.Run(async () =>
                    {
                        await Bom.SaveAsync();
                        Debug.WriteMessage($"Bom copied to {Bom.AssemblyNumber}", "info");
                    });
                }
                else if (e.PropertyName.Equals(nameof(Bom.IsFinalized)))
                {
                    OnPropertyChanged(nameof(FinalizeButtonVisibility));
                    OnPropertyChanged(nameof(UndoButtonVisibility));
                    OnPropertyChanged(nameof(IsEditable));
                }

                OnPropertyChanged(nameof(WindowTitle)); // do this regardless of which property changed
            };

            WorkingFastener.PropertyChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

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
                if (string.IsNullOrWhiteSpace(e.PropertyName)) return;

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
                () => NullsafeHandleShortcut(OnShortcutPressedCtrlK)
            );

            ShortcutCtrlS = new RelayCommand
            (
                () => SaveAs()
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

                    AddToBom(newFasteners.ToArray()); 
                },
                () => !string.IsNullOrWhiteSpace(Clipboard.GetText())
            // TODO_ An exception is thrown if the Clipboard is already being accessed by another process...
            //       See here: https://stackoverflow.com/questions/68666/clipbrd-e-cant-open-error-when-setting-the-clipboard-from-net
            //       Exception thrown: 'System.Runtime.InteropServices.COMException' in PresentationCore.dll
            );

            //
            // ICommands

            AddToListCommand = new RelayCommand
            (
                () =>
                {
                    WorkingFastener.ForceNewUniqueID();
                    AddToBom(WorkingFastener);
                    NullsafeHandleShortcut(OnWorkingFastenerSubmitted);
                },
                () => !Bom.IsFinalized
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
                () => !Bom.IsFinalized && ObservableFasteners.Any(f => f.IsSelected)
            );

            EditCommand = new RelayCommand
            (
                () =>
                {
                    // TODO_ Find a way to make Editing existing fasteners in BOM work
                    //       The problem is that drop down menus don't update when their selected targets update...
                    WorkingFastener.UpdateFrom(ObservableFasteners.FirstOrDefault(f => f.IsSelected)?.Fastener);
                },
                () => ObservableFasteners.Count(f => f.IsSelected) == 1 // only allow when exactly one is selected
            );

            ListChangeQuantityCommand = new RelayCommand
            (
                () =>
                {
                    ChangeQuantity(ObservableFasteners.First(f => f.IsSelected)); 
                },
                () =>
                {
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

            PrintCommand = new RelayCommand
            (
                () =>
                {
                    // if this fucking works...
                    var doc = new FlowDocument(new Paragraph(new Run(FormatBomToText())))
                    {
                        PagePadding  = new Thickness(PrintPagePadding),
                        FontFamily   = new FontFamily(PrintFontFamily),
                        FontSize     = PrintFontSize,
                        LineHeight   = PrintLineHeight,
                        PageWidth    = (int)Math.Ceiling(PrintPageWidth * PrintDpi),
                        PageHeight   = (int)Math.Ceiling(PrintPageHeight * PrintDpi),
                        ColumnWidth  = 800
                    };

                    var dialog = new PrintDialog();

                    if (dialog.ShowDialog() == true)
                    {
                        dialog.PrintDocument((doc as IDocumentPaginatorSource).DocumentPaginator, WindowTitle);
                    }

                    // >.>
                }
            );

            FinalizeCommand = new RelayCommand
            (
                () =>
                {
                    if (ConfirmationDialog.Show("Edit inventory?", "Do you want to remove the fastener quantities from the inventory?\n\n" +
                                                                   "Selecting no will just make this BOM uneditable without changing the inventory."))
                    {
                        SubtractFromInventory();
                    }

                    Bom.IsFinalized = true;
                    Bom.QueueSave();

                    CheckStock();
                },
                () => Bom.SourceList != null && Bom.SourceList.Any() 
            );

            UndoFinalizeCommand = new RelayCommand
            (
                () =>
                {
                    if (ConfirmationDialog.Show("Edit inventory?", "Do you want to add the fastener quantities back to the inventory?\n\n" + 
                                                    "Selecting no will just make this BOM editable again and leave the inventory untouched."))
                    {
                        ReplaceInventory();
                    }

                    Bom.IsFinalized = false;
                    Bom.QueueSave();
                },
                () => Bom.IsFinalized
            );

            // Hook up context menu commands for FastenerControls
            foreach (FastenerControl control in ObservableFasteners)
            {
                control.RequestingRemoval += sender => Bom.Remove((sender as FastenerControl)?.Fastener, Int32.MaxValue);
                control.RequestingQuantityChange += ChangeQuantity;
            }

            // Lists from XML
            XmlSizes         = new Models.IO.Sizes();
            XmlPitches       = new Models.IO.Pitches();
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
            _PropertyRefreshTimer.Elapsed  += (s, e) =>
            {
                XmlSizes.QueueReload();
                XmlPitches.QueueReload();
                XmlImperialSizes.QueueReload();

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
            AddToBom(new[] {fastener});
        }

        
        private void AddToBom(IEnumerable<UnifiedFastener> fasteners)
        {
            using (Inventory inv = new Inventory(Properties.Settings.Default.DefaultInventoryPath))
            {
                foreach (var fastener in fasteners)
                {
                    //
                    // Check exact match
                    if (inv.Fasteners.Any(f => f.Equals(fastener)))
                    {
                        Bom.Add(inv.Fasteners.First(f => f.Equals(fastener)));
                        continue;
                    }

                    //
                    // Look for possible substitutes
                    var canditates = inv.Fasteners.Where
                    (
                        f => (Math.Abs(f.Size - fastener.Size) < TOL)   &&
                             (Math.Abs(f.Pitch - fastener.Pitch) < TOL) &&
                             f.Type.Equals(fastener.Type)               &&
                             f.Length <= fastener.Length
                    );

                    if (canditates.Any())
                    {
                        var substituteView = new SubstituteView(canditates);
                        substituteView.Owner = FindThisWindow();
                        substituteView.ShowDialog();

                        if (substituteView.SelectedFastener != null)
                        {
                            // 
                            // A substitute was selected and can be added to the BOM
                            substituteView.SelectedFastener.Quantity = fastener.Quantity; 
                            Bom.Add(substituteView.SelectedFastener.Copy());
                            continue;
                        }
                    }

                    // 
                    // Either there was no substitute or none were selected
                    PromptQuickAdd(fastener, inv); 
                }

                inv.SubmitChanges();
            }
        }

        private void SubtractFromInventory()
        {
            // Keep a list of the fasteners that weren't in the inventory in case we want 
            // to do something with it later.
            List<UnifiedFastener> missingList = new List<UnifiedFastener>();

            using (Inventory inv = new Inventory(Properties.Settings.Default.DefaultInventoryPath))
            {
                foreach (UnifiedFastener item in Bom.SourceList)
                {
                    // Can't do a UID check because price and mass might not be set in BOM
                    var matches = inv.Fasteners.Where
                    (
                        f => (Math.Abs(f.Size - item.Size) < TOL)   &&
                             (Math.Abs(f.Pitch - item.Pitch) < TOL) &&
                             f.Type.Equals(item.Type)               &&
                             (Math.Abs(f.Length - item.Length) < TOL)
                    );

                    if (matches.Any())
                        matches.First().Quantity -= item.Quantity;
                    else
                        missingList.Add(item);
                }

                inv.SubmitChanges();
            }

            missingList.ForEach(f => Debug.WriteMessage($"Could not subtract from inventory because it was missing. {f.Description}", WarnLevel.Warn));

            // THOUGHT Do we want to display a list of fasteners that aren't in the inventory?
            //         In most cases (that I can think of...) the user should already have been 
            //         prompted about adding them to the inventory.
        }

        private void ReplaceInventory()
        {
            // TODOh double check that replacing quantities in inventory is functioning correctly
            using (Inventory inv = new Inventory(Properties.Settings.Default.DefaultInventoryPath))
            {
                foreach (UnifiedFastener item in Bom.SourceList)
                {
                    // Can't do a UID check because price and mass might not be set in BOM
                    var matches = inv.Fasteners.Where
                    (
                        f => (Math.Abs(f.Size - item.Size) < TOL)   &&
                             (Math.Abs(f.Pitch - item.Pitch) < TOL) &&
                             f.Type.Equals(item.Type)               &&
                             (Math.Abs(f.Length - item.Length) < TOL)
                    );

                    if (matches.Any())
                        matches.First().Quantity += item.Quantity;
                    else
                        Debug.WriteMessage($"Could not replace fastener in inventory because it was missing. {item.Description}", WarnLevel.Warn);
                }

                inv.SubmitChanges();
            }
        }

        private void CheckStock()
        {
            using (Inventory inv = new Inventory(Properties.Settings.Default.DefaultInventoryPath))
            {
                var query = inv.Fasteners.Where(f => f.Quantity <= Properties.Settings.Default.LowStockThreshold);

                if (query.Any())
                {
                    var alertDialog = new SubstituteView(query, SubstituteViewModel.Modes.LowStockList);
                    alertDialog.ShowDialog();
                }
            }
        }

        private void PromptQuickAdd(UnifiedFastener fastener, Inventory inventory)
        {
            var promptResult = MessageBox.Show
            (
                "Do you want to open the quick editor and add a new item to the inventory?",
                "No Suitable Fastener(s) Found",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            bool requireWarn = true;

            if (promptResult == MessageBoxResult.Yes)
            {
                var quickEditor = new QuickEditDialog(fastener.Copy());
                quickEditor.Owner = FindThisWindow();
                quickEditor.ShowDialog();

                if (quickEditor.Result == QuickEditDialogResult.Submit)
                {
                    inventory.Add(quickEditor.Editing);
                    requireWarn = false;
                }
            }

            if (requireWarn) // Either the quick editor was never shown, or it was and they discarded it.
                MessageBox.Show
                (
                    "The fastener will be added to the BOM anyway, but nothing was added to the inventory.",
                    "Note:",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

            Bom.Add(fastener.Copy());
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
                                                         .Where(d => !d.EndsWith(" 0 TPI"))
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
                if (!ObservableFasteners.Any(f => f.IsSelected))
                {
                    // No items selected... we edit whatever element the user clicked on
                    var match = Bom.SourceList.FirstOrDefault(f => ((FastenerControl)sender).Fastener.UniqueID.Equals(f.UniqueID));
                    if (match != null && !match.Equals(default(UnifiedFastener)))
                        match.Quantity = dialog.Value;
                }
                else
                {
                    // Has one (or more) items selected... edit them all
                    foreach (var selected in ObservableFasteners.Where(fc => fc.IsSelected))
                    {
                        var match = Bom.SourceList.FirstOrDefault(f => f.UniqueID.Equals(selected.Fastener.UniqueID));
                        if (match != null && !match.Equals(default(UnifiedFastener)))
                            match.Quantity = dialog.Value;
                    }
                }

                Bom.QueueSave();
            }
            else Bom.Remove(((FastenerControl)sender).Fastener, Int32.MaxValue);

            RefreshBom(); // Make sure the ObservableCollection is updated to reflect the change in Bom.SourceList.
        }

        private Window FindThisWindow()
        {
            // HACK Yeah... this is bad.
            Window mainWindow = Application.Current.MainWindow;
            return (mainWindow as Views.MainView)?.FindBomWindow(Bom.AssemblyNumber);
        }

        private bool SaveAs()
        {
            // TODO_ sometimes crashes when saving as txt file. !! - can't reproduce...
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title           = "Save a copy of the BOM as...",
                DefaultExt      = ".txt",
                Filter          = @"(*.txt)
|*.txt|(*.csv)|*.csv|(*.xlsx)|*.xlsx|(*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension    = true,
                OverwritePrompt = true,
                FileName        = Path.GetFileNameWithoutExtension(Bom.FilePath)
            };

            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value) return false;

            var ext = Path.GetExtension(dialog.FileName)?.ToLower();
            if (string.IsNullOrWhiteSpace(ext)) return false;

            try
            {
                if (File.Exists(dialog.FileName))
                    File.Delete(dialog.FileName);

                if (ext.EndsWith("csv"))
                {
                    using (var writer = File.CreateText(dialog.FileName))
                        writer.Write(FormatBomToCsv());
                }
                else if (ext.EndsWith("txt"))
                {
                    using (var writer = File.CreateText(dialog.FileName))
                        writer.Write(FormatBomToText());
                }
                else if (ext.EndsWith("xml"))
                {
                    File.Copy(Bom.FilePath, dialog.FileName);
                } 
                else if (ext.EndsWith("xlsx"))
                {
                    ExportToExcel(dialog.FileName, false);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show
                (
                    $"Encountered an exception while saving as {ext}:\n{e.Message}",
                    "Exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                ExceptionTools.WriteExceptionText(e, true);
                return false; 
            }

            return true;
        }

        private string FormatBomToText()
        {
            if (Bom.SourceList.Length <= 0)
                return string.Empty;

            StringBuilder buffer = new StringBuilder();

            if (Properties.Settings.Default.AlignDescriptionsTxtExport)
            {
                if (IncludePrintHeaders) buffer.AppendLine(Bom.SourceList[0].AlignedPrintHeaders);
                Bom.SourceList.ForEach(f => buffer.AppendLine(f.AlignedPrintDescription));
            }
            else
            {
                if (IncludePrintHeaders) buffer.AppendLine(Bom.SourceList[0].PrintHeaders);
                Bom.SourceList.ForEach(f => buffer.AppendLine(f.DescriptionForPrint));
            }

            return buffer.ToString();
        }

        private string FormatBomToCsv()
        {
            if (Bom.SourceList.Length <= 0)
                return string.Empty;

            StringBuilder buffer = new StringBuilder();

            // There's a switch for this in Properties.Settings, but as of 2016-03-28 there's nothing controlling it in the UI
            if (TwoColumnCsv) 
            {
                buffer.AppendLine("callout,qty");
                foreach (var item in Bom.SourceList)
                    buffer.AppendLine($"{(ExportLengthFractions ? item.DescriptionWithFrac : item.Description)},{item.Quantity}");
            }
            else
            {
                buffer.AppendLine("quantity,size,pitch,length,type,total mass,unit price,sub total");
                foreach (var item in Bom.SourceList)
                {
                    string len = ExportLengthFractions ? item.LengthFraction : item.LengthDisplay;

                    buffer.AppendLine($"{item.Quantity},{item.SizeDisplay},{item.ShortPitchDisplay},{len},{item.Type},{item.Mass * item.Quantity},{item.Price},{item.Price * item.Quantity}");
                }
            }

            return buffer.ToString();
        }

        private void ExportToExcel(string filename, bool append)
        {
            if (!append && File.Exists(filename))
                File.Delete(filename);

            ExcelExporter ex = new ExcelExporter(filename);

            ex.Append(Bom.SourceList);
            ex.Save();
        }

        private void NullsafeHandleShortcut(ShortcutEventHandler handler)
        {
            handler?.Invoke();
        }

        private string PrintFontFamily       => Properties.Settings.Default.PrintFontFamily;
        private int    PrintFontSize         => Properties.Settings.Default.PrintFontSize;
        private int    PrintLineHeight       => Properties.Settings.Default.PrintFontLineHeight;
        private int    PrintPagePadding      => Properties.Settings.Default.PrintPagePadding;
        private int    PrintDpi              => Properties.Settings.Default.PrintDpi;
        private float  PrintPageWidth        => Properties.Settings.Default.PrintPageWidth;
        private float  PrintPageHeight       => Properties.Settings.Default.PrintPageHeight;
        private bool   IncludePrintHeaders   => Properties.Settings.Default.IncludePrintHeaders;
        private bool   ExportLengthFractions => Properties.Settings.Default.ExportLengthFractions;
        private bool   TwoColumnCsv          => Properties.Settings.Default.TwoColumnCsv;
    }

    public delegate void ShortcutEventHandler();

}
