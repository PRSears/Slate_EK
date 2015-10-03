using Extender;
using System;
using System.Diagnostics;

namespace Slate_EK.Models
{
    public class Material
    {
        private readonly string _Material;

        public double Multiplier
        {
            get;
        }

        private Material(string material, double multiplier)
        {
            _Material  = material.ToLower();
            Multiplier = multiplier;
        }

        public static Material Parse(string material)
        {
            foreach (Material m in Materials)
                if (m._Material.ToLower().Equals(material.ToLower()))
                    return m;

            // try to find partial match
            foreach (Material m in Materials)
            {
                if (m._Material.ToLower().Contains(material.ToLower()))
                {
                    Extender.Debugging.Debug.WriteMessage
                    (
                        $"Material.Parse found only a partial match. {material} -> {m}",
                        "warn"
                    );

                    return m;
                }
            }

            throw new ArgumentException($@"Material name ""{material}"" is not a valid material.");
        }

        public static Material TryParse(string material)
        {
            try
            {
                return Parse(material);
            }
            catch(Exception e)
            {
                Extender.Debugging.Debug.WriteMessage
                (
                    $@"Could not parse material ""{material}""\n{e.Message}",
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

            return (_Material.Equals(b._Material))     &&
                   (Multiplier.Equals(b.Multiplier));
        }

        public override int GetHashCode()
        {
            byte[][] blocks = new byte[2][];

            Debug.Assert(_Material != null, "_Material != null");
            blocks[0] = System.Text.Encoding.Default.GetBytes(_Material);
            blocks[1] = BitConverter.GetBytes(Multiplier);

            return BitConverter.ToInt32(Extender.ObjectUtils.Hashing.GenerateHashCode(blocks), 0);
        }

        public override string ToString()
        {
            return _Material.ToPropercase();
        }

        public static bool operator == (Material a, Material b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static bool operator != (Material a, Material b)
        {
            if (ReferenceEquals(null, a))
                return !ReferenceEquals(null, b);

            return !(a == b);
        }
        #endregion

        public static Material Steel => new Material("steel", 1d);

        public static Material Aluminum => new Material("aluminum", 1.5d);
        public static Material Unspecified => new Material("unspecified", 0d);

        public static Material[] Materials = { Steel, Aluminum, Unspecified };
    }
}
