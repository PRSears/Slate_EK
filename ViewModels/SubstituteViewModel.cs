using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public sealed class SubstituteViewModel : ViewModel
    {
        public ICommand SubmitCommand        { get; private set; }
        public ICommand DiscardCommand       { get; private set; }
        public ICommand OpenInventoryCommand { get; private set; }

        public ObservableCollection<FastenerControl> Candidates { get; }

        public UnifiedFastener SelectedFastener { get; private set; }
        public Modes           Mode             { get; private set; }
        public string          WindowTitle      { get; private set; }

        public Visibility SelectCancelButtonVisibility => Mode == Modes.Substitutes  ? Visibility.Visible : Visibility.Collapsed;
        public Visibility DoneButtonVisibility         => Mode == Modes.LowStockList ? Visibility.Visible : Visibility.Collapsed;

        public SubstituteViewModel(IQueryable<UnifiedFastener> candidates, Modes mode = Modes.Substitutes)
        {
            Mode       = mode;
            Candidates = new ObservableCollection<FastenerControl>(FastenerControl.FromArray(candidates.ToArray()));

            switch (mode)
            {
                case Modes.Substitutes:
                    WindowTitle = "Select a substitute";
                    break;
                case Modes.LowStockList:
                    WindowTitle = "The following have low stock:";
                    break;
            }

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

            OpenInventoryCommand = new RelayCommand
            (
                () =>
                {
                    throw new NotImplementedException();
                }
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

        public enum Modes { Substitutes, LowStockList}
    }

}
