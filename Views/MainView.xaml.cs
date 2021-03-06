﻿using Extender.Debugging;
using Extender.WPF;
using Microsoft.Win32;
using Slate_EK.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Debug = System.Diagnostics.Debug;

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
                newWindowMenuItem.Command  = new RelayCommand
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

            CheckSqlServer();
            CheckDefaultInv();
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

        public Window FindInventoryWindow()
        {
            Window invWindow;

            try
            {
                invWindow = ViewModel.WindowManager.Children.First(w => w.Title.Contains("nventory")); // Lazy, I know. I blame SCAR.
            }
            catch (System.InvalidOperationException)
            {
                return null;
            }

            return invWindow;
        }

        private void CheckSqlServer()
        {
            RegistryView registryView = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Registry32;

            using (RegistryKey hklm = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, registryView))
            {
                RegistryKey instanceKey = hklm.OpenSubKey(@"SOFTWARE\Microsoft\Microsoft SQL Server Local DB\Installed Versions", false);

                if (instanceKey == null)
                {
                    Extender.Debugging.Debug.WriteMessage($"SQL NOT found.", WarnLevel.Error);
                    var result = MessageBox.Show
                    (
                        "This application won't function correctly unless SQL LocalDB v11 (2012) is installed.",
                        "SQL Server Not Found",
                        MessageBoxButton.OK,
                        MessageBoxImage.Exclamation
                    );
                }
                else
                {
                    foreach (var installedVersion in instanceKey.GetSubKeyNames())
                    {
                        Extender.Debugging.Debug.WriteMessage($"SQL Found: {Environment.MachineName}\\{installedVersion}", WarnLevel.Info);
                    }
                }
            }
        }

        private void CheckDefaultInv()
        {
            if (!DefaultInventoryPath.StartsWith(@"choose a path")) return;

            //
            // If we don't have a path set, force the user to pick one

            var mBoxResult = MessageBox.Show
            (
                "It looks like this is the first run of this application, or " +
                "the settings file may have been reset.\n\n" +
                "Press 'Yes' to select an inventory database file or to create a new one.\n" +
                "Press 'No' to use the default at '" + CreateInvPath() + "'.",
                "Browse for an inventory file?",
                MessageBoxButton.YesNo,
                MessageBoxImage.Exclamation
            );

            if (mBoxResult == MessageBoxResult.Yes)
            {

                var dialog = new Microsoft.Win32.OpenFileDialog()
                {
                    AddExtension = true,
                    DefaultExt = "mdf",
                    CheckFileExists = false,
                    CheckPathExists = false,
                    InitialDirectory = System.IO.Directory.GetCurrentDirectory()
                };

                var dialogResult = dialog.ShowDialog();
                if (!dialogResult.HasValue || !dialogResult.Value)
                    DefaultInventoryPath = CreateInvPath();

                DefaultInventoryPath = dialog.FileName;
            }
            else
            {
                DefaultInventoryPath = CreateInvPath();
            }
        }

        private string CreateInvPath()
        {
            return $"{System.IO.Directory.GetCurrentDirectory()}\\inventory.mdf";
        }

        private string DefaultInventoryPath
        {
            get { return Properties.Settings.Default.DefaultInventoryPath; }
            set
            {
                Properties.Settings.Default.DefaultInventoryPath = value;
                Properties.Settings.Default.Save();
            }
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
            if (Properties.Settings.Default.ConfirmClose)
            {
                e.Cancel = !ConfirmationDialog.Show("Confirm exit", "Are you sure you want to close the application?");
            }

            if (!e.Cancel) ViewModel.WindowManager.CloseAll();
            base.OnClosing(e);
            if (!e.Cancel) Dispatcher.InvokeShutdown();
        }
    }
}
