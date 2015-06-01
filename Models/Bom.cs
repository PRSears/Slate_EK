using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Slate_EK.Models
{
    // TODOh Bom could implement SerializedArray as well
    //       That would make save-as-you-type much less prone to error
    public class Bom : INotifyPropertyChanged
    {
        public string           AssemblyNumber
        {
            get
            {
                return _AssemblyNumber;
            }
            set
            {
                _AssemblyNumber = value;
                OnPropertyChanged("AssemblyNumber");
            }
        }
        public List<Fastener>   Fasteners
        {
            get
            {
                return _Fasteners;
            }
            set
            {
                _Fasteners = value;
                OnPropertyChanged("Fasteners");
            }
        }


        private string          _AssemblyNumber;
        private List<Fastener>  _Fasteners;

        public Bom() : this("none")
        {

        }

        public Bom(string assemblyNumber) : this(assemblyNumber, new List<Fastener>())
        {
            this.AssemblyNumber = assemblyNumber;
        }

        public Bom(string assemblyNumber, List<Fastener> fasteners)
        {
            this.AssemblyNumber = assemblyNumber;
            this.Fasteners      = fasteners;
        }

        # region operators & overrides
        public override bool Equals(object obj)
        {
            if (!(obj is Bom))
                return false;

            return this.GetHashCode() == (obj as Bom).GetHashCode();

        }

        public override int GetHashCode()
        {
            List<byte[]> blocks = new List<byte[]>();

            blocks.Add(System.Text.Encoding.Default.GetBytes(AssemblyNumber));
            foreach(Fastener f in Fasteners)
            {
                blocks.Add(f.GetHashData());
            }

            return BitConverter.ToInt32(Extender.ObjectUtils.Hashing.GenerateHashCode(blocks), 0);
        }

        public override string ToString()
        {
            return string.Format
            (
                "Assembly #{0} [{1} Fasteners]",
                AssemblyNumber,
                Fasteners.Count
            );
        }

        public static Boolean operator ==(Bom a, Bom b)
        {
            return a.Equals(b);
        }

        public static Boolean operator !=(Bom a, Bom b)
        {
            if (object.ReferenceEquals(null, a))
                return object.ReferenceEquals(null, b);

            return !(a == b);
        }
        #endregion

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
