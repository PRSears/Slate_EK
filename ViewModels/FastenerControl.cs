using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.Inventory;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media;

namespace Slate_EK.ViewModels
{
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
                OnPropertyChanged(nameof(IsSelected));
                OnPropertyChanged(nameof(Background));
                OnPropertyChanged(nameof(AltBackground));
            }
        }

        public bool IsEditable
        {
            get
            {
                return _IsEditable;
            }
            set
            {
                _IsEditable = value;
                OnPropertyChanged(nameof(IsEditable));
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
        private bool _IsEditable;
        #endregion

        public ICommand SelectCommand         { get; private set; }
        public ICommand DeselectCommand       { get; private set; }
        public ICommand ToggleSelectCommand   { get; private set; }
        public ICommand EditCommand           { get; private set; }
        public ICommand RequestRemoval        { get; private set; }
        public ICommand RequestQuantityChange { get; private set; }

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
            Fastener = fastener;
        }

        public FastenerControl(FastenerTableLayer fastener)
            : this()
        {
            Fastener = (Models.Fastener)fastener;
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
            EditControlEventHandler handler = EditingControl;

            handler?.Invoke(sender);
        }

        #region Settings.Settings aliases
        protected string SelectedColor => Properties.Settings.Default.ItemSelectedBackgroundColor;
        protected string HoverColor    => Properties.Settings.Default.ItemHoverBackgroundColor;
        protected string NormalColor   => Properties.Settings.Default.ItemDefaultBackgroundColor;
        protected string AltColor      => Properties.Settings.Default.ItemAltnernateBackgroundColor;
        public int BomFontSize         => Properties.Settings.Default.BomListFontSize;

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
