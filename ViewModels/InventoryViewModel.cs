using Extender;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public enum UnitType   : byte { Metric, Imperial }
    public enum SortMethod        { None, Quantity, Price, Mass, Size, Length, Pitch, Material, FastType }

    public sealed class InventoryViewModel : ViewModel
    {
        #region // ICommands

        //
        // InventoryBoxContextMenu commands
        public ICommand AddNewFastenerCommand    { get; private set; }
        public ICommand RemoveFastenerCommand    { get; private set; }
        public ICommand DuplicateFastenerCommand { get; private set; }
        public ICommand SelectAllCommand         { get; private set; }
        public ICommand SelectNoneCommand        { get; private set; }
        public ICommand TestHarnessCommand       { get; private set; }

        //
        // Main menu commands
        public ICommand OpenCommand                 { get; private set; }
        public ICommand ExportCommand               { get; private set; }
        public ICommand OpenInExcelCommand          { get; private set; }
        public ICommand ImportCommand               { get; private set; }
        public ICommand ShowAllFastenersCommand     { get; private set; }
        public ICommand ClearQueryResultsCommand    { get; private set; }
        public ICommand DropDatabaseCommand         { get; private set; }
        public ICommand SwitchToMetricCommand       { get; private set; }
        public ICommand SwitchToImperialCommand     { get; private set; }

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

        //
        // Lower controls
        public ICommand SubmitChangesCommand    { get; private set; }
        public ICommand DiscardChangesCommand   { get; private set; }
        public ICommand ExecuteSearchCommand    { get; private set; }

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

        private UnitType               _CurrentUnit;
        private SortMethod             _LastSortBy;
        private bool                   _OrderByDescending;
        private bool                   _PendingOperations;
        private bool                   _EditMode;
        private Inventory              _Inventory;
        private Queue<UnifiedFastener> _PendingFasteners;
        private List<UnifiedFastener>  _FastenersMarkedForRemoval;
        private string                 _WindowTitle = "Inventory Viewer";

        public string   WindowTitle
        {
            get
            {
                return _WindowTitle;
            }
            private set
            {
                _WindowTitle = value;
                OnPropertyChanged(nameof(WindowTitle));
            }
        }
        public string   SearchQuery
        {
            get;
            set;
        }
        public UnitType CurrentUnit
        {
            get
            {
                return _CurrentUnit;
            }
            set
            {
                _CurrentUnit = value;
                OnPropertyChanged(nameof(CurrentUnit));
                OnPropertyChanged(nameof(UsingMetric));
                OnPropertyChanged(nameof(UsingImperial));
            }
        }
        public bool     UsingMetric   => CurrentUnit == UnitType.Metric;
        public bool     UsingImperial => CurrentUnit == UnitType.Imperial;
        public enum     SearchType : byte
        {
            Quantity,
            Price,
            Mass,
            Size,
            Length,
            Pitch,
            Material,
            FastenerType
        }
        public string[] SearchByPropertyList => new[]
        {
            "Quantity",
            "Price",
            "Mass",
            "Size",
            "Length",
            "Pitch",
            "Material",
            "Fastener Type",
            "Hole Type"
        };
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
        public SearchType SelectedSearchType => (SearchType)Array.IndexOf(SearchByPropertyList, SelectedSearchProperty);
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
        public string     SubmitImageSource  => PendingOperations ? SubmitImageSourceStrings[1]  : SubmitImageSourceStrings[0];
        public string     DiscardImageSource => PendingOperations ? DiscardImageSourceStrings[1] : DiscardImageSourceStrings[0];
        public Visibility EditModeVisibility => EditMode ? Visibility.Visible : Visibility.Hidden;
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
                    UnifiedFastener empty = new UnifiedFastener();
                    _PendingFasteners.Enqueue(empty);

                    if (!EditMode)
                        FastenerList.Clear();

                    AddToFastenerList(new FastenerControl(empty));

                    PendingOperations = true;
                    EnterEditMode(false);
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
                }
            );

            DuplicateFastenerCommand = new RelayCommand
            (
                () =>
                {

                    PendingOperations = true;
                }
            );

            SelectAllCommand = new RelayCommand
            (
                () =>
                {
                    FastenerList.ForEach(f => f.IsSelected = true);
                }
            );

            SelectNoneCommand = new RelayCommand
            (
                () =>
                {
                    FastenerList.ForEach(f => f.IsSelected = false);
                }
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

                }
            );

            ExportCommand = new RelayCommand
            (
                () =>
                {

                }
            );

            OpenInExcelCommand = new RelayCommand
            (
                () =>
                {

                }
            );

            ImportCommand = new RelayCommand
            (
                () =>
                {

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
                    _Inventory.DEBUG_DropDatabase();
                }
            );

            SwitchToMetricCommand = new RelayCommand
            (
                () =>
                {

                }
            );

            SwitchToImperialCommand = new RelayCommand
            (
                () =>
                {

                }
            );

            #endregion

            #region // SortBy Commands

            SortByQuantityCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Quantity)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Quantity;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Quantity)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Quantity)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByPriceCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Price)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Price;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Price)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Price)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByMassCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Mass)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Mass;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Mass)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Mass)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortBySizeCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Size)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Size;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Size)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Size)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByLengthCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Length)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Length;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Length)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Length)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByPitchCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Pitch)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Pitch;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Pitch)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Pitch)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByMaterialCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.Material)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.Material;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Material)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Material)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
            );

            SortByFastTypeCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.FastType)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.FastType;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.Type)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.Type)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) AddToFastenerList(item);
                }
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
                    //TODOh Figure out why adding default fastener -> submitting -> removing -> adding -> and submitting
                    //      again throws an exception
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

            #endregion
        }

        //TODO  move drop database command from the tools menu somewhere less accessible, like the 
        //      settings panel perhaps. -- there is no settings panel for the Inventory Viewer itself,
        //      so that might not work. 

        //TODO_ Hook up the rest of the main menu buttons

        //THOUGHT Maybe add / remove / submit shouldn't be anonymous methods. Could split them into 
        //        full functions to make keeping track of pending changes clearer / less code duplication.

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
                // THOUGHT Do I need to check which property changed here?
                //         If we're in edit mode then it will already be in the list,
                //         so it's not doing anything unnecessary.
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
                }

                #endregion
            }

            queryResults.ForEach(AddToFastenerList);
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
                        new PlateInfo(4, HoleType.CBore)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        6,
                        0.25f,
                        Material.Steel,
                        FastenerType.FlatCountersunkHeadCapScrew,
                        new PlateInfo(4, HoleType.CSink)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        8,
                        0.5f,
                        Material.Steel,
                        FastenerType.LowHeadSocketHeadCapScrew,
                        new PlateInfo(4, HoleType.Straight)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        10,
                        0.5f,
                        Material.Steel,
                        FastenerType.SocketHeadFlatScrew,
                        new PlateInfo(4, HoleType.CBore)
                    )));
                dummies.Add(new FastenerControl(new UnifiedFastener
                    (
                        8,
                        0.75f,
                        Material.Steel,
                        FastenerType.SocketHeadFlatScrew,
                        new PlateInfo(4, HoleType.CBore)
                    )));
            }

            return dummies;
        }
    }
}
