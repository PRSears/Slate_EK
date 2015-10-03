using Extender.WPF;
using Slate_EK.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public enum UnitType   : byte { Metric, Imperial }
    public enum SortMethod : byte { None, Quantity, Price, Mass, Size, Length, Pitch, Material, FastType, HoleType }

    public class InventoryViewModel : ViewModel
    {
        #region // ICommands

        //
        // InventoryBoxContextMenu commands
        public ICommand AddNewFastenerCommand   { get; private set; }
        public ICommand RemoveFastenerCommand   { get; private set; }
        public ICommand ChangeQuantityCommand   { get; private set; }
        public ICommand SelectAllCommand        { get; private set; }
        public ICommand SelectNoneCommand       { get; private set; }

        //
        // Main menu commands
        public ICommand OpenCommand                 { get; private set; }
        public ICommand ExportCommand               { get; private set; }
        public ICommand ExitCommand                 { get; private set; }
        public ICommand OpenInExcelCommand          { get; private set; }
        public ICommand ImportCommand               { get; private set; }
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
        public ICommand SortByHoleTypeCommand   { get; private set; }

        //
        // Lower controls
        public ICommand RemoveItemCommand       { get; private set; }
        public ICommand AddItemCommand          { get; private set; }
        public ICommand ExecuteSearchCommand    { get; private set; }

        #endregion

        //
        // Bound properties
        private ObservableCollection<FastenerControl> _FastenerList;
        private string          _WindowTitle = "Inventory Viewer";
        private UnitType        _CurrentUnit;
        private SortMethod      _LastSortBy;
        private bool            _OrderByDescending;

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
        public bool     UsingMetric => CurrentUnit == UnitType.Metric;

        public bool     UsingImperial => CurrentUnit == UnitType.Imperial;

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

        public string   SelectedSearchProperty
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
        public bool     OrderByDescending
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
        public InventoryViewModel()
        {
            Initialize();

            // HACK generating dummy fasteners for debug
            FastenerList    = new ObservableCollection<FastenerControl>(CreateDummies());
            _LastSortBy     = SortMethod.None;
        }

        public override void Initialize()
        {
            base.Initialize();

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
                    foreach (var item in sorted) FastenerList.Add(item);
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
                    foreach (var item in sorted) FastenerList.Add(item);
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
                    foreach (var item in sorted) FastenerList.Add(item);
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
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.SizeString)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.SizeString)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) FastenerList.Add(item);
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
                    foreach (var item in sorted) FastenerList.Add(item);
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
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.PitchString)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.PitchString)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) FastenerList.Add(item);
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
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.MaterialString)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.MaterialString)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) FastenerList.Add(item);
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
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.TypeString)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.TypeString)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) FastenerList.Add(item);
                }
            );

            SortByHoleTypeCommand = new RelayCommand
            (
                () =>
                {
                    if (_LastSortBy == SortMethod.HoleType)
                        OrderByDescending = !OrderByDescending;
                    else
                        _LastSortBy = SortMethod.HoleType;

                    FastenerControl[] sorted;
                    if (OrderByDescending)
                    {
                        sorted = _FastenerList.OrderByDescending(f => f.Fastener.HoleTypeString)
                                              .ToArray();
                    }
                    else
                    {
                        sorted = _FastenerList.OrderBy(f => f.Fastener.HoleTypeString)
                                              .ToArray();
                    }

                    FastenerList.Clear();
                    foreach (var item in sorted) FastenerList.Add(item);
                }
            );
            
            #endregion
            
        }

        public List<FastenerControl> CreateDummies()
        {
            List<FastenerControl> dummies = new List<FastenerControl>();

            for(int i = 0; i < 10; i++)
            {
                dummies.Add(new FastenerControl(new Fastener
                    (
                        new Size("M2"),
                        new Pitch(0.25),
                        new Thickness(1),
                        Material.Steel,
                        HoleType.Straight,
                        FastenerType.SocketHeadFlatScrew
                    )));
                dummies.Add(new FastenerControl(new Fastener
                    (
                        new Size("M5"),
                        new Pitch(0.5),
                        new Thickness(1.25),
                        Material.Steel,
                        HoleType.CSink,
                        FastenerType.SocketCountersunkHeadCapScrew
                    )));
                dummies.Add(new FastenerControl(new Fastener
                    (
                        new Size("M4"),
                        new Pitch(0.25),
                        new Thickness(1),
                        Material.Steel,
                        HoleType.Straight,
                        FastenerType.SocketHeadFlatScrew
                    )));
                dummies.Add(new FastenerControl(new Fastener
                    (
                        new Size("M6"),
                        new Pitch(0.75),
                        new Thickness(5),
                        Material.Steel,
                        HoleType.CSink,
                        FastenerType.FlatCountersunkHeadCapScrew
                    )));
                dummies.Add(new FastenerControl(new Fastener
                    (
                        new Size("M8"),
                        new Pitch(1),
                        new Thickness(8),
                        Material.Steel,
                        HoleType.CBore,
                        FastenerType.LowHeadSocketHeadCapScrew
                    )));
            }

            return dummies;
        }
    }
}
