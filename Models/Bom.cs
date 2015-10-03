﻿using Extender.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Slate_EK.Models
{
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
                new SerializeTask<Fastener>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            BomXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Fastener>(xmlFile, this, SerializeOperations.Save)
            );
        }

        public override void Add(Fastener item)
        {
            if (SourceList != null && SourceList.Contains(item))
            {
                SourceList.First(f => f.Id.Equals(item.Id))
                          .Quantity += item.Quantity;
                Sort();
                Save();
            }
            else
            {
                base.Add(item);
                Sort();
                Save(); // THOUGHT This makes base.Add()'s Save redundant.
            }
        }

        public override bool Remove(Fastener item)
        {
            return Remove(item, 1);
        }

        public bool Remove(Fastener item, int quantity)
        {
            item.GetNewId();

            if (SourceList != null && SourceList.Contains(item))
            {
                if ((SourceList.First(f => f.Id.Equals(item.Id))
                               .Quantity -= quantity) <= 0)
                {
                    base.Remove(item);
                }

                Sort();
                Save();
                return true;
            }
            else return false;
        }

        public override void Sort()
        {
            List<Fastener> sorted = new List<Fastener>();

            foreach (var typeGroup in SourceList.OrderBy(f => f.Length).GroupBy(f => f.TypeString))
            {
                sorted.AddRange(typeGroup.OrderBy(f => f.SizeString));
            }

            SourceList = sorted.ToArray();
        }

        #region operators & overrides

        public override bool Equals(object obj)
        {
            if (!(obj is Bom))
                return false;

            return GetHashCode() == (obj as Bom).GetHashCode();

        }

        public override int GetHashCode()
        {
            List<byte[]> blocks = new List<byte[]> {System.Text.Encoding.Default.GetBytes(AssemblyNumber)};

            foreach (Fastener f in SourceList)
            {
                if (f != null)
                blocks.Add(f.GetHashData());
            }

            return BitConverter.ToInt32(Extender.ObjectUtils.Hashing.GenerateHashCode(blocks), 0);
        }

        public override string ToString()
        {
            return $"Assembly #{AssemblyNumber} [{SourceList.Length} Fasteners]";
        }

        public static bool operator ==(Bom a, Bom b)
        {
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

        private string Filename => string.Format
        (
            Properties.Settings.Default.BomFilenameFormat,
            AssemblyNumber
        );

        #endregion
    }

    static class BomXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<Fastener>> _TaskQueue = new BlockingCollection<SerializeTask<Fastener>>();

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

        public static void Enqueue(SerializeTask<Fastener> operation)
        {
            _TaskQueue.Add(operation);
        }
    }
}
