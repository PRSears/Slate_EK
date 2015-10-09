using Extender;
using System;
using System.ComponentModel;

namespace Slate_EK.Models
{
    public class HoleType : INotifyPropertyChanged
    {
        private readonly string _HoleType;

        public bool IsCounterBore => _HoleType.ToLower().Equals(CBore._HoleType.ToLower());

        private HoleType(string holeType)
        {
            _HoleType = holeType;
        }

        public static HoleType[] HoleTypes => new[] { CBore, Straight, CSink };

        public static HoleType CBore       => new HoleType("cbore");

        public static HoleType CSink       => new HoleType("csink");

        public static HoleType Straight    => new HoleType("straight");

        public static HoleType Unspecified => new HoleType("unspecified");

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
                        $"HoleType.Parse found only a partial match. {holeType} -> {t}",
                        "warn"
                    );

                    return t;
                }
            }

            throw new ArgumentException($@"HoleType name ""{holeType}"" is not a valid HoleType.");
        }

        /// <returns>Returns FastenerType.Unspecified if a parse did not succeed.</returns>
        public static HoleType TryParse(string holeType)
        {
            try
            {
                return Parse(holeType);
            }
            catch (ArgumentException e)
            {
                Extender.Debugging.ExceptionTools.WriteExceptionText(e, true);
                return Unspecified;
            }
        }


        public override string ToString()
        {
            return _HoleType.ToPropercase();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is HoleType))
                return false;

            return ((HoleType)obj)._HoleType.ToLower().Equals(_HoleType.ToLower());
        }

        public override int GetHashCode()
        {
            return _HoleType.GetHashCode();
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
