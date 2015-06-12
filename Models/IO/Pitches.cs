using Extender.IO;
using System.Collections.Concurrent;
using System.IO;
using System.Timers;

namespace Slate_EK.Models.IO
{
    public class Pitches : SerializedArray<Pitch>
    {
        public override string FilePath
        {
            get { return Path.Combine(PropertiesPath, Filename); }
        }

        public Pitches()
        {
        }

        public Pitches(Models.Pitch[] sourceList) : this()
        {
            this.SourceList = sourceList;
        }

        public override void Reload()
        {
            FileInfo xmlFile = new FileInfo(this.FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(this.FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Save)
            );
        }

        #region Settings.Settings aliases
        public string Filename
        {
            get
            {
                return Properties.Settings.Default.PitchesFilename;
            }
        }

        public string PropertiesPath
        {
            get
            {
                return Properties.Settings.Default.DefaultPropertiesFolder;
            }
        }
        #endregion
    }

    /// <summary>
    /// Use a static constructor and blocking collection to handle reads/writes to XML file
    /// to minimize IOExceptions and avoid blocking the main thread.
    /// </summary>
    static class PitchesXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<Pitch>> TaskQueue = new BlockingCollection<SerializeTask<Pitch>>();

        static PitchesXmlOperationsQueue()
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

        public static void Enqueue(SerializeTask<Pitch> operation)
        {
            TaskQueue.Add(operation);
        }
    }
}
