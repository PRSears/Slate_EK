using Extender;
using System;
using System.Linq;

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
            OuterDiameter = this.TryParse(designation);
        }

        protected double TryParse(string designation)
        {
            double parsed;

            if (designation.Contains('M'))
            {
                bool result = double.TryParse
                (
                    new string(designation.Where(Char.IsDigit).ToArray()),
                    out parsed
                );

                if (result) return parsed;
                else
                {
                    Extender.Debugging.Debug.WriteMessage
                    (
                        string.Format(@"Could not parse Size designation string: ""{0}""", designation),
                        "error"
                    );

                    return 0d;
                }
            }
            else
            {
                bool result = double.TryParse(designation, out parsed);

                if (result) return parsed;
                else
                {
                    Extender.Debugging.Debug.WriteMessage
                    (
                        string.Format(@"Could not parse Size designation string: ""{0}""", designation),
                        "error"
                    );
                    return 0d;
                }
            }
        }

        public override string ToString()
        {
            // If it is an integer
            if(OuterDiameter % 1 == 0)
                return string.Format("M{0}", ((int)Math.Round(OuterDiameter)).ToString());
            else
                return string.Format("{0}mm", OuterDiameter.ToString());

            // TODO Check on this later to make sure it's actually what we want
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
