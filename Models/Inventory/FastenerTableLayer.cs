﻿using System;
using System.Collections.Generic;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using Extender.Databases;
using Extender.ObjectUtils;

namespace Slate_EK.Models.Inventory
{
    [Table(Name = "Fasteners")]
    public class FastenerTableLayer : IStorable
    {
        //
        // TODOh Ultimately, I want the inventory to be kept/maintained in a real DB, and importing and exporting can be done via
        //       csv files. I could see if Eric has access to a real inventory database.

        // BomViewModel can query the database when it is adding a fastener to the BOM.
        // I can write a quick custom object to handle csv exporting of a completed BOM for print.

        [Column(IsPrimaryKey = true, Storage = "_UniqueID")]
        public Guid UniqueID
        {
            get
            {
                if (_UniqueID == null)
                    _UniqueID = new Guid(GetHashData());

                return _UniqueID;
            }
        }

        [Column(Storage = "_Length")]
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

        [Column(Storage = "_Price")]
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

        [Column(Storage = "_Mass")]
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

        [Column(Storage = "_Size")]
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

        [Column(Storage = "_StockQuantity")]
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

        [Column(Storage = "_Pitch")]
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

        [Column(Storage = "_Material")]
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

        [Column(Storage = "_FastenerType")]
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

        private double  _Length;
        private double  _Price;
        private double  _Mass;
        private int     _StockQuantity;
        private double  _Size;
        private double  _Pitch;
        private string  _Material;
        private string  _FastenerType;

        private Guid    _UniqueID;
        
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
            _Material       = fastener.MaterialString;
            _FastenerType   = fastener.TypeString;
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
            cast.Material       = value.MaterialString;
            cast.FastenerType   = value.TypeString;

            cast.ForceNewUniqueID();

            return cast;
        }

        public void ForceNewUniqueID()
        {
            _UniqueID = new Guid(GetHashData());
        }

        public byte[] GetHashData()
        {
            byte[][] blocks = new byte[8][];

            blocks[0] = BitConverter.GetBytes(_Length);
            blocks[1] = BitConverter.GetBytes(_Price);
            blocks[2] = BitConverter.GetBytes(_Mass);
            blocks[3] = BitConverter.GetBytes(_StockQuantity);
            blocks[4] = BitConverter.GetBytes(_Size);
            blocks[5] = BitConverter.GetBytes(_Pitch);
            blocks[6] = Encoding.Default.GetBytes(_Material);
            blocks[7] = Encoding.Default.GetBytes(_FastenerType);

            return Hashing.GenerateSHA256(blocks).Take(16).ToArray(); // I know... I know...
        }

        public override int GetHashCode()
        {
            return BitConverter.ToInt32(GetHashData(), 0);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FastenerTableLayer))
                return false;

            return GetHashCode().Equals(obj.GetHashCode());
        }
    }
}