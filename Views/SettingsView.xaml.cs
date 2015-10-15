using Slate_EK.ViewModels;
using System.Windows;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : Window
    {
        private SettingsViewModel ViewModel
        {
            get
            {
                if (DataContext is SettingsViewModel)
                    return (SettingsViewModel)DataContext;

                return null;
            }
            set { DataContext = value; }
        }

        public SettingsView()
        {
            InitializeComponent();

            ViewModel = new SettingsViewModel();
        }
    }
}
