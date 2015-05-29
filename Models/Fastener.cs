using System.ComponentModel;

namespace Slate_EK.Models
{
    public class Fastener : INotifyPropertyChanged
    {
        public string Family
        {
            get
            {
                return _Family;
            }
            set
            {
                _Family = value;
                OnPropertyChanged("Family");
            }
        }

        public Material Material
        {
            get
            {
                return _Material;
            }
            set
            {
                _Material = value;
                OnPropertyChanged("Material");
            }
        }
        
        public double Pitch
        {
            get
            {
                return _Pitch;
            }
            set
            {
                _Pitch = value;
                OnPropertyChanged("Pitch");
            }
        }

        public double Thickness
        {
            get
            {
                return _Thickness;
            }
            set
            {
                _Thickness = value;
                OnPropertyChanged("Thickness");
            }
        }

        public double Length
        {
            get
            {
                return _Length;
            }
            set
            {
                _Length = value;
                OnPropertyChanged("Length");
            }
        }

        public double Mass
        {
            get
            {
                return _Mass;
            }
            set
            {
                _Mass = value;
                OnPropertyChanged("Mass");
            }
        }

        public float Price
        {
            get
            {
                return _Price;
            }
            set
            {
                _Price = value;
                OnPropertyChanged("Price");
            }
        }

        public int Quantity
        {
            get
            {
                return _Quantity;
            }
            set
            {
                _Quantity = value;
                OnPropertyChanged("Quantity");
            }
        }


        #region Boxed properties
        private float       _Price;
        private double      _Mass;
        private double      _Length;
        private double      _Thickness;
        private double      _Pitch;
        private string      _Family;
        private int         _Quantity;
        private Material    _Material;
        #endregion

        public Fastener()
        {

        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
