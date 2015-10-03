using Extender;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Slate_EK.Models
{
    public class Thickness
    {
        public double PlateThickness { get; }

        protected const double SIGMA = 0.0001d;

        public Thickness()
        {
            //TODO switch to using UnitConversion.Lengths to make switching between inches / mm easier
        }

        public Thickness(double thickness)
        {
            PlateThickness = thickness;
        }

        public static Thickness TryParse(string value)
        {
            Regex query = new Regex("([^0-9.-])");
            string cleaned = query.Replace(value, "");

            double thickness = 0d;
            double.TryParse(cleaned, out thickness);

            return new Thickness(thickness);
        }

        public override int GetHashCode()
        {
            return PlateThickness.GetHashCode();
        }

        public override string ToString()
        {
            return PlateThickness.ToString(CultureInfo.CurrentCulture);
        }

        public override bool Equals(object obj)
        {
            if (obj is Thickness)
                return PlateThickness.RoughEquals((obj as Thickness).PlateThickness, SIGMA);
            if (obj is float)
                return PlateThickness.RoughEquals((float)obj, SIGMA);
            if (obj is double)
                return PlateThickness.RoughEquals((double)obj, SIGMA);
            if (obj is decimal)
                return ((decimal)PlateThickness).Equals((decimal)obj);
            if (obj.IsNumber())
                return PlateThickness.RoughEquals((long)obj, SIGMA);
            return false;
        }

        #region operator overloads
        public static bool operator ==(Thickness a, Thickness b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static bool operator !=(Thickness a, Thickness b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return !a.Equals(b);
        }

        public static bool operator ==(double a, Thickness b)
        {
            if (ReferenceEquals(null, b))
                return ReferenceEquals(null, a);

            return b.Equals(a);
        }

        public static bool operator !=(double a, Thickness b)
        {
            if (ReferenceEquals(null, b))
                return ReferenceEquals(null, a);

            return !b.Equals(a);
        }
        public static bool operator ==(Thickness a, double b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);
            
            return a.Equals(b);
        }

        public static bool operator !=(Thickness a, double b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return !a.Equals(b);
        }
        #endregion
    }
}
