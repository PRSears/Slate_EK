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
        public double OuterDiameter { get; set; }

        protected const double SIGMA = 0.0001d;

        public Size()
        {
        }

        public Size(double OD)
        {
            OuterDiameter = OD;
        }

        public Size(int OD)
        {
            OuterDiameter = OD;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="designation">Size designation. Ie: M6. Cannot contain decimal values.</param>
        public Size(string designation)
        {
            OuterDiameter = Size.TryParse(designation).OuterDiameter;
        }

        public static Size TryParse(string pitch)
        {
            Regex query = new Regex("([^0-9.-])");
            string cleaned = query.Replace(pitch, "");

            double distance = 0d;
            double.TryParse(cleaned, out distance);

            return new Size(distance);
        }

        public override string ToString()
        {
            // If it is an integer
            if(OuterDiameter % 1 == 0)
                return string.Format("M{0}", ((int)Math.Round(OuterDiameter)).ToString());
            else
                return string.Format("{0}mm", OuterDiameter.ToString());

            // TODO_ Check on this later to make sure it's actually what we want
        }

        public override bool Equals(object obj)
        {
            if (obj is Models.Size)
                return this.OuterDiameter.RoughEquals((obj as Models.Size).OuterDiameter, SIGMA);
            else if (obj is float)
                return OuterDiameter.RoughEquals((float)obj, SIGMA);
            else if (obj is double)
                return OuterDiameter.RoughEquals((double)obj, SIGMA);
            else if (obj is decimal)
                return ((decimal)OuterDiameter).Equals((decimal)obj);
            else if (obj.IsNumber())
                return OuterDiameter.RoughEquals((long)obj, SIGMA);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return OuterDiameter.GetHashCode();
        }

        public static Boolean operator == (Models.Size a, Models.Size b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static Boolean operator != (Models.Size a, Models.Size b)
        {
            return !(a == b);
        }
    }
}
