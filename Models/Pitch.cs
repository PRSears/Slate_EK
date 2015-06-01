using Extender;
using System;

namespace Slate_EK.Models
{
    public class Pitch
    {
        public double Distance { get; set; }

        protected const double SIGMA = 0.0001d;

        public Pitch()
        {
        }

        public Pitch(double pitch)
        {
            this.Distance = pitch;
        }

        public Pitch(int pitch)
        {
            this.Distance = pitch;
        }

        public override string ToString()
        {
            return Distance.ToString();
        }

        public override int GetHashCode()
        {
            return Distance.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (obj is Models.Pitch)
                return this.Distance.RoughEquals((obj as Models.Pitch).Distance, SIGMA);
            else if (obj is float)
                return Distance.RoughEquals((float)obj, SIGMA);
            else if (obj is double)
                return Distance.RoughEquals((double)obj, SIGMA);
            else if (obj is decimal)
                return ((decimal)Distance).Equals((decimal)obj);
            else if (obj.IsNumber())
                return Distance.RoughEquals((long)obj, SIGMA);
            else
                return false;
        }
        public static Boolean operator ==(Models.Pitch a, Models.Pitch b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static Boolean operator !=(Models.Pitch a, Models.Pitch b)
        {
            return !(a == b);
        }
    }
}
