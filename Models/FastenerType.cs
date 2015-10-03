using System;
using System.ComponentModel;
using System.Linq;

namespace Slate_EK.Models
{
    public class FastenerType : INotifyPropertyChanged
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

        private string _Type;

        public string Type => _Type;

        private FastenerType(string familyType)
        {
            _Type = familyType;
        }

        public string Callout
        {
            get
            {
                return new string
                (
                    _Type.Split(new[] { ' ', '-' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(c => c[0])
                         .ToArray()
                ).ToUpper();
            }
        }

        public static FastenerType Parse(string familyType)
        {
            // Check for exact _FamilyType match
            foreach (FastenerType f in Types)
            {
                if (familyType.ToLower().Equals(f._Type.ToLower()))
                    return f;
            }

            // Check for exact Callout match
            foreach (FastenerType f in Types)
            {
                if (f.Callout.ToLower().Equals(familyType.ToLower()))
                    return f;
            }

            // Fuzzy check for _FamilyType match
            foreach (FastenerType f in Types)
            {
                if (f._Type.ToLower().Contains(familyType.ToLower()))
                    return f;
            }

            // Fuzzy Callout match
            foreach (FastenerType f in Types)
            {
                if (familyType.ToLower().Contains(f.Callout.ToLower()))
                    return f;
            }

            throw new ArgumentException
            (
                $@"Specified string ""{familyType}"" was not a valid FamilyType"
            );
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FastenerType))
                return false;

            return ((FastenerType)obj)._Type.ToLower().Equals(_Type.ToLower());
        }

        public override string ToString()
        {
            return Callout;
        }

        public override int GetHashCode()
        {
            return _Type.GetHashCode();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}
