using Extender.IO;
using Slate_EK.Models.ThreadParameters;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Slate_EK.Models.IO
{
    public sealed class Pitches : SerializedArray<Pitch>
    {
        public Pitches()
        {
        }

        public Pitches(Pitch[] sourceList) : this()
        {
            SourceList = sourceList;
        }

        /// <summary>
        /// When overridden in a derived class, queues a SerializeTask to reload the SourceList. Should be non-blocking.
        /// </summary>
        public override void QueueReload()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Load)
            );
        }

        /// <summary>
        /// When overridden in a derived class, queues a SerializeTask to save the SourceList. Should be non-blocking.
        /// </summary>
        public override void QueueSave()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            PitchesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Pitch>(xmlFile, this, SerializeOperations.Save)
            );
        }

        public override async Task ReloadAsync()
        {
            QueueReload();

            await Task.Run
            (
                () =>
                {
                    while (!PitchesXmlOperationsQueue.IsCompleted() || SourceList == null)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            );
        }

        public override async Task SaveAsync()
        {
            QueueSave();

            await Task.Run
            (
                () =>
                {
                    while (!PitchesXmlOperationsQueue.IsCompleted())
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            );
        }

        public override string FilePath => Path.Combine
        (
            Properties.Settings.Default.DefaultPropertiesFolder,
            Properties.Settings.Default.PitchesFilename
        );
    }
    
    public static class PitchesCache
    {
        public static Pitch[] Table;

        public static bool IsBuilt()
        {
            return Table != null;
        }

        static PitchesCache()
        {
            Pitches table = new Pitches();

            Task.Run(async () =>
            {
                await table.ReloadAsync();
                Table = table.SourceList.ToArray();
            });
        }
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
                    while (true)
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

        public static bool IsCompleted()
        {
            return _TaskQueue.Count < 1;
        }
    }
}
