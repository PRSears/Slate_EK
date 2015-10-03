using Extender;
using System;
using System.Text.RegularExpressions;

namespace Slate_EK.Models
{
    public class Size
    {
        /// <summary>
        /// Outer diameter (in millimeters) of the screw being described.
        /// </summary>
        public double OuterDiameter { get; }

        protected const double SIGMA = 0.0001d;

        public Size()
        {
        }

        public Size(double od)
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

        public static Size TryParse(string pitch)
        {
            Regex query = new Regex("([^0-9.-])");
            string cleaned = query.Replace(pitch, "");

            double distance;
            double.TryParse(cleaned, out distance);

            return new Size(distance);
        }

        public override string ToString()
        {
            // TODO_ Check on this later to make sure it's actually what we want
            return Math.Abs(OuterDiameter % 1) < SIGMA ? $"M{Math.Round(OuterDiameter),-2}" : $"{OuterDiameter}mm";
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
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static bool operator != (Size a, Size b)
        {
            return !(a == b);
        }
    }
}
