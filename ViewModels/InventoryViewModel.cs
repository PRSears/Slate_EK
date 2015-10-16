using Extender;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
using Slate_EK.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public enum SortMethod { None, Quantity, Price, Mass, Size, Length, Pitch, Material, FastType, Unit }

    public sealed class InventoryViewModel : ViewModel
    {
        #region // ICommands

        //
        // InventoryBoxContextMenu commands
        public ICommand AddNewFastenerCommand    { get; private set; }
        public ICommand RemoveFastenerCommand    { get; private set; }
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
        private string                 _WindowTitle = "Inventory Viewer";
        private string                 _SearchQuery;


        private bool Debug => Properties.Settings.Default.Debug;

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
            get { return _SearchQuery; }
            set
            {
                _SearchQuery = value;
                OnPropertyChanged(nameof(SearchQuery));
            }
        }
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
        public Visibility DebugModeVisibilty     => Debug ? Visibility.Visible : Visibility.Hidden;
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

            SelectAllCommand = new RelayCommand
            (
                () => FastenerList.ForEach(f => f.IsSelected = true)
            );

            SelectNoneCommand = new RelayCommand
            (
                () => FastenerList.ForEach(f => f.IsSelected = false)
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
                    _Inventory.DEBUG_DropDatabase();
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

        //TODO Hook up the rest of the main menu buttons

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
            SortFastenerListBy(_LastSearchSelector, _LastSortBy, false); // re-apply the last sort
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
