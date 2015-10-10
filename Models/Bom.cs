using Extender.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Slate_EK.Models
{
    public class Bom : SerializedArray<UnifiedFastener>
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
                OnPropertyChanged(nameof(AssemblyNumber));
            }
        }

        private string _AssemblyNumber;

        public override string FilePath => Path.Combine(FolderPath, Filename);

        public Bom() 
            : this("none")
        {

        }

        public Bom(string assemblyNumber)
        {
            AssemblyNumber = assemblyNumber;

            if (File.Exists(FilePath)) // there's an existing BOM with the same name
            {
                base.Reload();
                Sort();
            }
        }

        public override void Reload()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            BomXmlOperationsQueue.Enqueue
            (
                new SerializeTask<UnifiedFastener>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            BomXmlOperationsQueue.Enqueue
            (
                new SerializeTask<UnifiedFastener>(xmlFile, this, SerializeOperations.Save)
            );
        }

        public override void Add(UnifiedFastener item)
        {
            //if (SourceList != null && SourceList.Contains(item))
            //{
            //    SourceList.First(f => f.UniqueID.Equals(item.UniqueID))
            //              .Quantity += item.Quantity;
            //    Sort();
            //    Save();
            //}
            //else
            //{
            //    base.Add(item);
            //    Sort();
            //    Save(); // THOUGHT This makes base.Add()'s Save redundant.
            //}
            Add(new[] {item});
        }

        public void Add(UnifiedFastener[] fasteners)
        {
            if (SourceList == null || fasteners == null || fasteners.Length < 1) return;

            var uniqueFasteners = new List<UnifiedFastener>();
            throw new NotImplementedException();

            // TODOh Finish fixing Bom.Add()
            //
            // Make two lists => one with all the fasteners that don't appear in SourceList
            //                 & one with all which do
            // Iterate the pre-existing fasteners and update SourceLists' quantities
            // Pass unique.ToArray() to base.Add( ...[] )

            //foreach (var item in fasteners)
            //{
            //    if (SourceList.Contains(item))
            //    {
            //        SourceList.First(f => f.UniqueID.Equals(item.UniqueID))
            //                  .Quantity += item.Quantity;
            //    }
            //    else
            //    {
            //        base.Add(item);
            //    }
            //}

            Sort();
            Save();
        }

        public override bool Remove(UnifiedFastener item)
        {
            return Remove(item, 1);
        }

        public bool Remove(UnifiedFastener item, int quantityToRemove)
        {
            if (SourceList == null || !SourceList.Contains(item))
                return false;

            bool result = false;
            int  newQty = item.Quantity - quantityToRemove;

            if (newQty <= 0 || quantityToRemove == Int32.MaxValue)
                result = base.Remove(item);
            else
                result = (SourceList.First(i => i.UniqueID.Equals(item.UniqueID)).Quantity = newQty) > 0;

            Sort();
            Save();

            return result;
        }

        public override void Sort()
        {
            List<UnifiedFastener> sorted = new List<UnifiedFastener>();

            foreach (var typeGroup in SourceList.OrderBy(f => f.Length).GroupBy(f => f.Type))
            {
                sorted.AddRange(typeGroup.OrderBy(f => f.Size));
            }

            SourceList = sorted.ToArray();
        }

        #region operators & overrides

        public override bool Equals(object obj)
        {
            return GetHashCode() == (obj as Bom)?.GetHashCode();
        }

        public override int GetHashCode()
        {
            List<byte[]> blocks = new List<byte[]> {System.Text.Encoding.Default.GetBytes(AssemblyNumber)};

            blocks.AddRange
            (
                SourceList.Where(f => f != null)
                          .Select(f => f.GetHashData())
            );

            return BitConverter.ToInt32(Extender.ObjectUtils.Hashing.GenerateHashCode(blocks), 0);
        }

        public override string ToString()
        {
            return $"Assembly #{AssemblyNumber} [{SourceList.Length} Fasteners]";
        }

        public static bool operator ==(Bom a, Bom b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return a.Equals(b);
        }

        public static bool operator !=(Bom a, Bom b)
        {
            if (ReferenceEquals(null, a))
                return ReferenceEquals(null, b);

            return !(a == b);
        }
        #endregion

        #region Settings.Settings aliases
        private string FolderPath => Properties.Settings.Default.DefaultAssembliesFolder;
        private string Filename   => string.Format
        (
            Properties.Settings.Default.BomFilenameFormat,
            AssemblyNumber
        );
        #endregion
    }

    static class BomXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<UnifiedFastener>> _TaskQueue = new BlockingCollection<SerializeTask<UnifiedFastener>>();

        static BomXmlOperationsQueue()
        {
            var thread = new System.Threading.Thread
            (
                () =>
                {
                    while (true)
                    {
                        _TaskQueue.Take().Execute();
                    }
                }
            ) {IsBackground = true};

            thread.Start();
        }

        public static void Enqueue(SerializeTask<UnifiedFastener> operation)
        {
            _TaskQueue.Add(operation);
        }
    }
}
