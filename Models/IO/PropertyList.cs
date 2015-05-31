using Extender;
using System;
using System.IO;
using System.Xml.Serialization;

namespace Slate_EK.Models.IO
{
    public abstract class PropertyList<T>
    {
        public    T[]       SourceList { get; set; }
        protected string    FilePath;
        protected bool      SurpressAutoRefresh;


        public virtual void Add(T item)
        {
            SurpressAutoRefresh = true;
            Reload();

            T[] appendedList = new T[SourceList.Length + 1];

            Array.Copy(SourceList, appendedList, SourceList.Length);
            appendedList[SourceList.Length] = item;

            SourceList = appendedList;
            Save();

            SurpressAutoRefresh = false;
        }

        public virtual bool Remove(T item)
        {
            T[] ammendedList = new T[SourceList.Length - 1];

            try
            {
                int p = 0;
                for(int i = 0; i < SourceList.Length; i++)
                {
                    if (!SourceList[i].Equals(item))
                    {
                        ammendedList[p] = SourceList[i];
                        p++;
                    }
                }

                SourceList = ammendedList;
                return true;
            }
            catch(IndexOutOfRangeException)
            {
                Extender.Debugging.Debug.WriteMessage
                (
                    string.Format("item ({0}) could not be removed from SourceList because it did not exist.", item.ToString()),
                    "warn"
                );
                return false; // 'item' was never found
            }
        }

        public virtual void Reload()
        {
            if (!File.Exists(FilePath))
                return;

            using(FileStream stream = new FileStream(FilePath, FileMode.Open, FileAccess.Read))
            {
                XmlSerializer xml = new XmlSerializer(this.GetType());

                this.UpdateFrom<PropertyList<T>>
                (
                    (PropertyList<T>)xml.Deserialize(stream)
                );
            }
        }

        public virtual void Save()
        {
            using(FileStream stream = new FileStream(FilePath, FileMode.Create, FileAccess.Write))
            {
                XmlSerializer xml = new XmlSerializer(this.GetType());

                xml.Serialize(stream, this);
            }
        }

        /// <summary>
        /// Calls Array.Sort on the Source List.
        /// </summary>
        public virtual void Sort()
        {
            Array.Sort(SourceList);
        }
    }
}
