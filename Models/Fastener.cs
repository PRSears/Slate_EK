using Extender;
using Extender.ObjectUtils;
using System;
using System.ComponentModel;
using System.Linq;
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
                OnPropertyChanged(nameof(AssemblyNumber));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Guid ID
        {
            get
            {
                if (_ID == null || _ID.Equals(Guid.Empty))
                    this._ID = new Guid(GetHashData().Take(16).ToArray()); // first 16 bytes from SHA256 
                return _ID;
            }
            protected set
            {
                _ID = value;
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public FastenerType Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Material Material
        {
            get
            {
                return _Material;
            }
            set
            {
                _Material = value;
                OnPropertyChanged(nameof(Material));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Pitch Pitch
        {
            get
            {
                return _Pitch;
            }
            set
            {
                _Pitch = value;
                OnPropertyChanged(nameof(Pitch));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Size Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
                OnPropertyChanged(nameof(Size));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public Thickness PlateThickness
        {
            get
            {
                return _PlateThickness;
            }
            set
            {
                _PlateThickness = value;
                OnPropertyChanged(nameof(PlateThickness));
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public HoleType HoleType
        {
            get
            {
                return _HoleType;
            }
            set
            {
                _HoleType = value;
                OnPropertyChanged(nameof(HoleType));
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
                OnPropertyChanged(nameof(Length));
            }
        }
        
        public double Price
        {
            get
            {
                return _Price;
            }
            set
            {
                _Price = value;
                OnPropertyChanged(nameof(Price));
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
                OnPropertyChanged(nameof(Mass));
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
                OnPropertyChanged(nameof(Quantity));
            }
        }

        #region string intermediaries 
        public string SizeString
        {
            get
            {
                return this.Size.ToString();
            }
            set
            {
                this.Size = Size.TryParse(value);
                OnPropertyChanged(nameof(SizeString));
            }
        }

        public string PitchString
        {
            get
            {
                return this.Pitch.ToString();
            }
            set
            {
                this.Pitch = Pitch.TryParse(value);
                OnPropertyChanged(nameof(PitchString));
            }
        }

        public string PlateThicknessString
        {
            get
            {
                return this.PlateThickness.ToString();
            }
            set
            {
                this.PlateThickness = Thickness.TryParse(value);
                OnPropertyChanged(nameof(PlateThicknessString));
            }
        }

        public string MaterialString
        {
            get
            {
                return this.Material.ToString();
            }
            set
            {
                this.Material = Material.TryParse(value);
                OnPropertyChanged(nameof(MaterialString));
            }
        }

        public string TypeString
        {
            get
            {
                return this.Type.ToString();
            }
            set
            {
                this.Type = FastenerType.Parse(value);
                OnPropertyChanged(nameof(TypeString));
            }
        }

        public string HoleTypeString
        {
            get
            {
                return this.HoleType.ToString();
            }
            set
            {
                this.HoleType = HoleType.Parse(value);
                OnPropertyChanged(nameof(HoleTypeString));
            }
        }

        public string IdString
        {
            get
            {
                return this.ID.ToString();
            }
            set
            {
                this.ID = new Guid(value);
                OnPropertyChanged(nameof(IdString));
            }
        }
        #endregion

        #region boxed properties
        private string          _AssemblyNumber;
        private double          _Price;
        private double          _Mass;
        private double          _Length;
        private int             _Quantity;
        private Size            _Size;
        private Pitch           _Pitch;       
        private Thickness       _PlateThickness;
        private Material        _Material;
        private HoleType        _HoleType;
        private FastenerType    _Type;
        private Guid            _ID;
        #endregion

        public Fastener(
            string assemblyNumber,
            Size size,
            Pitch pitch,
            Thickness plateThickness,
            Material material,
            HoleType holeType,
            FastenerType type)
        {
            this.AssemblyNumber = assemblyNumber;
            this.Size           = size;
            this.Pitch          = pitch;
            this.PlateThickness = plateThickness;
            this.Material       = material;
            this.HoleType       = holeType;
            this.Type           = type;

            this.Price          = 0d;
            this.Mass           = 0d;
            this.Quantity       = 1;
        }

        public Fastener(
            Size size,
            Pitch pitch,
            Thickness plateThickness,
            Material material,
            HoleType holeType,
            FastenerType type)
            : this(string.Empty, 
                   size, 
                   pitch, 
                   plateThickness, 
                   material, 
                   holeType, 
                   type)
        { }

        public Fastener(string assemblyNumber)
            : this(assemblyNumber, 
                   new Size(1d),
                   new Pitch(1d),
                   new Thickness(1d),
                   Material.Aluminum,
                   HoleType.CBore,
                   FastenerType.SocketHeadFlatScrew)
        { }

        public Fastener()
            : this(string.Empty)
        { }

        protected void AnnouncePropertiesChanged()
        {
            OnPropertyChanged(nameof(Price));
            OnPropertyChanged(nameof(Mass));
            OnPropertyChanged(nameof(Length));
            OnPropertyChanged(nameof(Quantity));
            OnPropertyChanged(nameof(SizeString));
            OnPropertyChanged(nameof(PitchString));
            OnPropertyChanged(nameof(MaterialString));
            OnPropertyChanged(nameof(TypeString));
            OnPropertyChanged(nameof(HoleTypeString));
            OnPropertyChanged(nameof(PlateThicknessString));
        }

        public void ResetToDefault()
        {
            System.IO.FileInfo defaultFile = new System.IO.FileInfo(System.IO.Path.GetFullPath(Properties.Settings.Default.DefaultFastenerFilePath));

            using (System.IO.FileStream stream = new System.IO.FileStream(
                defaultFile.FullName,
                System.IO.FileMode.OpenOrCreate,
                System.IO.FileAccess.ReadWrite,
                System.IO.FileShare.Read))
            {
                System.Xml.Serialization.XmlSerializer xml = new System.Xml.Serialization.XmlSerializer(typeof(Fastener));

                Fastener deserialized;
                try
                {
                    deserialized = (Fastener)xml.Deserialize(stream);
                }
                catch (InvalidOperationException)
                {
                    // File was empty ... probably. I should implement something a little cleaner
                    deserialized = new Fastener();
                    xml.Serialize(stream, deserialized);
                }

                if (deserialized != null)
                    this.UpdateFrom(deserialized);

                this.GetNewID();
                this.AnnouncePropertiesChanged();
            }
        }

        public void ResetToDefault(string assemblyNumber)
        {
            ResetToDefault();
            AssemblyNumber = assemblyNumber;
        }

        public void GetNewID()
        {
            ID = new Guid(GetHashData().Take(16).ToArray());
        }

        public string Description
        {
            get
            {
                return $"{SizeString} - {PitchString} x {Length.ToString()}  {this.Type.ToString()}";
            }
        }

        public override string ToString()
        {
            return $"[Fastener] {this.SizeString}, {this.PitchString}, {this.TypeString}, {this.MaterialString}, {this.Length}";
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[9][];

            blocks[0] = Encoding.Default.GetBytes(this.TypeString);
            blocks[1] = Encoding.Default.GetBytes(this.MaterialString);
            blocks[2] = BitConverter.GetBytes(this.Size.OuterDiameter);
            blocks[3] = BitConverter.GetBytes(this.Pitch.Distance);
            blocks[4] = BitConverter.GetBytes(this.PlateThickness.PlateThickness);
            blocks[5] = BitConverter.GetBytes(this.Length);
            blocks[6] = BitConverter.GetBytes(this.Mass);
            blocks[7] = BitConverter.GetBytes(this.Price);
            blocks[8] = Encoding.Default.GetBytes(this.HoleTypeString);

            return Hashing.GenerateSHA256(blocks);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(GetHashData(), 0);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fastener))
                return false;

            return this.GetHashCode().Equals((obj as Fastener).GetHashCode());
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
