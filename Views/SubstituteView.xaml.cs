using Slate_EK.Models;
using Slate_EK.ViewModels;
using System.Linq;
using System.Windows;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for SubstituteView.xaml
    /// </summary>
    public partial class SubstituteView : Window
    {
        private SubstituteViewModel ViewModel
        {
            get
            {
                return DataContext as SubstituteViewModel;
            }
            set { DataContext = value; }
        }

        public UnifiedFastener SelectedFastener => ViewModel.SelectedFastener;

        public SubstituteView(IQueryable<UnifiedFastener> candidates)
        {
            InitializeComponent();

            ViewModel = new SubstituteViewModel(candidates);
            CandidatesItemsControl.ItemsSource = ViewModel.Candidates;
            ViewModel.RegisterCloseAction(Close);
        }
    }
}
