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

        public decimal Price
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
        private decimal     _Price;
        private double      _Mass;
        private double      _Length;
        private double      _Thickness;
        private double      _Pitch;
        private string      _FamilyType;
        private string      _AssemblyNumber;
        private int         _Quantity;
        private Material    _Material;
        private Guid        _ID;
        #endregion

        public Fastener()
        {

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
            byte[][] blocks = new byte[7][];

            blocks[0] = Encoding.Default.GetBytes(this.FamilyType);
            blocks[1] = Encoding.Default.GetBytes(this.Material.ToString());
            blocks[2] = BitConverter.GetBytes(this.Pitch);
            blocks[3] = BitConverter.GetBytes(this.Thickness);
            blocks[4] = BitConverter.GetBytes(this.Length);
            blocks[5] = BitConverter.GetBytes(this.Mass);
            blocks[6] = BitConverter.GetBytes(Convert.ToDouble(this.Price));

            return Hashing.GenerateSHA256(blocks);
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
                fasteners[i].Price = 0.01M;
                fasteners[i].Quantity = 1;
                fasteners[i].Thickness = 0.25d;
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
