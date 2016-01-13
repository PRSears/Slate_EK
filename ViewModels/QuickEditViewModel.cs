using Extender.WPF;
using Slate_EK.Models;
using System;
using System.Linq;
using System.Windows.Input;

namespace Slate_EK.ViewModels
{
    public class QuickEditViewModel : ViewModel
    {
        private UnifiedFastener _Fastener;
        private bool            _OverrideLength;

        public ICommand SubmitCommand  { get; set; }
        public ICommand DiscardCommand { get; set; }

        public Units[]    UnitsList           => (Units[])Enum.GetValues(typeof(Units));
        public Material[] MaterialsList       => Material.Materials.Where(m => !m.Equals(Material.Unspecified)).ToArray();
        public HoleType[] HoleTypesList       => HoleType.HoleTypes;
        public string[]   FastenerTypesList   => FastenerType.Types.Select(t => $"{t.Callout} ({t.Type})").ToArray();
        public string[]   SizeOptionsList     { get; private set; }
        public string[]   PitchOptionsList    { get; private set; }
        

        public UnifiedFastener WorkingFastener
        {
            get
            {
                return _Fastener;
            }
            set
            {
                _Fastener = value;
                OnPropertyChanged(nameof(WorkingFastener));
            }
        }

        public string ShortDescription => WorkingFastener.Description;

        public bool OverrideLength
        {
            get { return _OverrideLength; }
            set
            {
                _OverrideLength = value;
                OnPropertyChanged(nameof(OverrideLength));
            }
        }

        public string WindowTitle => $"Quick Edit: {WorkingFastener.Description}";
        public string QtyDisplay  => $"Qty: {WorkingFastener.Quantity}";

        public QuickEditViewModel(UnifiedFastener fastener)
        {
            base.Initialize();

            WorkingFastener = fastener;
            WorkingFastener.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(UnifiedFastener.Length)))
                {
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(ShortDescription));
                }
            };
        }
    }
}
