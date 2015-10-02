using Extender;
using System;
using System.Text.RegularExpressions;

namespace Slate_EK.Models
{
    public class Thickness
    {
        public double PlateThickness { get; set; }

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
            return PlateThickness.ToString();
        }

        public override bool Equals(object obj)
        {
            if (obj is Thickness)
                return this.PlateThickness.RoughEquals((obj as Thickness).PlateThickness, SIGMA);
            else if (obj is float)
                return PlateThickness.RoughEquals((float)obj, SIGMA);
            else if (obj is double)
                return PlateThickness.RoughEquals((double)obj, SIGMA);
            else if (obj is decimal)
                return ((decimal)PlateThickness).Equals((decimal)obj);
            else if (obj.IsNumber())
                return PlateThickness.RoughEquals((long)obj, SIGMA);
            else
                return false;
        }

        #region operator overloads
        public static Boolean operator ==(Models.Thickness a, Models.Thickness b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static Boolean operator !=(Models.Thickness a, Models.Thickness b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return !a.Equals(b);
        }

        public static Boolean operator ==(double a, Models.Thickness b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return b.Equals(a);
        }

        public static Boolean operator !=(double a, Models.Thickness b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return !b.Equals(a);
        }
        public static Boolean operator ==(Models.Thickness a, double b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);
            
            return a.Equals(b);
        }

        public static Boolean operator !=(Models.Thickness a, double b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return !a.Equals(b);
        }
        #endregion
    }
}
