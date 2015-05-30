using Extender.ObjectUtils;
using System;
using System.ComponentModel;
using System.Text;

namespace Slate_EK.Models
{
    public class Fastener : INotifyPropertyChanged
    {
        [System.Xml.Serialization.XmlIgnore]
        public string AssemblyNumber
        {
            get
            {
                return _AssemblyNumber;
            }
            set
            {
                _AssemblyNumber = value;
                OnPropertyChanged("AssemblyNumber");
            }
        }

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
        private string      _AssemblyNumber;
        private int         _Quantity;
        private Material    _Material;
        #endregion

        public Fastener()
        {

        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fastener))
                return false;

            Fastener b = (Fastener)obj;

            return  this.Family.Equals(b.Family)        &&
                    this.Material.Equals(b.Material)    &&
                    this.Pitch.Equals(b.Pitch)          &&
                    this.Thickness.Equals(b.Thickness)  &&
                    this.Length.Equals(b.Length)        &&
                    this.Mass.Equals(b.Mass);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(this.GetHashData(), 0);
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[6][];

            blocks[0] = Encoding.Default.GetBytes(this.Family);
            blocks[1] = Encoding.Default.GetBytes(this.Material.ToString());
            blocks[2] = BitConverter.GetBytes(this.Pitch);
            blocks[2] = BitConverter.GetBytes(this.Thickness);
            blocks[2] = BitConverter.GetBytes(this.Length);
            blocks[2] = BitConverter.GetBytes(this.Mass);

            return Hashing.GenerateHashCode(blocks);
        }

        public override string ToString()
        {
            return base.ToString();
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
