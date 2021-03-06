﻿using Extender;
using System.Text.RegularExpressions;

namespace Slate_EK.Models.ThreadParameters
{
    public class Pitch
    {
        // These need to be publicly set-able for the serialized array to work correctly.
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable once MemberCanBePrivate.Global
        public float Distance { get; set; }

        protected const double SIGMA = 0.0001d;

        public Pitch()
        {
        }

        public Pitch(float pitch)
        {
            Distance = pitch;
        }

        public Pitch(int pitch)
        {
            Distance = pitch;
        }

        public override string ToString()
        {
            return Distance.ToString("#0.00");
        }

        public override int GetHashCode()
        {
            return Distance.GetHashCode();
        }

        public static Pitch TryParse(string pitch)
        {
            Regex query = new Regex("([^0-9.-])");
            string cleaned = query.Replace(pitch, "");

            float distance = 0f;
            float.TryParse(cleaned, out distance);

            return new Pitch(distance);
        }

        public override bool Equals(object obj)
        {
            if (obj is Pitch)
                return Distance.RoughEquals((obj as Pitch).Distance, SIGMA);
            if (obj is float)
                return Distance.RoughEquals((float)obj, SIGMA);
            if (obj is double)
                return Distance.RoughEquals((double)obj, SIGMA);
            if (obj is decimal)
                return ((decimal)Distance).Equals((decimal)obj);
            if (obj.IsNumber())
                return Distance.RoughEquals((long)obj, SIGMA);
            return false;
        }

        public static bool operator ==(Pitch a, Pitch b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static bool operator !=(Pitch a, Pitch b)
        {
            return !(a == b);
        }
    }
}
