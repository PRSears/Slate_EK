using Extender.Databases;
using Extender.ObjectUtils;
using System;
using System.Data.Linq.Mapping;
using System.Text;

namespace Slate_EK.Models.Inventory
{
    [Table(Name = "Fasteners")]
    public class FastenerTableLayer : IStorable
    {
        // TODO BomViewModel can query the database when it is adding a fastener to the BOM.
        // I can write a quick custom object to handle csv exporting of a completed BOM for print. 
        // Or just use string interpolation and a text file stream.

        // Adding a fastener to the BOM should search for the closest match from inventory & update stock at some point

        [Column(IsPrimaryKey = true, Storage = nameof(_UniqueId))]
        public Guid UniqueID
        {
            get
            {
                if (_UniqueId == null)
                    _UniqueId = new Guid(GetHashData());

                return _UniqueId;
            }
        }

        [Column(Storage = nameof(_Length), UpdateCheck = UpdateCheck.Never)]
        public double Length
        {
            get
            {
                return _Length;
            }
            set
            {
                _Length = value;
            }
        }

        [Column(Storage = nameof(_Price), UpdateCheck = UpdateCheck.Never)]
        public double Price
        {
            get
            {
                return _Price;
            }
            set
            {
                _Price = value;
            }
        }

        [Column(Storage = nameof(_Mass), UpdateCheck = UpdateCheck.Never)]
        public double Mass
        {
            get
            {
                return _Mass;
            }
            set
            {
                _Mass = value;
            }
        }

        [Column(Storage = nameof(_Size), UpdateCheck = UpdateCheck.Never)]
        public double Size
        {
            get
            {
                return _Size;
            }
            set
            {
                _Size = value;
            }
        }

        [Column(Storage = nameof(_StockQuantity), UpdateCheck = UpdateCheck.Never)]
        public int StockQuantity
        {
            get
            {
                return _StockQuantity;
            }
            set
            {
                _StockQuantity = value;
            }
        }

        [Column(Storage = nameof(_Price), UpdateCheck = UpdateCheck.Never)]
        public double Pitch
        {
            get
            {
                return _Pitch;
            }
            set
            {
                _Pitch = value;
            }
        }

        [Column(Storage = nameof(_Material), UpdateCheck = UpdateCheck.Never)]
        public string Material
        {
            get
            {
                return _Material;
            }
            set
            {
                _Material = value;
            }
        }

        [Column(Storage = nameof(_FastenerType), UpdateCheck = UpdateCheck.Never)]
        public string FastenerType
        {
            get
            {
                return _FastenerType;
            }
            set
            {
                _FastenerType = value;
            }
        }

        [Column(Storage = nameof(_Thickness), UpdateCheck = UpdateCheck.Never)]
        public double Thickness
        {
            get { return _Thickness; }
            set { _Thickness = value; }
        }

        [Column(Storage = nameof(_HoleType), UpdateCheck = UpdateCheck.Never)]
        public string HoleType
        {
            get { return _HoleType; }
            set { _HoleType = value; }
        }

        private double  _Length;
        private double  _Price;
        private double  _Mass;
        private int     _StockQuantity;
        private double  _Size;
        private double  _Pitch;
        private double  _Thickness;
        private string  _Material;
        private string  _FastenerType;
        private string  _HoleType;

        private Guid    _UniqueId;
        
        public FastenerTableLayer()
        {
            
        }

        public FastenerTableLayer(Fastener fastener)
        {
            _Length         = fastener.Length;
            _Price          = fastener.Price;
            _Mass           = fastener.Mass;
            _StockQuantity  = fastener.Quantity;
            _Size           = fastener.Size.OuterDiameter;
            _Pitch          = fastener.Pitch.Distance;
            _Thickness      = fastener.PlateThickness.PlateThickness;
            _Material       = fastener.MaterialString;
            _FastenerType   = fastener.TypeString;
            _HoleType       = fastener.HoleTypeString;
        }
        
        public static explicit operator FastenerTableLayer(Fastener value)
        {
            FastenerTableLayer cast = new FastenerTableLayer();

            cast.Length         = value.Length;
            cast.Price          = value.Price;
            cast.Mass           = value.Mass;
            cast.StockQuantity  = value.Quantity;
            cast.Size           = value.Size.OuterDiameter;
            cast.Pitch          = value.Pitch.Distance;
            cast.Thickness      = value.PlateThickness.PlateThickness;
            cast.Material       = value.MaterialString;
            cast.FastenerType   = value.TypeString;
            cast.HoleType       = value.HoleTypeString;

            cast.ForceNewUniqueID();

            return cast;
        }

        public void ForceNewUniqueID()
        {
            _UniqueId = new Guid(GetHashData());
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[9][];

            //
            // THOUGHT Should quantity be used in the UID? 
            //         If it is then changing the quantity will make a new listing in the database.
            //         Excluding it here won't matter to normal Fastener objects, only for the db. 
            blocks[0] = BitConverter.GetBytes(_Length);
            blocks[1] = BitConverter.GetBytes(_Price);
            blocks[2] = BitConverter.GetBytes(_Mass);
            //blocks[3] = BitConverter.GetBytes(_StockQuantity); 
            blocks[3] = BitConverter.GetBytes(_Size);
            blocks[4] = BitConverter.GetBytes(_Pitch);
            blocks[5] = Encoding.Default.GetBytes(_Material);
            blocks[6] = Encoding.Default.GetBytes(_FastenerType);
            blocks[7] = Encoding.Default.GetBytes(_HoleType);
            blocks[8] = BitConverter.GetBytes(_Thickness);

            return Hashing.GenerateHashCode(blocks); // I know... I know...
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(GetHashData(), 0);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is FastenerTableLayer))
                return false;

            return GetHashCode().Equals(obj.GetHashCode());
        }
    }
}
