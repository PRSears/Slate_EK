using Extender;
using System;

namespace Slate_EK.Models
{
    public class Material
    {
        private string _Material;

        public double Multiplier
        {
            get;
            set;
        }

        private Material(string material, double multiplier)
        {
            this._Material  = material.ToLower();
            this.Multiplier = multiplier;
        }

        private Material(string material) : this(material, 1d) 
        { }

        public Material Parse(string material)
        {
            foreach (Material m in Materials)
                if (m._Material.Equals(material.ToLower()))
                    return m;

            // try to find partial match
            foreach (Material m in Materials)
                if (m._Material.Contains(material.ToLower()))
                {
                    Extender.Debugging.Debug.WriteMessage
                    (
                        string.Format("Material.Parse found only a partial match. {0} -> {1}", material, m.ToString()), 
                        "warn"
                    );

                    return m;
                }

            throw new ArgumentException(string.Format(@"Material name ""{0}"" is not a valid material.", material));
        }

        public Material TryParse(string material)
        {
            try
            {
                return Parse(material);
            }
            catch(Exception e)
            {
                Extender.Debugging.Debug.WriteMessage
                (
                    string.Format(@"Could not parse material ""{0}""\n{1}", material, e.Message),
                    "error"
                );
                return null;
            }
        }

        # region operators & overrides
        public override bool Equals(object obj)
        {
            if (!(obj is Material))
                return false;

            Material b = (Material)obj;

            return (this._Material.Equals(b._Material))     &&
                   (this.Multiplier.Equals(b.Multiplier));
        }

        public override int GetHashCode()
        {
            byte[][] blocks = new byte[2][];

            blocks[0] = System.Text.Encoding.Default.GetBytes(this._Material);
            blocks[1] = BitConverter.GetBytes(Multiplier);

            return BitConverter.ToInt32(Extender.ObjectUtils.Hashing.GenerateHashCode(blocks), 0);
        }

        public override string ToString()
        {
            return _Material.ToPropercase();
        }

        public static Boolean operator == (Material a, Material b)
        {
            return a.Equals(b);
        }

        public static Boolean operator != (Material a, Material b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return !(a == b);
        }
        #endregion

        public static Material Steel
        {
            get
            {
                return new Material("steel", 1d);
            }
        }

        public static Material Aluminum
        {
            get
            {
                return new Material("aluminum", 1.5d);
            }
        }

        public static Material[] Materials = { Steel, Aluminum };
    }
}
