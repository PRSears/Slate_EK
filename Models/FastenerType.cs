﻿using System;
using System.ComponentModel;
using System.Linq;

namespace Slate_EK.Models
{
    public class FastenerType : INotifyPropertyChanged
    {
        public static FastenerType SocketHeadFlatScrew
        {
            get
            {
                return new FastenerType("socket head flat screw");
            }
        }

        public static FastenerType SocketCountersunkHeadCapScrew
        {
            get
            {
                return new FastenerType("socket countersunk head cap screw");
            }
        }

        public static FastenerType LowHeadSocketHeadCapScrew
        {
            get
            {
                return new FastenerType("low-head socket head cap screw");
            }
        }

        public static FastenerType[] Types
        {
            get
            {
                return new FastenerType[] 
                {
                    SocketHeadFlatScrew, 
                    SocketCountersunkHeadCapScrew, 
                    LowHeadSocketHeadCapScrew 
                };
            }
        }

        private string _FamilyType;

        private FastenerType(string familyType)
        {
            this._FamilyType = familyType;
        }

        public string Callout
        {
            get
            {
                return new string
                (
                    _FamilyType.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(c => c[0])
                               .ToArray()
                ).ToUpper();
            }
        }

        public static FastenerType Parse(string familyType)
        {
            // Check for exact _FamilyType match
            foreach(FastenerType f in Types)
            {
                if(familyType.ToLower().Equals(f._FamilyType.ToLower()))
                    return f;
            }

            // Check for exact Callout match
            foreach(FastenerType f in Types)
            {
                if (f.Callout.ToLower().Equals(familyType.ToLower()))
                    return f;
            }

            // Fuzzy check for _FamilyType match
            foreach(FastenerType f in Types)
            {
                if (f._FamilyType.ToLower().Contains(familyType.ToLower()))
                    return f;
            }

            // Fuzzy Callout match
            foreach(FastenerType f in Types)
            {
                if (f.Callout.ToLower().Contains(familyType.ToLower()))
                    return f;
            }

            throw new ArgumentException
            (
                string.Format(@"Specified string ""{0}"" was not a valid FamilyType", familyType)
            );
        }

        public override bool Equals(object obj)
        {
            if (!(obj is FastenerType))
                return false;

            return (obj as FastenerType)._FamilyType.ToLower().Equals(this._FamilyType.ToLower());
        }

        public override string ToString()
        {
            return _FamilyType;
        }

        public override int GetHashCode()
        {
            return _FamilyType.GetHashCode();
        }

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}