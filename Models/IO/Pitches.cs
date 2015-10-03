using Extender.IO;
using System.Collections.Concurrent;
using System.IO;

namespace Slate_EK.Models.IO
{
    public class Pitches : SerializedArray<Pitch>
    {
        public override string FilePath => Path.Combine(PropertiesPath, Filename);

        public Pitches()
        {
        }

        public Pitches(Pitch[] sourceList) : this()
        {
            SourceList = sourceList;
        }

        public override void Reload()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Save)
            );
        }

        #region Settings.Settings aliases
        public string Filename       => Properties.Settings.Default.PitchesFilename;

        public string PropertiesPath => Properties.Settings.Default.DefaultPropertiesFolder;

        #endregion
    }

    /// <summary>
    /// Use a static constructor and blocking collection to handle reads/writes to XML file
    /// to minimize IOExceptions and avoid blocking the main thread.
    /// </summary>
    static class PitchesXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<Pitch>> _TaskQueue = new BlockingCollection<SerializeTask<Pitch>>();

        static PitchesXmlOperationsQueue()
        {
            var thread = new System.Threading.Thread
            (
                () =>
                {
                    while(true)
                    {
                        _TaskQueue.Take().Execute();
                    }
                }
            );

            thread.IsBackground = true;
            thread.Start();
        }

        public static void Enqueue(SerializeTask<Pitch> operation)
        {
            _TaskQueue.Add(operation);
        }
    }
}
