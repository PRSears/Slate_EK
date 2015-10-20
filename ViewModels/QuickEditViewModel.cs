using Extender;
using Extender.Debugging;
using Extender.WPF;
using Slate_EK.Models;
using Slate_EK.Models.IO;
using Slate_EK.Models.ThreadParameters;
using System;
using System.Linq;
using System.Windows.Input;
using Sizes = Slate_EK.Models.IO.Sizes;

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

        private readonly Sizes         _XmlSizes;
        private readonly ImperialSizes _XmlImperialSizes;
        private readonly Pitches       _XmlPitches;  


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

            //WorkingFastener.PropertyChanged += (sender, args) =>
            //{
            //    if (string.IsNullOrWhiteSpace(args.PropertyName)) return;

            //    if (args.PropertyName.Equals(nameof(UnifiedFastener.Material)) || args.PropertyName.Equals(nameof(UnifiedFastener.Size)))
            //    {
            //        if (!OverrideLength)
            //            WorkingFastener.CalculateLength(true);
            //    }
            //    else if (args.PropertyName.Equals(nameof(UnifiedFastener.Unit)))
            //    {
            //        SetSizesList();
            //    }
            //    else if (WorkingFastener.Unit == Units.Inches && args.PropertyName.Equals(nameof(UnifiedFastener.SizeDisplay)))
            //    {
            //        float? pitchReset = (float?)Measure.Convert<Inch, Millimeter>(
            //            UnifiedThreadStandard.FromMillimeters(WorkingFastener.Size)?.CourseThreadPitch);

            //        WorkingFastener.Pitch = pitchReset ?? 0f;

            //        SetPitchesList();
            //    }
            //};

            //_XmlSizes         = new Sizes();
            //_XmlImperialSizes = new ImperialSizes();
            //_XmlPitches       = new Pitches();
            
            //Task.Run(async () =>
            //{
            //    await _XmlSizes.ReloadAsync();
            //    if (WorkingFastener.Unit == Units.Millimeters)
            //        SetSizesList();
            //});

            //Task.Run(async () =>
            //{
            //    await _XmlImperialSizes.ReloadAsync();
            //    if (WorkingFastener.Unit == Units.Inches)
            //        SetSizesList();
            //});

            //Task.Run(async () =>
            //{
            //    await _XmlPitches.ReloadAsync();
            //    SetPitchesList();
            //});

            //OnPropertyChanged(null); // Indicates that all properties have changed.
        }

        private void SetSizesList()
        {
            switch (WorkingFastener.Unit)
            {
                case Units.Millimeters:
                    SizeOptionsList  = _XmlSizes.SourceList?.Select(s => s.ToString()).ToArray();
                    PitchOptionsList = _XmlPitches.SourceList?.Select(p => p.ToString()).ToArray();
                    break;
                case Units.Inches:
                    SizeOptionsList  = _XmlImperialSizes.SourceList?.Select(i => i.Designation).ToArray();
                    PitchOptionsList = Enum.GetNames(typeof(ThreadDensity));
                    break;
            }

            OnPropertyChanged(nameof(SizeOptionsList));
        }

        private void SetPitchesList()
        {
            switch (WorkingFastener.Unit)
            {
                case Units.Millimeters:
                    PitchOptionsList = _XmlPitches.SourceList?.Select(p => p.ToString()).ToArray();
                    break;
                case Units.Inches:
                    var selectedSize = _XmlImperialSizes.SourceList?
                                                        .FirstOrDefault(s => s.Designation.Equals(WorkingFastener.SizeDisplay));
                    if (selectedSize == null || selectedSize.Equals(default(UnifiedThreadStandard)))
                        return;
                    try
                    { 
                        PitchOptionsList = ((ThreadDensity[])Enum.GetValues(typeof(ThreadDensity)))
                                                                 .Select(thread => selectedSize.GetThreadDensityDisplay(thread))
                                                                 .Where(display => !display.EndsWith(" 0 TPI"))
                                                                 .ToArray();
                    }
                    catch
                    {
                        Debug.WriteMessage("SetLists had to use fallback for Imperial Pitches.");
                        PitchOptionsList = Enum.GetNames(typeof(ThreadDensity));
                    }
                    break;
            }

            OnPropertyChanged(nameof(PitchOptionsList));
        }

        public void SetWorkingFastener(UnifiedFastener fastener)
        {
            WorkingFastener = fastener.Copy();

        }
    }
}
