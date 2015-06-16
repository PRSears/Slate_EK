using Extender.IO;
using Extender.ObjectUtils;
using System;
using System.ComponentModel;
using System.IO;
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
                OnPropertyChanged("AssemblyNumber");
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

        /// <summary>
        /// Contains a string representation of the ID.
        /// This property is used for serialization only. Use this.ID directly, instead.
        /// </summary>
        public string UniqueID
        {
            get
            {
                return this.ID.ToString();
            }
            set
            {
                this.ID = new Guid(value);
            }
        }

        public string FamilyType
        {
            get
            {
                return _FamilyType;
            }
            set
            {
                _FamilyType = value;
                OnPropertyChanged("Family");
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
                OnPropertyChanged("Material");
            }
        }

        /// <summary>
        /// Contains a string representation of the Material. 
        /// This property is used for serialization only. Use this.Material directly, instead.
        /// </summary>
        public string MaterialString
        {
            get
            {
                return this.Material.ToString();
            }
            set
            {
                this.Material = Material.Parse(value);
                OnPropertyChanged("MaterialString");
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

        [System.Xml.Serialization.XmlIgnore]
        public string PitchString
        {
            get
            {
                return Pitch.ToString();
            }
            set
            {
                Pitch = double.Parse(value);
                OnPropertyChanged("PitchString");
            }
        }

        public double PlateThickness
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

        [System.Xml.Serialization.XmlIgnore]
        public string PlateThicknessString
        {
            get
            {
                return PlateThickness.ToString();
            }
            set
            {
                PlateThickness = double.Parse(value);
                OnPropertyChanged("ThicknessString");
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

        [System.Xml.Serialization.XmlIgnore]
        public string LengthString
        {
            get
            {
                return Length.ToString();
            }
            set
            {
                Length = double.Parse(value);
                OnPropertyChanged("LengthString");
            }
        }

        public double Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
                OnPropertyChanged("Size");
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string SizeString
        {
            get
            {
                return new Models.Size(Size).ToString();
            }
            set
            {
                Size = Models.Size.TryParse(value).OuterDiameter;
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

        [System.Xml.Serialization.XmlIgnore]
        public string MassString
        {
            get
            {
                return Mass.ToString();
            }
            set
            {
                Mass = double.Parse(value);
                OnPropertyChanged("MassString");
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
                OnPropertyChanged("Price");
            }
        }

        [System.Xml.Serialization.XmlIgnore]
        public string PriceString
        {
            get
            {
                return Price.ToString();
            }
            set
            {
                Price = double.Parse(value);
                OnPropertyChanged("PriceString");
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

        [System.Xml.Serialization.XmlIgnore]
        public string QuantityString
        {
            get
            {
                return Quantity.ToString();
            }
            set
            {
                Quantity = int.Parse(value);
                OnPropertyChanged("QuantityString");
            }
        }

        #region Boxed properties
        private double      _Price;
        private double      _Mass;
        private double      _Length;
        private double      _Thickness;
        private double      _Size;
        private double      _Pitch;
        private string      _FamilyType;
        private string      _AssemblyNumber;
        private int         _Quantity;
        private Material    _Material;
        private Guid        _ID;
        #endregion

        public Fastener() 
            : this(string.Empty, Material.NotSpecified, 0, 0)
        {

        }

        public Fastener(string assemblyNumber) 
            : this()
        {
            this.AssemblyNumber = assemblyNumber;
        }

        public Fastener(string type, Material material, double size, double pitch)
        {
            this.Price          = 0d;
            this.Mass           = 0d;
            this.Length         = 0d;
            this.PlateThickness = size;
            this.Pitch          = pitch;
            this.FamilyType     = type;
            this.AssemblyNumber = "0";
            this.Quantity       = 0;
            this.Material       = material;
        }

        public Fastener(string assemblyNumber, string type, Material material, double size, double pitch)
            : this(type, material, size, pitch)
        {
            this.AssemblyNumber = assemblyNumber;
        }

        public void RefreshID()
        {
            _ID = new Guid(GetHashData().Take(16).ToArray());
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Fastener))
                return false;

            Fastener b = (Fastener)obj;

            return this.ID.Equals(b.ID);
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(this.GetHashData(), 0);
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[8][];

            blocks[0] = Encoding.Default.GetBytes(this.FamilyType);
            blocks[1] = Encoding.Default.GetBytes(this.Material.ToString());
            blocks[2] = BitConverter.GetBytes(this.Pitch);
            blocks[3] = BitConverter.GetBytes(this.PlateThickness);
            blocks[4] = BitConverter.GetBytes(this.Length);
            blocks[5] = BitConverter.GetBytes(this.Mass);
            blocks[6] = BitConverter.GetBytes(this.Price);
            blocks[7] = BitConverter.GetBytes(this.Size);

            return Hashing.GenerateSHA256(blocks);
        }

        public override string ToString()
        {
            return string.Format
            (
                "[Fastener] {0}, {1}, {2}, {3}, {4}",
                FamilyType,
                Material.ToString(),
                Size,
                Pitch, 
                Length
            );
        }

        public string Description
        {
            get
            {
                return string.Format
                (
                    "{0} - {1} - {2}   {3}",
                    this.SizeString,
                    this.Pitch,
                    this.Length,
                    this.Material.ToString()
                );
            }
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

        public static void TestHarness()
        {
            Fastener[] fasteners = new Fastener[2500];
            for(int i = 0; i < 2500; i++)
            {
                fasteners[i] = new Fastener();
                fasteners[i].FamilyType = "some screw " + i.ToString("D4");
                fasteners[i].Length = 2d + (i / 1000d);
                fasteners[i].Mass = 0.3d + (Math.PI * i / 1000d);
                fasteners[i].Material = (i % 5 == 0) ? Material.Steel : Material.Aluminum;
                fasteners[i].Pitch = 0.25d;
                fasteners[i].Price = 0.01d;
                fasteners[i].Quantity = 1;
                fasteners[i].PlateThickness = 0.25d;
                fasteners[i].RefreshID();
            }


            using (FileStream stream = new FileStream("TestHarness.csv", FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                System.Diagnostics.Stopwatch t = new System.Diagnostics.Stopwatch();

                t.Start();
                CsvSerializer<Fastener> csv = new CsvSerializer<Fastener>();
                csv.Serialize(stream, fasteners);
                t.Stop();

                Extender.Debugging.Debug.WriteMessage
                (
                    string.Format("Fastener.TestHarness() took {0}ms to serialize the array.", t.ElapsedMilliseconds.ToString()),
                    "debug"
                );
            }
        }

        //public static void TestHarness()
        //{
        //    Console.WriteLine(string.Join(",", Fastener.GetPropertyNames()));
        //}

        //public static void TestHarness_1()
        //{
        //    // --- Test to/from string

        //    Fastener f1 = new Fastener();
        //    f1.ID = "0000-0001";
        //    f1.FamilyType = "long screw";
        //    f1.Length = 2d;
        //    f1.Mass = 0.3;
        //    f1.Material = Material.Steel;
        //    f1.Pitch = 0.25;
        //    f1.Price = 0.1M;
        //    f1.Quantity = 1;
        //    f1.Thickness = 1d;

        //    string asString = f1.ToString();
        //    string headers = string.Join(",", Fastener.GetPropertyNames());

        //    Console.WriteLine(asString);

        //    Fastener f2 = Fastener.FromString(asString, headers);

        //    Console.WriteLine(f2.ToString());

        //    // --- Test to/from file

        //    string testHarnessFile = "TestHarness.csv";

        //    using(StreamWriter stream = File.CreateText(testHarnessFile))
        //    {
        //        stream.WriteLine(headers);
        //        stream.WriteLine(f1.ToString());
        //    }

        //    using(StreamReader stream = File.OpenText(testHarnessFile))
        //    {
        //        string readHeader = stream.ReadLine();
        //        string fastenerString = stream.ReadLine();

        //        Fastener f3 = Fastener.FromString(fastenerString, readHeader);
        //        Console.WriteLine(f3.ToString());
        //    }
        //}
    }
}
