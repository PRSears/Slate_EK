using Extender;
using System;
using System.ComponentModel;

namespace Slate_EK.Models
{
    public class HoleType : INotifyPropertyChanged
    {
        private string _HoleType;

        public bool IsCounterBore
        {
            get
            {
                return this._HoleType.ToLower().Equals(CBore._HoleType.ToLower());
            }
        }

        private HoleType(string holeType)
        {
            this._HoleType = holeType;
        }

        public static HoleType[] HoleTypes
        {
            get
            {
                return new HoleType[] { CBore, Straight, CSink };
            }
        }

        public static HoleType CBore
        {
            get
            {
                return new HoleType("cbore");
            }
        }

        public static HoleType CSink
        {
            get
            {
                return new HoleType("csink");
            }
        }

        public static HoleType Straight
        {
            get
            {
                return new HoleType("straight");
            }
        }

        public static HoleType Parse(string holeType)
        {
            foreach (HoleType t in HoleTypes)
                if (t._HoleType.ToLower().Equals(holeType.ToLower()))
                    return t;

            // try to find partial match
            foreach (HoleType t in HoleTypes)
            {
                if (t._HoleType.ToLower().Contains(holeType.ToLower()))
                {
                    Extender.Debugging.Debug.WriteMessage
                    (
                        string.Format("HoleType.Parse found only a partial match. {0} -> {1}", holeType, t.ToString()),
                        "warn"
                    );

                    return t;
                }
            }

            throw new ArgumentException(string.Format(@"HoleType name ""{0}"" is not a valid HoleType.", holeType));
        }


        public override string ToString()
        {
            return _HoleType.ToPropercase();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HoleType))
                return false;

            return (obj as HoleType)._HoleType.ToLower().Equals(this._HoleType.ToLower());
        }

        public override int GetHashCode()
        {
            return this._HoleType.GetHashCode();
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
