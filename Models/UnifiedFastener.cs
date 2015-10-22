using Extender.Databases;
using Extender.ObjectUtils;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using Slate_EK.Models.ThreadParameters;
using System;
using System.ComponentModel;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;


namespace Slate_EK.Models
{
    public enum Units { Millimeters, Inches }

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

        private Slate_EK.Models.Units _Unit;
        
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

        [Column(Storage =  nameof(_Unit), DbType = "NVarChar(15)", CanBeNull = true), XmlIgnore]
        public Slate_EK.Models.Units Unit
        {
            get
            {
                return _Unit;
            }
            set
            {
                _Unit = value;
                OnPropertyChanged(nameof(Unit));
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
                //return new Size(Size).ToString();
                switch (Unit)
                {
                    case Units.Millimeters:
                        return new Size(Size).ToString();
                    case Units.Inches:
                        return UnifiedThreadStandard.FromMillimeters(Size)?.Designation;
                }

                return "Something fucked up.";
            }
            set
            {
                float? parsed = null;
                switch (Unit)
                {
                    case Units.Millimeters:
                        parsed = ThreadParameters.Size.TryParse(value)?.OuterDiameter;
                        break;
                    case Units.Inches:
                        parsed = (float?)Measure.Convert<Inch, Millimeter>(UnifiedThreadStandard.FromDesignation(value)?.MajorDiameter);
                        break; 
                }

                Size = parsed ?? Size; // If it failed to parse, don't change the value.
                OnPropertyChanged(nameof(SizeDisplay));
            }
        }

        [XmlIgnore]
        public string PitchDisplay
        {
            get
            {
                switch (Unit)
                {
                    case Units.Millimeters:
                        return Pitch.ToString(Spec);
                    case Units.Inches:
                        return UnifiedThreadStandard.FromMillimeters(Size)?.GetThreadDensityDisplay(Pitch);
                }

                return "Something fucked up.";
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                float? parsed = null;
                switch (Unit)
                {
                    case Units.Millimeters:
                        float n;
                        parsed = float.TryParse(value, out n) ? (float?)n : null;
                        break;
                    case Units.Inches:
                        parsed = (float?)Measure.Convert<Inch, Millimeter>
                        (
                            1f / (UnifiedThreadStandard.FromMillimeters(Size)?.GetThreadDensity(value))
                        );
                        break;
                }

                Pitch = parsed ?? Pitch; // If it failed to parse, don't change the value.
                OnPropertyChanged(nameof(PitchDisplay));
            }
        }

        [XmlIgnore]
        public string ShortPitchDisplay
        {
            get
            {
                if (Unit != Units.Inches) return PitchDisplay;

                var tpi = UnifiedThreadStandard.FromMillimeters(Size)?.GetThreadDensity(PitchDisplay);
                return tpi.HasValue ? ((int)Math.Round(tpi.Value)).ToString() : PitchDisplay;
            }
            set
            {
                PitchDisplay = value;
                OnPropertyChanged(nameof(ShortPitchDisplay));
            }
        }

        [XmlIgnore]
        public string LengthDisplay
        {
            get
            {
                switch (Unit)
                {
                    case Units.Millimeters:
                        return Length.ToString(Spec);
                    case Units.Inches:
                        return Measure.Convert<Millimeter, Inch>(Length).ToString(Spec);
                }

                return "Something fucked up.";
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                float n;
                float? parsed = float.TryParse(value, out n) ? (float?)n : null;

                switch (Unit)
                {
                    case Units.Millimeters:
                        Length = parsed ?? Length;
                        break; 
                    case Units.Inches:
                        if (!parsed.HasValue) return;
                        Length = (float)Measure.Convert<Inch, Millimeter>(parsed.Value);
                        break;
                }

                OnPropertyChanged(nameof(LengthDisplay));
            }
        } 

        [XmlIgnore]
        public string MassDisplay
        {
            get
            {
                return Mass.ToString(Spec);
            }
            set
            {
                float parsed;
                Mass = !float.TryParse(value, out parsed) ? 0f : parsed;
            }
        }

        public string UnitDisplay
        {
            get { return Enum.GetName(typeof(Units), Unit); }
            set
            {
                var unitOptions = (Units[])Enum.GetValues(typeof(Units));
                var parsed = unitOptions.FirstOrDefault
                (
                    u => Enum.GetName(typeof(Units), u)
                                .ToLower()
                                .Contains(value.ToLower())
                );

                //foreach (var unit in unitOptions)
                //{
                //    if (Enum.GetName(typeof(Units), unit).ToLower().Contains(value.ToLower()))
                //}

                Unit = parsed;
            }
        }

        [XmlIgnore]
        public string Description               => $"{SizeDisplay} - {ShortPitchDisplay} x {LengthDisplay} {Type}";

        [XmlIgnore]
        public string AlignedDescription        => $"{SizeDisplay,5} - {ShortPitchDisplay,4} x {LengthDisplay,4} {Type,6}";

        [XmlIgnore]
        public string DescriptionForPrint       => $"{Quantity}, {Description}, {Mass * Quantity}, {Price}, {Price * Quantity}";

        [XmlIgnore]
        public string AlignedPrintDescription   => $"{Quantity,3}, {AlignedDescription}, {Mass * Quantity,5}, {Price,6}, {Price * Quantity,6}";

        //
        // Constructors
        public UnifiedFastener()
        {
            Material  = Models.Material.Unspecified.ToString();
            Type      = Models.FastenerType.Unspecified.ToString();
            Unit      = Units.Millimeters;
            PlateInfo = new PlateInfo(Unit);
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

            float threadEngagemnt = (float)material.Multiplier * _Size; // number of threads (required to?) engage the hole

            float result = 0;

            if (PlateInfo.HoleType.Equals(HoleType.Straight) || PlateInfo.HoleType.Equals(HoleType.CSink))
                result = threadEngagemnt + PlateInfo.Thickness;
            else if (PlateInfo.HoleType.Equals(HoleType.CBore))
                result = threadEngagemnt + PlateInfo.Thickness - _Size;

            if (overwrite)
                Length = result;

            return result;
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
            blocks[7] = Encoding.Default.GetBytes(_Type);

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

            var b = (UnifiedFastener)obj;

            return Size.Equals(b.Size)         &&
                   Pitch.Equals(b.Pitch)       &&
                   Quantity.Equals(b.Quantity) &&
                   Price.Equals(b.Price)       &&
                   Mass.Equals(b.Mass)         &&
                   Length.Equals(b.Length)     &&
                   Material.Equals(b.Material) &&
                   Type.Equals(b.Type);
        }

        /// <summary>
        /// Compares this fastener against 'ideal' to determine if this is an acceptable
        /// match to 'ideal'.
        /// </summary>
        /// <param name="ideal">The ideal fastener.</param>
        /// <returns>
        /// If it is determined that this fastener is close enough to the ideal, 
        /// then the (absolute) difference in this.Length and ideal.Length is returned. 
        /// If it is not close, a negative value is returned. 
        /// </returns>
        public float CloseEnough(UnifiedFastener ideal)
        {
            bool close = (this.Size.Equals(ideal.Size)         &&
                          this.Pitch.Equals(ideal.Pitch)       &&
                          this.Type.Equals(ideal.Type)         &&
                          this.Material.Equals(ideal.Material) &&
                         (this.Length >= ideal.Length));

            return close ? Math.Abs(this.Length - ideal.Length) : -1;
        }

        /// <summary>
        /// Attempts to create a new UnifiedFastener from a string Description.
        /// </summary>
        /// <param name="fastenerDescription"></param>
        /// <returns></returns>
        public static UnifiedFastener FromString(string fastenerDescription)
        {
            Regex verify = new Regex(@"([Mm0-9 .//#]{2,5})-([0-9 .]{3,})x([0-9 .]{3,8})([FfCcHhSsLl]{4,6})"); 
            var   match  = verify.Match(fastenerDescription);

            if (!match.Success) return null;

            var captures = match.Groups;
            if (captures.Count == 5)
            {
                Units unit = (captures[1].ToString().Contains("#") || captures[1].ToString().Contains(@"/")) ?
                                 Units.Inches : Units.Millimeters;

                return new UnifiedFastener
                {
                    Unit         = unit,
                    SizeDisplay  = captures[1].ToString().Trim(),
                    PitchDisplay = captures[2].ToString().Trim(),
                    Material     = Models.Material.Steel.ToString(),
                    Type         = FastenerType.Parse(captures[4].ToString()).ToString(),
                    Length       = float.Parse(captures[3].ToString()),
                    PlateInfo    = new PlateInfo(unit)
                };
            }

            return null;
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #region //Aliases

        private string Spec => Properties.Settings.Default.FloatFormatSpecifier;

        #endregion
    }
}
