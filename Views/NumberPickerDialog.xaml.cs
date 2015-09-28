using Extender.WPF;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for NumberPickerDialog.xaml
    /// </summary>
    public partial class NumberPickerDialog : Window
    {
        public int Value
        {
            get;
            set;
        }

        public bool Success
        {
            get;
            set;
        }

        public ICommand DoneCommand                 { get; private set; }
        public ICommand ExitWithoutSavingCommand    { get; private set; }

        public NumberPickerDialog()
        {
            InitializeComponent();

            DataContext = this;

            DoneCommand = new RelayCommand
            (
                () =>
                {
                    this.Close();
                    this.Success = true;
                }
            );

            ExitWithoutSavingCommand = new RelayCommand
            (
                () =>
                {
                    this.Close();
                    this.Value      = 0;
                    this.Success    = false;
                }
            );

            ValueField.Focus();
        }

        public NumberPickerDialog(int initialValue) : this()
        {
            ExitWithoutSavingCommand = new RelayCommand
            (
                () =>
                {
                    this.Close();
                    this.Value      = initialValue;
                    this.Success    = false;
                }
            );

            Value = initialValue;
        }
    }
}
