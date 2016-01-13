using Extender.WPF;
using Slate_EK.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public sealed class SubstituteViewModel : ViewModel
    {
        public ObservableCollection<FastenerControl> Candidates { get; }
        public string WindowTitle => "Select a substitute";

        public ICommand SubmitCommand  { get; private set; }
        public ICommand DiscardCommand { get; private set; }

        public UnifiedFastener SelectedFastener { get; private set; }

        public SubstituteViewModel(IQueryable<UnifiedFastener> candidates)
        {
            Candidates = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(candidates.ToArray()));

            SubmitCommand = new RelayCommand
            (
                () =>
                {
                    SelectedFastener = Candidates.FirstOrDefault(fc => fc.IsSelected)?
                                                 .Fastener;

                    CloseCommand.Execute(null);
                },
                () => Candidates.Any(fc => fc.IsSelected)
            );

            DiscardCommand = new RelayCommand
            (
                () =>
                {
                    SelectedFastener = null;
                    CloseCommand.Execute(null);
                }
            );
        }
    }
}
