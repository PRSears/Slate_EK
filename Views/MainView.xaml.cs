using Slate_EK.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for NewBomWindow.xaml
    /// </summary>
    public partial class MainView
    {
        private MainViewModel ViewModel
        {
            get
            {
                if (DataContext is MainViewModel)
                    return (MainViewModel)DataContext;

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
            ViewModel.RegisterCloseAction(Close);

            ViewModel.WindowManager.WindowOpened += (s, w) =>
            {
                MenuItem newWindowMenuItem = new MenuItem();
                newWindowMenuItem.Header   = w.Title;
                newWindowMenuItem.Command  = new Extender.WPF.RelayCommand
                (
                    () => w.Focus()
                );

                WindowsMenu.Items.Insert(0, newWindowMenuItem);

                w.MouseLeave += (sender, args) => // Make sure the MenuItem stays up-to-date
                {
                    newWindowMenuItem.Header = w.Title;
                };
            };

            ViewModel.WindowManager.WindowClosed += (s, w) =>
            {
                Queue<MenuItem> removalQueue = new Queue<MenuItem>();

                foreach (var item in WindowsMenu.Items.SourceCollection)
                {
                    if (item is MenuItem)
                    {
                        string header = ((item as MenuItem).Header as string);

                        Debug.Assert(header != null, "header != null");
                        if (header.Equals(CloseAllMenuitem.Header)) continue;
                        if (ViewModel.WindowManager.Children.Count(c => c.Title.Equals(header)) < 1)
                        {
                            removalQueue.Enqueue(item as MenuItem);
                        }
                    }
                }

                foreach (var item in removalQueue) // so we don't modify the collection while it's being iterated
                {
                    WindowsMenu.Items.Remove(item);
                }
            };
        }

        public MainView(Window openWith) : this()
        {
            ViewModel.WindowManager.OpenWindow(openWith);
        }

        public Window FindBomWindow(string assemblyNumber)
        {
            Window bomWindow;

            try
            {
                bomWindow = ViewModel.WindowManager.Children.First(w => w.Title.Contains(assemblyNumber));
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }

            return bomWindow;
        }

        private void MainWindow_Drop(object sender, DragEventArgs e)
        {
            var items = e.Data.GetData(DataFormats.FileDrop);

            if (items is string[])
            {
                string[] filenames = (items as string[]);

                foreach (string filename in filenames)
                    ViewModel.FileDroppedCommand.Execute(filename);
            }
        }

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move | DragDropEffects.Link;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            ViewModel.WindowManager.CloseAll();

            base.OnClosing(e);

            if (!e.Cancel)
                Dispatcher.InvokeShutdown();
        }
    }
}
