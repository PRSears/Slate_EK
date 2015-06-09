using Extender.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace Slate_EK.Models
{
    // TODOh Bom could implement SerializedArray as well
    //       That would make save-as-you-type much less prone to error
    public class Bom : SerializedArray<Fastener>
    {
        public string AssemblyNumber
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

        private string _AssemblyNumber;

        public Bom() : this("none")
        {

        }

        public Bom(string assemblyNumber) : this(assemblyNumber, new Fastener[] { new Fastener() })
        {
            this.AssemblyNumber = assemblyNumber;
        }

        public Bom(string assemblyNumber, Fastener[] fasteners)
        {
            this.AssemblyNumber = assemblyNumber;
            this.SourceList     = fasteners;
        }


        # region operators & overrides
        public override void Reload()
        {
            FileInfo xmlFile = new FileInfo(this.FilePath);

            BomXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Fastener>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(this.FilePath);

            BomXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Fastener>(xmlFile, this, SerializeOperations.Save)
            );
        }

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
            foreach(Fastener f in SourceList)
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
                SourceList.Length
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
    }

    static class BomXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<Fastener>> TaskQueue = new BlockingCollection<SerializeTask<Fastener>>();

        static BomXmlOperationsQueue()
        {
            var thread = new System.Threading.Thread
            (
                () =>
                {
                    while(true)
                    {
                        TaskQueue.Take().Execute();
                    }
                }
            );

            thread.IsBackground = true;
            thread.Start();
        }

        public static void Enqueue(SerializeTask<Fastener> operation)
        {
            TaskQueue.Add(operation);
        }
    }
}
