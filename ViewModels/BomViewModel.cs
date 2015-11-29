﻿using Extender;
using Extender.Debugging;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
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

        #region commands
        public ICommand AddToListCommand            { get; private set; }
        public ICommand RemoveItemCommand           { get; private set; }
        public ICommand ListChangeQuantityCommand   { get; private set; }
        public ICommand ListDeleteItemCommand       { get; private set; }
        public ICommand SaveAsCommand               { get; private set; }
        public ICommand PrintCommand                { get; private set; }
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

        // Drop-down list data sources -- it shows no reference, but they're being bound to in the xaml
        public Units[]    UnitsList            => (Units[])Enum.GetValues(typeof(Units));
        public Material[] MaterialsList        => Material.Materials.Where(m => !m.Equals(Material.Unspecified)).ToArray();
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
                    // Check exact match
                    if (inv.Fasteners.Any(f => f.Equals(fastener)))
                    {
                        Bom.Add(inv.Fasteners.First(f => f.Equals(fastener)));
                        continue;
                    }

                    // No exact match, try to find an acceptable substitute
                    // TODOh Change as indicated in notes from call with Eric
                    var substitute = inv.Fasteners.FirstOrDefault
                    (
                        f => f.Size.Equals(fastener.Size)         && 
                             f.Pitch.Equals(fastener.Pitch)       && 
                             f.Type.Equals(fastener.Type)         && 
                             (f.Length >= fastener.Length)
                    );

                    if (substitute == null || substitute.Equals(default(UnifiedFastener)))
                    {
                        Debug.WriteMessage("No suitable fastener found.");
                        
                        var result = MessageBox.Show
                        (
                            "Do you want to open the quick editor and add it to the inventory?",
                            "No Suitable Fastener Found",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question
                        );

                        bool warn = true;

                        if (result == MessageBoxResult.Yes) 
                        {
                            var qedit = new Views.QuickEditDialog(fastener.Copy());
                            qedit.Owner = FindThisWindow();
                            qedit.ShowDialog();

                            if (qedit.Result == QuickEditDialogResult.Submit)
                            {
                                inv.Add(qedit.Editing);
                                warn = false;
                            }
                        }

                        if (warn) // Either the quick editor was never shown, or it was and they discarded it
                        {
                            MessageBox.Show
                            (
                                "The fastener will be added to the BOM anyway, but nothing was added to the inventory.",
                                "",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        }

                        substitute = fastener;
                    }

                    Bom.Add(substitute.Copy());
                }

                inv.SubmitChanges();
            }


        } 

        //
        // TODOh add button to finalize BOM + remove quantities from inventory
        //       - should freeze the bom not allowing other edits / additions
        //       - change button to undo finalize once clicked & handled
        //
        // This is probably going to require an IsFinalized field in the XML file (& therefor the BOM).

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
                //// Edit the value in the source list directly... need to select the fastener control from Bom.SourceList first.

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
            // Yeah... this is bad.
            Window mainWindow = Application.Current.MainWindow;
            return (mainWindow as Views.MainView)?.FindBomWindow(Bom.AssemblyNumber);
        }

        private bool SaveAs()
        {
            // TODOh crashes when saving as txt file. !! - can't reproduce...
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title           = "Save a copy of the BOM as...",
                DefaultExt      = ".txt",
                Filter          = @"(*.txt)
|*.txt|(*.csv)|*.csv|(*.xml)|*.xml|All files (*.*)|*.*",
                AddExtension    = true,
                OverwritePrompt = true,
                FileName        = Path.GetFileName(Bom.FilePath)
            };

            var result = dialog.ShowDialog();
            if (!result.HasValue || !result.Value) return false;

            var ext = Path.GetExtension(dialog.FileName)?.ToLower();
            if (string.IsNullOrWhiteSpace(ext)) return false;

            if (ext.EndsWith("csv"))
            {
                var csv = new Extender.IO.CsvSerializer<UnifiedFastener>();

                using (var stream = new FileStream(dialog.FileName, FileMode.Create,
                                                                    FileAccess.ReadWrite,
                                                                    FileShare.Read))
                {
                    csv.Serialize(stream, Bom.SourceList);
                }
            }
            else if (ext.EndsWith("xml"))
            {
                try
                {
                    if (File.Exists(dialog.FileName))
                        File.Delete(dialog.FileName);

                    File.Copy(Bom.FilePath, dialog.FileName);
                }
                catch (Exception e)
                {
                    ExceptionTools.WriteExceptionText(e, false);

                    MessageBox.Show
                    (
                        "Encountered an exception while copying BOM:\n" + e.Message,
                        "Exception",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    return false;
                }
            }
            else if (ext.EndsWith("txt"))
            {
                try
                {
                    if (File.Exists(dialog.FileName))
                        File.Delete(dialog.FileName);

                    using (var writer = File.CreateText(dialog.FileName))
                    {
                        writer.Write(FormatBomToText());
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show
                    (
                        "Encountered an exception while saving as .txt:\n" + e.Message,
                        "Exception",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                    ExceptionTools.WriteExceptionText(e, true);
                    return false;
                }
            }

            return true;
        }

        private string FormatBomToText()
        {
            if (Bom.SourceList.Length <= 0)
                return string.Empty;

            StringBuilder buffer = new StringBuilder();

            if (Properties.Settings.Default.AlignDescriptionsPrint)
            {
                if (IncludePrintHeaders) buffer.AppendLine(Bom.SourceList[0].AlignedPrintheaders);
                Bom.SourceList.ForEach(f => buffer.AppendLine(f.AlignedPrintDescription));
            }
            else
            {
                if (IncludePrintHeaders) buffer.AppendLine(Bom.SourceList[0].PrintHeaders);
                Bom.SourceList.ForEach(f => buffer.AppendLine(f.DescriptionForPrint));
            }

            return buffer.ToString();
        }

        private void NullsafeHandleShortcut(ShortcutEventHandler handler)
        {
            handler?.Invoke();
        }

        private string PrintFontFamily     => Properties.Settings.Default.PrintFontFamily;
        private int    PrintFontSize       => Properties.Settings.Default.PrintFontSize;
        private int    PrintLineHeight     => Properties.Settings.Default.PrintFontLineHeight;
        private int    PrintPagePadding    => Properties.Settings.Default.PrintPagePadding;
        private int    PrintDpi            => Properties.Settings.Default.PrintDpi;
        private float  PrintPageWidth      => Properties.Settings.Default.PrintPageWidth;
        private float  PrintPageHeight     => Properties.Settings.Default.PrintPageHeight;
        private bool   IncludePrintHeaders => Properties.Settings.Default.IncludePrintHeaders;
    }

    public delegate void ShortcutEventHandler();

}
