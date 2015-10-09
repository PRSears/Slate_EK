using System;
using System.Linq;

namespace Slate_EK.Models
{
    public class FastenerType
    {
        public static FastenerType SocketHeadFlatScrew              => new FastenerType("socket head flat screw");

        public static FastenerType SocketCountersunkHeadCapScrew    => new FastenerType("socket countersunk head cap screw");

        public static FastenerType LowHeadSocketHeadCapScrew        => new FastenerType("low-head socket head cap screw");

        public static FastenerType FlatCountersunkHeadCapScrew      => new FastenerType("flat countersunk head cap screw");

        public static FastenerType Unspecified                      => new FastenerType("unspecified");

        public static FastenerType[] Types => new[] 
        {
            SocketHeadFlatScrew, 
            SocketCountersunkHeadCapScrew, 
            LowHeadSocketHeadCapScrew,
            FlatCountersunkHeadCapScrew
        };

        public string Type { get; }

        private FastenerType(string familyType)
        {
            Type = familyType;
        }

        public string Callout
        {
            get
            {
                return new string
                (
                    Type.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(c => c[0])
                         .ToArray()
                ).ToUpper();
            }
        }

        public static FastenerType Parse(string fastenerType)
        {
            // Check for exact _FamilyType match
            foreach (FastenerType f in Types)
            {
                if (fastenerType.ToLower().Equals(f.Type.ToLower()))
                    return f;
            }

            // Check for exact Callout match
            foreach (FastenerType f in Types)
            {
                if (f.Callout.ToLower().Equals(fastenerType.ToLower()))
                    return f;
            }

            // Fuzzy check for _FamilyType match
            foreach (FastenerType f in Types)
            {
                if (f.Type.ToLower().Contains(fastenerType.ToLower()))
                    return f;
            }

            // Fuzzy Callout match
            foreach (FastenerType f in Types)
            {
                if (fastenerType.ToLower().Contains(f.Callout.ToLower()))
                    return f;
            }

            throw new ArgumentException
            (
                $@"Specified string ""{fastenerType}"" was not a valid FastenerType"
            );
        }
        
        /// <returns>Returns FastenerType.Unspecified if a parse did not succeed.</returns>
        public static FastenerType TryParse(string fastenerType)
        {
            try
            {
                return Parse(fastenerType);
            }
            catch (ArgumentException e)
            {
                Extender.Debugging.ExceptionTools.WriteExceptionText(e, true);
                return FastenerType.Unspecified;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FastenerType))
                return false;

            return ((FastenerType)obj).Type.ToLower().Equals(Type.ToLower());
        }

        public override string ToString()
        {
            return Callout;
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }
    }
}
