using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Slate_EK.Models
{
    public class PlateInfo : INotifyPropertyChanged
    {
        private float    _Thickness;
        private HoleType _HoleType;

        public event  PropertyChangedEventHandler PropertyChanged;

        public float Thickness
        {
            get
            {
                switch (Unit)
                {
                    case Units.Millimeters:
                        return _Thickness;
                    case Units.Inches:
                        return (float)Measure.Convert<Millimeter, Inch>(_Thickness);
                    default:
                        return _Thickness; // default to SI if something fucks up
                }
            }
            set
            {
                switch (Unit)
                {
                    case Units.Millimeters:
                        _Thickness = value;
                        break;
                    case Units.Inches:
                        _Thickness = (float)Measure.Convert<Inch, Millimeter>(value);
                        break;
                    default:
                        _Thickness = value;
                        break;
                }

                OnPropertyChanged(nameof(Thickness));
            }
        }
        [XmlIgnore]
        public HoleType HoleType
        {
            get { return _HoleType; }
            set
            {
                _HoleType = value;
                OnPropertyChanged(nameof(HoleType));
                OnPropertyChanged(nameof(HoleTypeDisplay));
            }
        }

        [XmlIgnore]
        public Units Unit
        {
            get; private set;
        }

        public string HoleTypeDisplay
        {
            get { return _HoleType.ToString(); }
            set
            {
                HoleType = HoleType.TryParse(value);
                OnPropertyChanged(nameof(HoleTypeDisplay));
            }
        }

        public PlateInfo(Units unit)
        {
            Unit     = unit;
            HoleType = HoleType.Unspecified;
        }

        public PlateInfo(float thickness, string holeType, Units unit)
        {
            Unit            = unit;         // Make sure units get set first so we know wtf to set thickness as
            Thickness       = thickness;
            HoleTypeDisplay = holeType;
        }

        public PlateInfo(float thickness, HoleType holeType, Units unit)
        {
            Unit      = unit;               // Make sure units get set first so we know wtf to set thickness as
            Thickness = thickness;
            HoleType  = holeType;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
