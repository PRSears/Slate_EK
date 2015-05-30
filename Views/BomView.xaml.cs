using Extender.WPF;
using Slate_EK.ViewModels;
using System.Windows;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class BomView : Window
    {
        private BomViewModel ViewModel
        {
            get
            {
                if (DataContext is BomViewModel)
                    return (BomViewModel)DataContext;
                else
                    return null;
            }
            set
            {
                DataContext = value;
            }
        }

        public BomView() : this(string.Empty)
        { }

        public BomView(string assemblyNumber)
        {
            InitializeComponent();

            ViewModel = new BomViewModel(assemblyNumber);
            
            ViewModel.RegisterCloseAction(() => this.Close());
            ViewModel.PropertyChanged += (s, e) =>
            {
                if(e.PropertyName.Equals("OverrideLength"))
                {
                    if (ViewModel.OverrideLength)
                        this.LengthTextbox.Opacity = 1d;
                    else
                        this.LengthTextbox.Opacity = 0.35;
                }
            };
            ViewModel.ShortcutPressed_CtrlK += () => this.AssemblyNumberField.Focus();
            ViewModel.OverrideLength = false;            
        }
    }
}
