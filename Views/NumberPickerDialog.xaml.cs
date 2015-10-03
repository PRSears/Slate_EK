using Extender.WPF;
using System.Windows.Input;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for NumberPickerDialog.xaml
    /// </summary>
    public partial class NumberPickerDialog
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
                    Close();
                    Success = true;
                }
            );

            ExitWithoutSavingCommand = new RelayCommand
            (
                () =>
                {
                    Close();
                    Value      = 0;
                    Success    = false;
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
                    Close();
                    Value      = initialValue;
                    Success    = false;
                }
            );

            Value = initialValue;
        }
    }
}
