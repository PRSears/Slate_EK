using Extender;
using Extender.Debugging;
using Extender.IO;
using Extender.WPF;
using Microsoft.Win32;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
using Slate_EK.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public enum SortMethod  { None, Quantity, Price, Mass, Size, Length, Pitch, Material, FastType, Unit }
    public enum SearchType : byte { Quantity, Price, Mass, Size, Length, Pitch, Material, FastenerType, Unit }

    public sealed class InventoryViewModel : ViewModel
    {
        #region // ICommands

        //
        // InventoryBoxContextMenu               commands
        public ICommand AddNewFastenerCommand    { get; private set; }
        public ICommand RemoveFastenerCommand    { get; private set; }
        public ICommand EditSelectedCommand      { get; private set; }
        public ICommand SelectAllCommand         { get; private set; }
        public ICommand SelectNoneCommand        { get; private set; }
        public ICommand CopyCommand              { get; private set; }
        public ICommand TestHarnessCommand       { get; private set; }

        //
        // Main menu commands
        public ICommand OpenCommand                 { get; private set; }
        public ICommand ExportCommand               { get; private set; }
        public ICommand ImportCommand               { get; private set; }
        public ICommand ShowAllFastenersCommand     { get; private set; }
        public ICommand DisplayLowStockCommand      { get; private set; }
        public ICommand ClearQueryResultsCommand    { get; private set; }
        public ICommand DropDatabaseCommand         { get; private set; }

        //
        // Sort commands
        public ICommand SortByQuantityCommand   { get; private set; }
        public ICommand SortByPriceCommand      { get; private set; }
        public ICommand SortByMassCommand       { get; private set; }
        public ICommand SortBySizeCommand       { get; private set; }
        public ICommand SortByLengthCommand     { get; private set; }
        public ICommand SortByPitchCommand      { get; private set; }
        public ICommand SortByMaterialCommand   { get; private set; }
        public ICommand SortByFastTypeCommand   { get; private set; }
        public ICommand SortByUnitCommand       { get; private set; }

        //
        // Lower controls
        public ICommand SubmitChangesCommand    { get; private set; }
        public ICommand DiscardChangesCommand   { get; private set; }
        public ICommand ExecuteSearchCommand    { get; private set; }

        public ICommand ShortcutCtrlQ           { get; private set; }

        public event ShortcutEventHandler ShortcutPressedCtrlQ;

        #endregion

        private static string[] SubmitImageSourceStrings => new[]
        {
            "/Slate_EK;component/Icons/ic_publish_grey_24dp_2x.png",
            "/Slate_EK;component/Icons/ic_publish_black_24dp_2x.png"
        };
        private static string[] DiscardImageSourceStrings => new[]
        {
            "/Slate_EK;component/Icons/ic_delete_grey_24dp_2x.png",
            "/Slate_EK;component/Icons/ic_delete_black_24dp_2x.png"
        };

        //
        // Bound properties
        private ObservableCollection<FastenerControl> _FastenerList;
        private Func<FastenerControl, object>         _LastSearchSelector;
        
        private SortMethod             _LastSortBy;
        private bool                   _OrderByDescending;
        private bool                   _PendingOperations;
        private bool                   _EditMode;
        private Inventory              _Inventory;
        private Queue<UnifiedFastener> _PendingFasteners;
        private List<UnifiedFastener>  _FastenersMarkedForRemoval;
        private string                 _SearchQuery;


        private bool Debug => Properties.Settings.Default.Debug;

        public string     SearchQuery
        {
            get { return _SearchQuery; }
            set
            {
                _SearchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }
        public string[]   SearchByPropertyList   => Enum.GetNames(typeof(SearchType));
        public string     SelectedSearchProperty
        {
            get
            {
                return Properties.Settings.Default.Inventory_DefaultSearchBy;
            }
            set
            {
                Properties.Settings.Default.Inventory_DefaultSearchBy = value;
                Properties.Settings.Default.Save();

                OnPropertyChanged(nameof(SelectedSearchProperty));
            }
        }
        public SearchType SelectedSearchType     => (SearchType)Array.IndexOf(SearchByPropertyList, SelectedSearchProperty);
        public bool       OrderByDescending
        {
            get
            {
                return _OrderByDescending;
            }
            set
            {
                _OrderByDescending = value;
                OnPropertyChanged(nameof(OrderByDescending));
            }
        }
        public string     SubmitImageSource      => PendingOperations ? SubmitImageSourceStrings[1]  : SubmitImageSourceStrings[0];
        public string     DiscardImageSource     => PendingOperations ? DiscardImageSourceStrings[1] : DiscardImageSourceStrings[0];
        public Visibility EditModeVisibility     => EditMode ? Visibility.Visible : Visibility.Hidden;
        public Visibility DebugModeVisibilty     => Debug ? Visibility.Visible : Visibility.Collapsed;
        public string     WindowTitle            => $"Inventory Viewer - {Path.GetFileName(_Inventory.Filename)}";
        public string     QuantityButtonText     => _LastSortBy == SortMethod.Quantity ? $"Quantity {OrderIndicator}" : "Quantity  ";
        public string     PriceButtonText        => _LastSortBy == SortMethod.Price ? $"Price {OrderIndicator}" : "Price  ";
        public string     MassButtonText         => _LastSortBy == SortMethod.Mass ? $"Mass {OrderIndicator}" : "Mass  ";
        public string     SizeButtonText         => _LastSortBy == SortMethod.Size ? $"Size {OrderIndicator}" : "Size  ";
        public string     PitchButtonText        => _LastSortBy == SortMethod.Pitch ? $"Pitch {OrderIndicator}" : "Pitch  ";
        public string     LengthButtonText       => _LastSortBy == SortMethod.Length ? $"Length {OrderIndicator}" : "Length  ";
        public string     MaterialButtonText     => _LastSortBy == SortMethod.Material ? $"Material {OrderIndicator}" : "Material  ";
        public string     TypeButtonText         => _LastSortBy == SortMethod.FastType ? $"Type {OrderIndicator}" : "Type  ";
        public string     UnitButtonText         => _LastSortBy == SortMethod.Unit ? $"Unit {OrderIndicator}" : "Unit  ";
        private string    OrderIndicator         => OrderByDescending ? "\u2193" : "\u2191";
        public bool       PendingOperations
        {
            get
            {
                return _PendingOperations;
            }
            set
            {
                _PendingOperations = value;
                OnPropertyChanged(nameof(PendingOperations));
                OnPropertyChanged(nameof(SubmitImageSource));
                OnPropertyChanged(nameof(DiscardImageSource));
            }
        }
        public bool       EditMode
        {
            get
            {
                return _EditMode; 
            }
            set
            {
                _EditMode = value;
                PendingOperations = PendingOperations | _EditMode;

                OnPropertyChanged(nameof(EditMode));
                OnPropertyChanged(nameof(EditModeVisibility));
            }
        }

        public ObservableCollection<FastenerControl> FastenerList
        {
            get
            {
                return _FastenerList;
            }
            set
            {
                _FastenerList = value;
                OnPropertyChanged(nameof(FastenerList));
            }
        }

        //
        //---------------------------------------------------------------------------------

        /// <summary>
        /// Constructs and initializes a new InventoryViewModel
        /// </summary>
        public InventoryViewModel(string filename)
        {
            Initialize();

            FastenerList               = new ObservableCollection<FastenerControl>();
            SearchQuery                = string.Empty;
            _PendingFasteners          = new Queue<UnifiedFastener>();
            _FastenersMarkedForRemoval = new List<UnifiedFastener>();
            _Inventory                 = new Inventory(filename);
            _LastSortBy                = SortMethod.None;
        }

        public override void Initialize()
        {
            base.Initialize();

            #region // InventoryBoxContextMenu commands

            AddNewFastenerCommand = new RelayCommand
            (
                () =>
                {
                    int number = 1;

                    if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        var dialog = new Views.NumberPickerDialog(1);

                        dialog.Owner = (Application.Current.MainWindow as MainView)?.FindInventoryWindow();
                        dialog.ShowDialog();

                        if (!dialog.Success || dialog.Value < 1) return;

                        number = dialog.Value;
                    }

                    while ((number--) > 0)
                    {
                        if (!EditMode)
                            FastenerList.Clear();

                        AddToFastenerList(new FastenerControl(new UnifiedFastener()));

                        PendingOperations = true;
                        EnterEditMode(false);
                    }
                }    
            );

            RemoveFastenerCommand = new RelayCommand
            (
                () =>
                {
                    var removal = FastenerList.Where(fc => fc.IsSelected).ToArray();

                    foreach (var item in removal)
                    {
                        PendingOperations = PendingOperations | _Inventory.Remove(item.Fastener);
                        FastenerList.Remove(item);

                        if (_PendingFasteners.Contains(item.Fastener))
                            _FastenersMarkedForRemoval.Add(item.Fastener);
                    }
                },
                () => FastenerList.Any(f => f.IsSelected)
            );

            EditSelectedCommand = new RelayCommand
            (
                () =>
                {
                    var selected = FastenerList.Where(fc => fc.IsSelected).ToArray();
                    FastenerList.Clear();
                    selected.ForEach(fc => AddToFastenerList(new FastenerControl(fc.Fastener.Copy())));

                    foreach (var item in selected)
                        PendingOperations = PendingOperations | _Inventory.Remove(item.Fastener);

                    EnterEditMode(false);
                }, 
                () => FastenerList.Any(fc => fc.IsSelected)
            );

            SelectAllCommand = new RelayCommand
            (
                () => FastenerList.ForEach(f => f.IsSelected = true)
            );

            SelectNoneCommand = new RelayCommand
            (
                () => FastenerList.ForEach(f => f.IsSelected = false)
            );

            CopyCommand = new RelayCommand
            (
                () =>
                {
                    var buffer = new StringBuilder();
                    foreach (var selected in FastenerList.Where(f => f.IsSelected).Select(f => f.Fastener))
                        buffer.AppendLine(selected.Description);

                    if (buffer.Length > 0)
                        Clipboard.SetText(buffer.ToString());
                },
                () => FastenerList.Any(f => f.IsSelected)
            );

            TestHarnessCommand = new RelayCommand
            (
                () =>
                {
                    if (ConfirmationDialog.Show("Empty the database?", ""))
                    {
                        _Inventory.Fasteners.ForEach(f => _Inventory.Remove(f));
                        _Inventory.SubmitChanges();
                    }
                }
            );

            #endregion

            #region // Main menu commands

            OpenCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new OpenFileDialog()
                    {
                        AddExtension = true,
                        DefaultExt = "mdf",
                        CheckFileExists = false,
                        CheckPathExists = false,
                        InitialDirectory = Path.GetDirectoryName(_Inventory.Filename)
                    };

                    var result = dialog.ShowDialog();

                    if (!result.HasValue || !result.Value) return;

                    _Inventory.Dispose();
                    _Inventory = new Inventory(dialog.FileName);

                    SearchQuery = string.Empty;
                    FastenerList.Clear();

                    Extender.Debugging.Debug.WriteMessage($"Window Title: {WindowTitle}");

                    OnPropertyChanged(nameof(WindowTitle));
                }
            );

            ExportCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new Microsoft.Win32.SaveFileDialog()
                    {
                        Title           = "Save Query Results",
                        DefaultExt      = ".csv",
                        Filter          = @"(*.csv)
|*.csv|(*.txt)|*.txt|All files (*.*)|*.*",
                        AddExtension    = false,
                        OverwritePrompt = true,
                        FileName        = $"Inventory_{DateTime.Today.ToString("yyyy-MM-dd")}.csv"
                    };

                    var result = dialog.ShowDialog();
                    if (!result.HasValue || !result.Value) return;


                    var ext = Path.GetExtension(dialog.FileName)?.ToLower();
                    if (string.IsNullOrWhiteSpace(ext)) return;

                    try
                    {
                        if (File.Exists(dialog.FileName))
                            File.Delete(dialog.FileName);

                        if (ext.EndsWith("csv"))
                        {
                            var serializer = new CsvSerializer<UnifiedFastener>();
                            using (var stream = new FileStream(dialog.FileName, FileMode.OpenOrCreate,
                                                                                FileAccess.ReadWrite,
                                                                                FileShare.Read))
                            {
                                serializer.Serialize(stream, FastenerList.Select(fc => fc.Fastener).ToArray());
                            }
                        }
                        else if (ext.EndsWith("txt"))
                        {
                            var buffer = new StringBuilder();

                            if (Properties.Settings.Default.AlignDescriptionsPrint)
                                 FastenerList.ForEach(f => buffer.AppendLine(f.Fastener.AlignedPrintDescription));
                            else FastenerList.ForEach(f => buffer.AppendLine(f.Fastener.DescriptionForPrint));

                            using (var writer = File.CreateText(dialog.FileName))
                                writer.Write(buffer.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show
                        (
                            $"Encountered an exception while exporting as {ext}:\n" + e.Message,
                            "Exception",
                            MessageBoxButton.OK,
                            MessageBoxImage.Error
                        );
                        ExceptionTools.WriteExceptionText(e, true);
                    }
                    
                },
                () => FastenerList.Any()
            );

            ImportCommand = new RelayCommand
            (
                () =>
                {
                    var dialog = new OpenFileDialog()
                    {
                        DefaultExt = ".csv",
                        Filter = @"CSV files (*.csv)
                |*.csv",
                        CheckFileExists = true
                    };

                    if (dialog.ShowDialog() == false) return;

                    FileInfo file  = new FileInfo(dialog.FileName);
                    var serializer = new CsvSerializer<UnifiedFastener>();

                    if (!file.Exists || !file.Extension.ToLower().EndsWith("csv")) return;

                    using (var stream = new FileStream(
                        file.FullName,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite))
                    {
                        try
                        {
                            var deserializedFasteners = serializer.Deserialize(stream);
                            EnterEditMode(true);

                            foreach (var newFastener in deserializedFasteners)
                            {
                                AddToFastenerList(new FastenerControl(newFastener));
                                _PendingFasteners.Enqueue(newFastener);
                            }
                        }
                        catch (Exception e) 
                        {
                            MessageBox.Show
                            (
                                "Encountered an exception while importing from file:\n" + e.Message,
                                "Exception",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error
                            );

                            Extender.Debugging.ExceptionTools.WriteExceptionText(e, true);
                            throw;
                        }

                    }
                }
            );

            ShowAllFastenersCommand = new RelayCommand
            (
                () =>
                {
                    FastenerList.Clear();
                    _Inventory.Dump().ForEach(f => AddToFastenerList(new FastenerControl(f)));

                    SearchQuery = "*";
                }
            );

            DisplayLowStockCommand = new RelayCommand
            (
                () =>
                {
                    FastenerList.Clear();
                    SelectedSearchProperty = Enum.GetName(typeof(SearchType), SearchType.Quantity);
                    SearchQuery = $"<={Properties.Settings.Default.LowStockThreshold}";

                    ExecuteSearch();

                    SortFastenerListBy(_LastSearchSelector, _LastSortBy, false); // Re-apply the last sort
                }
            );

            ClearQueryResultsCommand = new RelayCommand
            (
                () =>
                {
                    FastenerList.Clear();
                }
            );

            DropDatabaseCommand = new RelayCommand
            (
                () =>
                {
                    if (ConfirmationDialog.Show("Are you really, really sure?", 
                        "Dropping the database will result in complete loss of stored data, and remove it from the local server.\n" + 
                        "This should really only be used for debugging.\n" +
                        "\nSo you really have to do this?"
                        ))
                    {
                        _Inventory.DEBUG_DropDatabase();
                        CloseCommand.Execute(null);
                    }
                }
            );

            #endregion

            #region // SortBy Commands

            SortByQuantityCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Quantity, SortMethod.Quantity)
            );

            SortByPriceCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Price, SortMethod.Price)
            );

            SortByMassCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Mass, SortMethod.Mass)
            );

            SortBySizeCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Size, SortMethod.Size)
            );

            SortByLengthCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Length, SortMethod.Length)
            );

            SortByPitchCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Pitch, SortMethod.Pitch)
            );

            SortByMaterialCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Material, SortMethod.Material)
            );

            SortByFastTypeCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Type, SortMethod.FastType)
            );

            SortByUnitCommand = new RelayCommand
            (
                () => SortFastenerListBy(f => f.Fastener.Unit, SortMethod.Unit)
            );

            #endregion

            #region Lower controls

            SubmitChangesCommand = new RelayCommand
            (
                () =>
                {
                    while (_PendingFasteners.Any())
                    {
                        if (!_FastenersMarkedForRemoval.Contains(_PendingFasteners.Peek()))
                            _Inventory.Add(_PendingFasteners.Dequeue());
                        else
                            _FastenersMarkedForRemoval.Remove(_PendingFasteners.Dequeue());
                    }

                    _Inventory.SubmitChanges();

                    PendingOperations = false;
                    _Inventory.InitDataContext();
                    LeaveEditMode();
                },
                () => PendingOperations
            );

            DiscardChangesCommand = new RelayCommand
            (
                () =>
                {
                    if (ConfirmationDialog.Show("Confirm", "Are you sure you want to discard all pending operations?"))
                    {
                        _PendingFasteners.Clear();
                        _FastenersMarkedForRemoval.Clear();
                        _Inventory.InitDataContext();

                        PendingOperations = false;
                        LeaveEditMode();
                    }
                },
                () => PendingOperations
            );

            ExecuteSearchCommand = new RelayCommand
            (
                () =>
                {
                    if (string.IsNullOrWhiteSpace(SearchQuery))
                        return;

                    FastenerList.Clear();
                    SearchQuery = SearchQuery.Trim();
                    ExecuteSearch();
                }
            );

            ShortcutCtrlQ = new RelayCommand(() => HandleShortcut(ShortcutPressedCtrlQ));

            #endregion
        }

        /// <param name="keySelector"></param>
        /// <param name="sortMethod">Which column is being sorted.</param>
        /// <param name="invertIfRepeat">Switch OrderBy/OrderByDescending if the last sort was on the same
        /// column as the current sort.</param>
        private void SortFastenerListBy(Func<FastenerControl, object> keySelector, SortMethod sortMethod, bool invertIfRepeat = true)
        {
            if (keySelector == null) return;

            if (invertIfRepeat)
                OrderByDescending = (_LastSortBy == sortMethod) ? !OrderByDescending : OrderByDescending;

            var sorted = OrderByDescending ? _FastenerList.OrderByDescending(keySelector).ToArray() : 
                                             _FastenerList.OrderBy(keySelector).ToArray();

            FastenerList.Clear();
            sorted.ForEach(AddToFastenerList);

            _LastSortBy         = sortMethod;
            _LastSearchSelector = keySelector;

            OnPropertyChanged(nameof(QuantityButtonText));
            OnPropertyChanged(nameof(MassButtonText));
            OnPropertyChanged(nameof(PriceButtonText));
            OnPropertyChanged(nameof(SizeButtonText));
            OnPropertyChanged(nameof(PitchButtonText));
            OnPropertyChanged(nameof(LengthButtonText));
            OnPropertyChanged(nameof(MaterialButtonText));
            OnPropertyChanged(nameof(TypeButtonText));
            OnPropertyChanged(nameof(UnitButtonText));
            // It's easier just to update all of them, instead of checking which two need updating.
        }
        
        private void EnterEditMode(bool clearFirst)
        {
            if(clearFirst)
                FastenerList.Clear();
            else
                FastenerList.ForEach(f => f.IsEditable = true);

            EditMode = true;
        }

        private void LeaveEditMode()
        {
            EditMode = false;
            FastenerList.Clear();

            ExecuteSearch(); // go back to search query if we had one.
        }

        private void AddToFastenerList(FastenerControl fastener)
        {
            fastener.Fastener.PropertyChanged += (sender, args) =>
            {
                if (string.IsNullOrWhiteSpace(args.PropertyName)) return;

                if (!_PendingFasteners.Contains(fastener.Fastener))
                {
                    _PendingFasteners.Enqueue(fastener.Fastener);
                    PendingOperations = true;
                }
            };

            FastenerList.Add(fastener);
        }

        private void ExecuteSearch()
        {
            if (SearchQuery.Equals("*"))
            {
                ShowAllFastenersCommand.Execute(null);
                return;
            }

            List<FastenerControl> queryResults = new List<FastenerControl>();

            #region Note:
            // This could be inverted -- Do one big switch statement, with checks for the modifiers
            // within each case. At the time of writing the current method seemed clearer - albeit 
            // at the cost of a lot of code duplication. 
            #endregion
            if (SearchQuery.StartsWith("<="))
            {
                #region // Less than or equal to search
                string query = SearchQuery.TrimStart(new char[] {'<', '>', '='}).Trim();
                switch (SelectedSearchType)
                {
                    case SearchType.Length:
                        double parsed;
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length <= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Mass:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass <= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Pitch:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch <= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Price:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price <= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Size:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size <= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Quantity:
                        int parsedInt;
                        if (int.TryParse(query, out parsedInt))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity <= parsedInt)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    default:
                        Extender.Debugging.Debug.WriteMessage(@"Operator ""<="" cannot be applied to string searches.", "warn");
                        SearchQuery = query;
                        ExecuteSearch();
                        return;
                }
                #endregion
            }
            else if (SearchQuery.StartsWith(">="))
            {
                #region // Greater than or equal to search
                string query = SearchQuery.TrimStart(new char[] {'<', '>', '='}).Trim();
                switch (SelectedSearchType)
                {
                    case SearchType.Length:
                        double parsed;
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length >= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Mass:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass >= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Pitch:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch >= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Price:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price >= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Size:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size >= parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Quantity:
                        int parsedInt;
                        if (int.TryParse(query, out parsedInt))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity >= parsedInt)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    default:
                        Extender.Debugging.Debug.WriteMessage(@"Operator "">="" cannot be applied to string searches.", "warn");
                        SearchQuery = query;
                        ExecuteSearch();
                        return;
                }
                #endregion
            }
            else if (SearchQuery.StartsWith("<"))
            {
                #region // Less than search
                string query = SearchQuery.TrimStart(new char[] {'<', '>'}).Trim();
                switch (SelectedSearchType)
                {
                    case SearchType.Length:
                        double parsed;
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length < parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Mass:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass < parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Pitch:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch < parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Price:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price < parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Size:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size < parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Quantity:
                        int parsedInt;
                        if (int.TryParse(query, out parsedInt))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity < parsedInt)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    default:
                        Extender.Debugging.Debug.WriteMessage(@"Operator ""<"" cannot be applied to string searches.", "warn");
                        SearchQuery = query;
                        ExecuteSearch();
                        return;
                } 
                #endregion
            }
            else if (SearchQuery.StartsWith(">"))
            {
                #region // Greater than search
                string query = SearchQuery.TrimStart(new char[] {'<', '>'}).Trim();
                switch (SelectedSearchType)
                {
                    case SearchType.Length:
                        double parsed;
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length > parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Mass:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass > parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Pitch:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch > parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Price:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price > parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Size:
                        if (double.TryParse(query, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size > parsed)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Quantity:
                        int parsedInt;
                        if (int.TryParse(query, out parsedInt))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity > parsedInt)
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    default:
                        Extender.Debugging.Debug.WriteMessage(@"Operator "">"" cannot be applied to string searches.", "warn");
                        SearchQuery = query;
                        ExecuteSearch();
                        return;
                }
                #endregion
            }
            else if (SearchQuery.Contains(":"))
            {
                #region // Range search
                string[] preStrings = SearchQuery.Split(':');
                string queryA = preStrings[0].Trim(), queryB = preStrings[1].Trim();

                double parsedA = 0, parsedB = 0;
                bool parseFailed = false;

                parseFailed = parseFailed || !double.TryParse(queryA, out parsedA);
                parseFailed = parseFailed || !double.TryParse(queryB, out parsedB);

                if (parseFailed)
                    Extender.Debugging.Debug.WriteMessage("Could not parse query. Are you trying to do a range search on strings?", "warn");

                switch (SelectedSearchType)
                {
                    case SearchType.Length:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length >= parsedA && ft.Length <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Mass:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass >= parsedA && ft.Mass <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Pitch:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch >= parsedA && ft.Pitch <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Price:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price >= parsedA && ft.Price <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Size:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size >= parsedA && ft.Size <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Quantity:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity >= parsedA && ft.Quantity <= parsedB)
                                                                  .Select(ft => new FastenerControl(ft)));
                        break;
                    default:
                        string warn = @"Operator "":"" cannot be applied to string searches.";
                        Extender.Debugging.Debug.WriteMessage(warn, "warn");
                        MessageBox.Show
                        (
                            warn,
                            "",
                            MessageBoxButton.OK
                        );
                        //SearchQuery = query;
                        //ExecuteSearch();
                        return;
                }
                #endregion

            }
            else
            {
                #region // Regular search

                switch (SelectedSearchType)
                {
                    case SearchType.FastenerType:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Type.Contains(SearchQuery))
                                                        .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Material:
                        queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Material.Contains(SearchQuery))
                                                        .Select(ft => new FastenerControl(ft)));
                        break;
                    case SearchType.Length:
                        float parsed;
                        if (float.TryParse(SearchQuery, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Length.Equals(parsed))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Mass:
                        if (float.TryParse(SearchQuery, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Mass.Equals(parsed))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Pitch:
                        if (float.TryParse(SearchQuery, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Pitch.Equals(parsed))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Price:
                        if (float.TryParse(SearchQuery, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Price.Equals(parsed))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Size:
                        if (float.TryParse(SearchQuery, out parsed))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Size.Equals(parsed))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Quantity:
                        int parsedInt;
                        if (int.TryParse(SearchQuery, out parsedInt))
                        {
                            queryResults.AddRange(_Inventory.Fasteners.Where(ft => ft.Quantity.Equals(parsedInt))
                                                            .Select(ft => new FastenerControl(ft)));
                        }
                        break;
                    case SearchType.Unit:
                        var lq = ((Units[])Enum.GetValues(typeof(Units)))
                                               .FirstOrDefault
                                               (
                                                    unit => Enum.GetName(typeof(Units), unit)
                                                                .ToLower()
                                                                .Contains(SearchQuery)
                                               );

                        queryResults.AddRange(_Inventory.Fasteners.Where(f => f.Unit == lq)
                                                        .Select(ft => new FastenerControl(ft)));
                        break;
                }

                #endregion
            }

            queryResults.ForEach(AddToFastenerList);
            SortFastenerListBy(_LastSearchSelector, _LastSortBy, false); // re-apply the last sort
        }

        private void HandleShortcut(ShortcutEventHandler handler)
        {
            handler?.Invoke();
        }

        private List<FastenerControl> CreateDummies()
        {
            List<FastenerControl> dummies = new List<FastenerControl>();

            for(int i = 0; i < 10; i++)
            {
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        4,
                        0.25f,
                        Material.Steel,
                        FastenerType.SocketHeadFlatScrew,
                        new PlateInfo(4, HoleType.CBore, Units.Millimeters)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        6,
                        0.25f,
                        Material.Steel,
                        FastenerType.FlatCountersunkHeadCapScrew,
                        new PlateInfo(4, HoleType.CSink, Units.Millimeters)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        8,
                        0.5f,
                        Material.Steel,
                        FastenerType.LowHeadSocketHeadCapScrew,
                        new PlateInfo(4, HoleType.Straight, Units.Millimeters)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        10,
                        0.5f,
                        Material.Steel,
                        FastenerType.SocketHeadFlatScrew,
                        new PlateInfo(4, HoleType.CBore, Units.Millimeters)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        8,
                        0.75f,
                        Material.Steel,
                        FastenerType.SocketHeadFlatScrew,
                        new PlateInfo(4, HoleType.CBore, Units.Millimeters)
                    )));
            }

            return dummies;
        }
    }
}