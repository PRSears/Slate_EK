using Extender.Databases;
using Extender.ObjectUtils;
using System;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;


namespace Slate_EK.Models
{
    [Table(Name = "Fasteners")]
    public class UnifiedFastener : IStorable, INotifyPropertyChanged
    {
        private int       _Quantity;
        private float     _Price;
        private float     _Mass;
        private float     _Size;
        private float     _Pitch;
        private float     _Length;
        private string    _Material;
        private string    _Type;
        private PlateInfo _PlateInfo;
        private Guid      _UniqueId;

        public event PropertyChangedEventHandler PropertyChanged;

        [Column(IsPrimaryKey = true, Storage = nameof(_UniqueId)), XmlIgnore]
        public Guid UniqueID
        {
            get
            {
                if (_UniqueId.Equals(Guid.Empty))
                    ForceNewUniqueID();

                return _UniqueId;
            }
        }

        [Column(Storage = nameof(_Quantity))]
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

        [Column(Storage = nameof(_Price))]
        public float Price
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

        [Column(Storage = nameof(_Mass))]
        public float Mass
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

        [Column(Storage = nameof(_Size))]
        public float Size
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

        [Column(Storage = nameof(_Pitch))]
        public float Pitch
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

        [Column(Storage = nameof(_Length))]
        public float Length
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

        [Column(Storage = nameof(_Material))]
        public string Material
        {
            get
            {
                return _Material;
            }
            set
            {
                _Material = Models.Material.TryParse(value).ToString();
                OnPropertyChanged(nameof(Material));
            }
        }

        [Column(Storage = nameof(_Type))]
        public string Type
        {
            get
            {
                return _Type;
            }
            set
            {
                _Type = Models.FastenerType.TryParse(value).ToString();
                OnPropertyChanged(nameof(Type));
            }
        }

        [XmlIgnore]
        public PlateInfo PlateInfo
        {
            get
            {
                return _PlateInfo;
            }
            set
            {
                _PlateInfo = value;
                OnPropertyChanged(nameof(PlateInfo));
            }
        }

        [XmlIgnore]
        public string SizeDisplay
        {
            get
            {
                return new Models.Size(Size).ToString();
            }
            set
            {
                Size = (float)Models.Size.TryParse(value).OuterDiameter;
                OnPropertyChanged(nameof(SizeDisplay));
            }
        }
        [XmlIgnore]
        public string Description  => $"{SizeDisplay} - {Pitch} x {Length,-3} {Type}";

        //
        // Constructors
        public UnifiedFastener()
        {
            Material  = Models.Material.Unspecified.ToString();
            Type      = Models.FastenerType.Unspecified.ToString();
            PlateInfo = new PlateInfo();
        }

        public UnifiedFastener(
            float size, 
            float pitch, 
            Material material, 
            FastenerType type, 
            PlateInfo plateInfo)
        {
            Size      = size;
            Pitch     = pitch;
            Material  = material.ToString();
            Type      = type.ToString();
            PlateInfo = plateInfo;
        }

        //
        // --------------------------------------------------------------------------

        public float CalculateLength(bool overwrite)
        {
            Material material = Models.Material.TryParse(Material);

            float threadEngagemnt = (float)material.Multiplier * Size; // number of threads engaging the hole

            float result = 0;

            if (PlateInfo.HoleType.Equals(HoleType.Straight) || PlateInfo.HoleType.Equals(HoleType.CSink))
                result = threadEngagemnt + PlateInfo.Thickness;
            else if (PlateInfo.HoleType.Equals(HoleType.CBore))
                result = threadEngagemnt + PlateInfo.Thickness - Size;

            if (overwrite)
                Length = result;

            return result;

            // TODO When the fastener gets added to the BOM, search inventory for closest match and insert that.
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[8][];

            blocks[0] = BitConverter.GetBytes(_Quantity);
            blocks[1] = BitConverter.GetBytes(_Price);
            blocks[2] = BitConverter.GetBytes(_Mass);
            blocks[3] = BitConverter.GetBytes(_Size);
            blocks[4] = BitConverter.GetBytes(_Pitch);
            blocks[5] = BitConverter.GetBytes(_Length);
            blocks[6] = Encoding.Default.GetBytes(_Material);
            blocks[7] = Encoding.Default.GetBytes(Type);

            return Hashing.GenerateHashCode(blocks);
        }

        public void ForceNewUniqueID()
        {
            _UniqueId = new Guid(GetHashData());
        }

        /// <summary>
        /// Serves as the default hash function. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return BitConverter.ToInt32(GetHashData(), 0);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            return $"{Size}, {Pitch}, {Type}, {Material}, {Length}, {Quantity}, {Price}, {Mass}";
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (!(obj is UnifiedFastener))
                return false;

            return GetHashCode().Equals(((UnifiedFastener)obj).GetHashCode());
        }

        public static UnifiedFastener FromString(string fastenerDescription)
        {
            Regex verify = new Regex(@"([Mm0-9 .]{4})-([0-9 .]{3,})x([ 0-9]{3,5})([FfCcHhSsLl]{4,6})");
            var match    = verify.Match(fastenerDescription);

            if (match.Success)
            {
                var captures = match.Groups;
                if (captures.Count == 5)
                {
                    return new UnifiedFastener
                    (
                        (float)(new Size(captures[1].ToString()).OuterDiameter),
                        float.Parse(captures[2].ToString()),
                        Models.Material.Steel,
                        FastenerType.Parse(captures[4].ToString()),
                        new PlateInfo()
                    ) {Length = float.Parse(captures[3].ToString())};
                }
            }

            return null;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
