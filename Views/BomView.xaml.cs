using Extender.WPF;
using Slate_EK.ViewModels;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

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
                if (e.PropertyName.Equals("OverrideLength"))
                {
                    if (ViewModel.OverrideLength)
                        this.LengthTextbox.Opacity = 1d;
                    else
                        this.LengthTextbox.Opacity = 0.35;
                }
                else if (e.PropertyName.Equals("ObservableFasteners"))
                {
                    this.FastenerItemsControl.ItemsSource = ViewModel.ObservableFasteners;
                }
            };
            ViewModel.ShortcutPressed_CtrlK += () => this.AssemblyNumberField.Focus();
            ViewModel.OverrideLength = false;

            this.FastenerItemsControl.ItemsSource = ViewModel.ObservableFasteners;
        }

        private bool SelectingFasteners = false;
        private List<FastenerControl> JustSelected = new List<FastenerControl>();

        private void FastenersPanel_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var origin = (FrameworkElement)e.OriginalSource;

            if(origin.DataContext is FastenerControl)
            {
                SelectFastener(origin.DataContext as FastenerControl);
                JustSelected.Add(origin.DataContext as FastenerControl);
                SelectingFasteners = true;
            }
        }

        private void FastenersPanel_LeftMouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            SelectingFasteners = false;
            JustSelected = new List<FastenerControl>();
        }

        private void FastenersPanel_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton != System.Windows.Input.MouseButtonState.Pressed)
                return;

            var origin = (FrameworkElement)e.OriginalSource;

            if (origin.DataContext is FastenerControl && 
                SelectingFasteners && 
                !JustSelected.Contains(origin.DataContext as FastenerControl))
            {
                SelectFastener(origin.DataContext as FastenerControl);
                JustSelected.Add(origin.DataContext as FastenerControl);
            }
        }

        private void SelectFastener(FastenerControl c)
        {
            if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                c.SelectCommand.Execute(null);
            else if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
                c.DeselectCommand.Execute(null);
            else
                c.ToggleSelectCommand.Execute(null);
        }
    }
}
