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

        public override string FilePath
        {
            get
            {
                return Path.Combine(FolderPath, Filename);
            }
        }

        public Bom() 
            : this("none")
        {

        }

        public Bom(string assemblyNumber)
        {
            this.AssemblyNumber = assemblyNumber;

            if(File.Exists(FilePath)) // there's an existing BOM with the same name
            {
                base.Reload();
                Sort();
            }
        }

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

        public override void Add(Fastener item)
        {
            if (SourceList != null && SourceList.Contains(item))
            {
                SourceList.First(f => f.ID.Equals(item.ID))
                          .Quantity++;
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
            item.GetNewID();

            if (SourceList != null && SourceList.Contains(item))
            {
                SourceList.First(f => f.ID.Equals(item.ID))
                          .Quantity--;
                Sort();
                Save();
                return true;
            }
            else
            {
                Sort();
                return base.Remove(item);
            }
        }

        public override void Sort()
        {
            List<Fastener> sorted = new List<Fastener>();

            foreach(var type_group in SourceList.OrderBy(f => f.Length).GroupBy(f => f.TypeString))
            {
                sorted.AddRange(type_group.OrderBy(f => f.SizeString));
            }

            SourceList = sorted.ToArray();
        }

        #region operators & overrides

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
                if(f != null)
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

        #region Settings.Settings aliases
        private string FolderPath
        {
            get
            {
                return Properties.Settings.Default.DefaultAssembliesFolder;
            }
        }
        private string Filename
        {
            get
            {
                return string.Format
                (
                    Properties.Settings.Default.BomFilenameFormat,
                    AssemblyNumber
                );
            }
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
