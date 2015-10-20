using Extender;
using Extender.UnitConversion.Lengths;
using System;
using System.Text.RegularExpressions;

namespace Slate_EK.Models.ThreadParameters
{
    public class Size
    {
        // These need to be publicly set-able for the serialized array to work correctly.
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable once MemberCanBePrivate.Global
        /// <summary>
        /// Outer diameter (in millimeters) of the screw being described.
        /// </summary>
        public float OuterDiameter { get; set; }

        protected const double SIGMA = 0.0001d;

        public Size()
        {
        }

        public Size(float od)
        {
            OuterDiameter = od;
        }

        public Size(int od)
        {
            OuterDiameter = od;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="designation">Size designation. IE: M6. Cannot contain decimal values.</param>
        public Size(string designation)
        {
            OuterDiameter = TryParse(designation).OuterDiameter;
        }

        public static Size TryParse(string size)
        {
            if (string.IsNullOrWhiteSpace(size)) return new Size();

            Regex query    = new Regex("([^0-9.-])");
            string cleaned = query.Replace(size, "");

            float od;
            float.TryParse(cleaned, out od);

            return new Size(od);
        }

        public override string ToString()
        {
            return Math.Abs(OuterDiameter % 1) < SIGMA ? $"M{Math.Round(OuterDiameter)}" : new Millimeter(OuterDiameter).ToString(Spec);
        }

        public override bool Equals(object obj)
        {
            if (obj is Size)
                return OuterDiameter.RoughEquals((obj as Size).OuterDiameter, SIGMA);
            if (obj is float)
                return OuterDiameter.RoughEquals((float)obj, SIGMA);
            if (obj is double)
                return OuterDiameter.RoughEquals((double)obj, SIGMA);
            if (obj is decimal)
                return ((decimal)OuterDiameter).Equals((decimal)obj);
            if (obj.IsNumber())
                return OuterDiameter.RoughEquals((long)obj, SIGMA);

            return false;
        }

        public override int GetHashCode()
        {
            return OuterDiameter.GetHashCode();
        }

        public static bool operator == (Size a, Size b)
        {
            return ReferenceEquals(null, a) ? ReferenceEquals(null, b) : a.Equals(b);
        }

        public static bool operator != (Size a, Size b)
        {
            return !(a == b);
        }

        private string Spec => Properties.Settings.Default.FloatFormatSpecifier;
    }
}
