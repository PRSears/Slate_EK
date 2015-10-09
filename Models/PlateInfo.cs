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
                return _Thickness;
            }
            set
            {
                _Thickness = value;
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

        public string HoleTypeDisplay
        {
            get { return _HoleType.ToString(); }
            set
            {
                HoleType = HoleType.TryParse(value);
                OnPropertyChanged(nameof(HoleTypeDisplay));
            }
        }

        public PlateInfo()
        {
            HoleType = HoleType.Unspecified;
        }

        public PlateInfo(float thickness, string holeType)
        {
            Thickness       = thickness;
            HoleTypeDisplay = holeType;
        }

        public PlateInfo(float thickness, HoleType holeType)
        {
            Thickness = thickness;
            HoleType  = holeType;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
