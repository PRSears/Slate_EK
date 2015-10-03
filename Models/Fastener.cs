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
        public Guid Id
        {
            get
            {
                if (_Id.Equals(Guid.Empty))
                    _Id = new Guid(GetHashData().Take(16).ToArray()); // first 16 bytes from SHA256 
                return _Id;
            }
            protected set
            {
                _Id = value;
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
                return Size.ToString();
            }
            set
            {
                Size = Size.TryParse(value);
                OnPropertyChanged(nameof(SizeString));
            }
        }

        public string PitchString
        {
            get
            {
                return Pitch.ToString();
            }
            set
            {
                Pitch = Pitch.TryParse(value);
                OnPropertyChanged(nameof(PitchString));
            }
        }

        public string PlateThicknessString
        {
            get
            {
                return PlateThickness.ToString();
            }
            set
            {
                PlateThickness = Thickness.TryParse(value);
                OnPropertyChanged(nameof(PlateThicknessString));
            }
        }

        public string MaterialString
        {
            get
            {
                return Material.ToString();
            }
            set
            {
                Material parsed;
                try
                {
                    parsed = Material.Parse(value);
                }
                catch(ArgumentException)
                {
                    parsed = Material.Unspecified;
                }

                Material = parsed;
                OnPropertyChanged(nameof(MaterialString));
            }
        }

        public string TypeString
        {
            get
            {
                return Type.ToString();
            }
            set
            {
                FastenerType parsed;
                try
                {
                    parsed = FastenerType.Parse(value);
                }
                catch (ArgumentException)
                {
                    parsed = FastenerType.Unspecified;
                }

                Type = parsed;
                OnPropertyChanged(nameof(TypeString));
            }
        }

        public string HoleTypeString
        {
            get
            {
                return HoleType.ToString();
            }
            set
            {
                HoleType parsed;
                try
                {
                    parsed = HoleType.Parse(value);
                }
                catch (ArgumentException)
                {
                    parsed = HoleType.Unspecified;
                }

                HoleType = parsed;
                OnPropertyChanged(nameof(HoleTypeString));
            }
        }

        public string IdString
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                Id = new Guid(value);
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
        private Guid            _Id;
        #endregion

        public Fastener(
            string          assemblyNumber,
            Size            size,
            Pitch           pitch,
            Thickness       plateThickness,
            Material        material,
            HoleType        holeType,
            FastenerType    type)
        {
            AssemblyNumber = assemblyNumber;
            Size           = size;
            Pitch          = pitch;
            PlateThickness = plateThickness;
            Material       = material;
            HoleType       = holeType;
            Type           = type;

            Price          = 0d;
            Mass           = 0d;
            Quantity       = 1;
        }

        public Fastener(
            Size            size,
            Pitch           pitch,
            Thickness       plateThickness,
            Material        material,
            HoleType        holeType,
            FastenerType    type)
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

        public static explicit operator Fastener(Inventory.FastenerTableLayer value)
        {
            Fastener cast = new Fastener
            {
                Length         = value.Length,
                Price          = value.Price,
                Mass           = value.Mass,
                Quantity       = value.StockQuantity,
                Size           = new Size(value.Size),
                Pitch          = new Pitch(value.Pitch),
                MaterialString = value.Material,
                TypeString     = value.FastenerType
            };

            cast.GetNewId();

            return cast;
        }

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

                GetNewId();
                AnnouncePropertiesChanged();
            }
        }

        public void ResetToDefault(string assemblyNumber)
        {
            ResetToDefault();
            AssemblyNumber = assemblyNumber;
        }

        public void GetNewId()
        {
            Id = new Guid(GetHashData().Take(16).ToArray());
        }

        public void CalculateDesiredLength()
        {
            double threadEngagemnt = Material.Multiplier * Size.OuterDiameter; // number of threads engaging the hole

            if (HoleType.Equals(HoleType.Straight) || HoleType.Equals(HoleType.CSink))
            {
                Length = threadEngagemnt + PlateThickness.PlateThickness;
            }
            else if (HoleType.Equals(HoleType.CBore))
            {
                Length = threadEngagemnt + PlateThickness.PlateThickness - Size.OuterDiameter;
            }

            // TODO Make sure length updates when size/pitch/etc are updated
            //      Can call CalculateLength() in the ViewModel and have it check
            //      if OverrideLength is selected, and do nothing if it is.

            // TODO When the fastener gets added to the BOM, search inventory for closest match and insert that.
        }

        [System.Xml.Serialization.XmlIgnore]
        public string Description => $"{SizeString} - {PitchString} x {Length,-3} {Type}";

        public override string ToString()
        {
            return $"[Fastener] {SizeString}, {PitchString}, {TypeString}, {MaterialString}, {Length}";
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[9][];

            blocks[0] = Encoding.Default.GetBytes(TypeString);
            blocks[1] = Encoding.Default.GetBytes(MaterialString);
            blocks[2] = BitConverter.GetBytes(Size.OuterDiameter);
            blocks[3] = BitConverter.GetBytes(Pitch.Distance);
            blocks[4] = BitConverter.GetBytes(PlateThickness.PlateThickness);
            blocks[5] = BitConverter.GetBytes(Length);
            blocks[6] = BitConverter.GetBytes(Mass);
            blocks[7] = BitConverter.GetBytes(Price);
            blocks[8] = Encoding.Default.GetBytes(HoleTypeString);

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

            return GetHashCode().Equals((obj as Fastener).GetHashCode());
        }

        #region INotifyPropertyChanged Members
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
