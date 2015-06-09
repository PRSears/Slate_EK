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

        public string ID
        {
            get
            {
                return _ID;
            }
            set
            {
                _ID = value;
                OnPropertyChanged("ID");
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
        private string      _ID;
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

            return  this.FamilyType.Equals(b.FamilyType)        &&
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

            blocks[0] = Encoding.Default.GetBytes(this.FamilyType);
            blocks[1] = Encoding.Default.GetBytes(this.Material.ToString());
            blocks[2] = BitConverter.GetBytes(this.Pitch);
            blocks[2] = BitConverter.GetBytes(this.Thickness);
            blocks[2] = BitConverter.GetBytes(this.Length);
            blocks[2] = BitConverter.GetBytes(this.Mass);

            return Hashing.GenerateHashCode(blocks);
        }

        public static string[] GetPropertyNames()
        {
            System.Reflection.PropertyInfo[] properties = typeof(Fastener).GetProperties(Fastener._BindingFlags);

            return properties.Select(p => p.Name)
                             .ToArray();
        }

        private static System.Reflection.BindingFlags _BindingFlags = System.Reflection.BindingFlags.Public |
                                                                      System.Reflection.BindingFlags.Instance |
                                                                      System.Reflection.BindingFlags.GetProperty;

        public override string ToString()
        {
            System.Reflection.PropertyInfo[] properties = this.GetType().GetProperties(Fastener._BindingFlags);

            StringBuilder buffer = new StringBuilder();

            for (int i = 0; i < properties.Length; i++)
            {
                var propertyValue = properties[i].GetValue(this);

                if(propertyValue != null)
                    buffer.Append(propertyValue.ToString());

                if(i != (properties.Length - 1)) // the last property
                    buffer.Append(",");
            }

            return buffer.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">data must be aligned with headers.</param>
        /// <param name="header">headers must be aligned with data.</param>
        public void LoadFromString(string data, string header)
        {
            System.Reflection.PropertyInfo[] properties = this.GetType().GetProperties(Fastener._BindingFlags);

            string[] dataValues     = data.Split(',');
            string[] headerValues   = header.Split(',');

            for(int i = 0; i < dataValues.Length; i++)
            {
                System.Reflection.PropertyInfo property = properties.First(p => p.Name == headerValues[i]);

                if (property != null && property.CanWrite)
                {
                    if(property.PropertyType.Equals(typeof(string)))
                    {
                        property.SetValue(this, dataValues[i]);
                    }
                    else if (property.PropertyType.Equals(typeof(int)))
                    {
                        property.SetValue(this, int.Parse(dataValues[i]));
                    }
                    else if (property.PropertyType.Equals(typeof(double)))
                    {
                        property.SetValue(this, double.Parse(dataValues[i]));
                    }
                    else if (property.PropertyType.Equals(typeof(decimal)))
                    {
                        property.SetValue(this, decimal.Parse(dataValues[i]));
                    }
                    else if (property.PropertyType.Equals(typeof(Material)))
                    {
                        property.SetValue(this, Models.Material.Parse(dataValues[i]));
                    }
                    else
                    {
                        throw new ArgumentException
                        (
                            string.Format
                            (
                                "Type of data[{0}] not a valid property type for Fastener. ({1})",
                                i.ToString(),
                                property.PropertyType.ToString()
                            )
                        );
                    }
                }
            }
        }

        public static Fastener FromString(string data, string header)
        {
            Fastener newFastener = new Fastener();
            newFastener.LoadFromString(data, header);
            return newFastener;
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
            // --- Test to/from string

            Fastener f1 = new Fastener();
            f1.ID = "0000-0001";
            f1.FamilyType = "long screw";
            f1.Length = 2d;
            f1.Mass = 0.3;
            f1.Material = Material.Steel;
            f1.Pitch = 0.25;
            f1.Price = 0.1M;
            f1.Quantity = 1;
            f1.Thickness = 1d;

            string asString = f1.ToString();
            string headers = string.Join(",", Fastener.GetPropertyNames());

            Console.WriteLine(asString);

            Fastener f2 = Fastener.FromString(asString, headers);

            Console.WriteLine(f2.ToString());

            // --- Test to/from file

            string testHarnessFile = "TestHarness.txt";

            using(StreamWriter stream = File.CreateText(testHarnessFile))
            {
                stream.WriteLine(headers);
                stream.WriteLine(f1.ToString());
            }

            using(StreamReader stream = File.OpenText(testHarnessFile))
            {
                string readHeader = stream.ReadLine();
                string fastenerString = stream.ReadLine();

                Fastener f3 = Fastener.FromString(fastenerString, readHeader);
                Console.WriteLine(f3.ToString());
            }
        }
    }
}
