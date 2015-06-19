using Extender.WPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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

        public ICommand DoneCommand                 { get; private set; }
        public ICommand ExitWithoutSavingCommand    { get; private set; }

        public NumberPickerDialog()
        {
            InitializeComponent();

            DataContext = this;

            DoneCommand = new RelayCommand
            (
                () => this.Close()
            );

            ExitWithoutSavingCommand = new RelayCommand
            (
                () =>
                {
                    this.Close();
                    this.Value = 0;
                }
            );

            ValueField.Focus();
        }
    }
}
