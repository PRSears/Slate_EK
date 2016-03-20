using Extender;
using Extender.WPF;
using Slate_EK.Models;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Slate_EK.ViewModels
{
    public class FastenerControl : INotifyPropertyChanged
    {
        public UnifiedFastener Fastener
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
                OnPropertyChanged(nameof(HoverBackground));
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
                OnPropertyChanged(nameof(NotEditable));
            }
        }

        public bool IsSelectable
        {
            get { return _IsSelectable; }
            set
            {
                _IsSelectable = value;
                OnPropertyChanged(nameof(IsSelectable));
                OnPropertyChanged(nameof(CheckboxVisibility));
            }
        }

        public string     ToolTip                   => IsToolTipVisible ? Fastener.Description : null;
        public string     Description               => AlignDescription ? Fastener.AlignedDescription : Fastener.Description;
        public bool       NotEditable               => !IsEditable;
        public Visibility CheckboxVisibility        => IsSelectable ? Visibility.Visible   : Visibility.Collapsed;
        public Visibility InverseCheckboxVisibility => IsSelectable ? Visibility.Collapsed : Visibility.Visible;

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

        public SolidColorBrush HoverBackground
        {
            get
            {
                if (IsSelected)
                {
                    var darken = (Color)ColorConverter.ConvertFromString(SelectedColor);
                    darken.R -= 0x1f; // about 12% darker
                    darken.G -= 0x1f;
                    darken.B -= 0x1f;

                    return new SolidColorBrush(darken);
                }
                else
                    return (SolidColorBrush)(new BrushConverter().ConvertFrom(HoverColor));
            }
        }

        #region boxed properties
        private bool _IsSelected;
        private bool _IsEditable;
        private bool _IsSelectable;
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

        public FastenerControl(UnifiedFastener fastener)
            : this()
        {
            // Make a copy so we don't bind UI elements to cached data from the db.
            Fastener = fastener.Copy(); 
        }

        /// <summary>
        /// Creates an array of FastenerControls from an array of plain Fastener objects.
        /// </summary>
        public static FastenerControl[] FromArray(UnifiedFastener[] fasteners)
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

        private bool     IsToolTipVisible => Properties.Settings.Default.ShowInvFastenerToolTip;
        protected string SelectedColor    => Properties.Settings.Default.ItemSelectedBackgroundColor;
        protected string HoverColor       => Properties.Settings.Default.ItemHoverBackgroundColor;
        protected string NormalColor      => Properties.Settings.Default.ItemDefaultBackgroundColor;
        protected string AltColor         => Properties.Settings.Default.ItemAltnernateBackgroundColor;
        public int       BomFontSize      => Properties.Settings.Default.BomListFontSize;
        public bool      AlignDescription => Properties.Settings.Default.AlignDescriptionsBom;

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
