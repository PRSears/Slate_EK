using Slate_EK.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for NewBomWindow.xaml
    /// </summary>
    public partial class MainView : Window
    {
        private MainViewModel ViewModel
        {
            get
            {
                if (DataContext is MainViewModel)
                    return (MainViewModel)DataContext;
                else
                    return null;
            }
            set
            {
                DataContext = value;
            }
        }

        public MainView()
        {
            InitializeComponent();

            ViewModel = new MainViewModel();
            ViewModel.RegisterCloseAction(() => this.Close());

            //TODO Fix MenuItem showing out-of-date information when a BOM's Assembly# changes

            ViewModel.WindowManager.WindowOpened += (s, w) =>
            {
                MenuItem newWindow = new MenuItem();
                newWindow.Header   = w.Title;
                newWindow.Command  = new Extender.WPF.RelayCommand
                (
                    () => w.Focus()
                );

                WindowsMenu.Items.Add(newWindow);
            };

            ViewModel.WindowManager.WindowClosed += (s, w) =>
            {
                Queue<MenuItem> removalQueue = new Queue<MenuItem>();

                foreach(var item in WindowsMenu.Items.SourceCollection)
                { 
                    if(item is MenuItem)
                    {
                        string header = ((item as MenuItem).Header as string);
                        if(ViewModel.WindowManager.Children.Count( (c) => c.Title.Equals(header)) < 1)
                        {
                            removalQueue.Enqueue(item as MenuItem);
                        }
                    }
                }

                foreach(var item in removalQueue) // so we don't modify the collection while it's being iterated
                {
                    WindowsMenu.Items.Remove(item);
                }
            };
        }
    }
}
