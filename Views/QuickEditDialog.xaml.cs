using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.ViewModels;
using System.Windows;

namespace Slate_EK.Views
{
    /// <summary>
    /// Interaction logic for QuickEditDialog.xaml
    /// </summary>
    public partial class QuickEditDialog : Window
    {
        private QuickEditViewModel ViewModel
        {
            get
            {
                return DataContext as QuickEditViewModel;
            }
            set
            {
                DataContext = value;
            }
        }
        
        public UnifiedFastener Editing
        {
            get
            {
                return ViewModel.WorkingFastener; 
            }
            set
            {
                ViewModel.WorkingFastener = value;
            }
        }
        public QuickEditDialogResult Result { get; set; }
        
        public QuickEditDialog(UnifiedFastener editFastener)
        {
            InitializeComponent();

            Activated += (sender, args) => QtyBox.Focus();
            ViewModel  = new QuickEditViewModel(editFastener);

            ViewModel.SubmitCommand = new RelayCommand
            (
                () =>
                {
                    Close();
                    Result = QuickEditDialogResult.Submit;
                }
            );

            ViewModel.DiscardCommand = new RelayCommand
            (
                () =>
                {
                    Close();
                    Result = QuickEditDialogResult.Discard;
                }
            );
        }
    }

    public enum QuickEditDialogResult { Discard, Submit }
}
