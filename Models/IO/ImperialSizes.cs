using Extender.IO;
using Slate_EK.Models.ThreadParameters;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Slate_EK.Models.IO
{
    public sealed class ImperialSizes : SerializedArray<UnifiedThreadStandard>
    {
        public ImperialSizes()
        {
            
        }

        private ImperialSizes(UnifiedThreadStandard[] sourceList)
        {
            SourceList = sourceList;
        }

        public override async Task ReloadAsync()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            ImperialSizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<UnifiedThreadStandard>(xmlFile, this, SerializeOperations.Load)
            );

            await Task.Run
            (
                () =>
                {
                    while (!ImperialSizesXmlOperationsQueue.IsCompleted() || SourceList == null)
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            );
        }

        public override async Task SaveAsync()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            ImperialSizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<UnifiedThreadStandard>(xmlFile, this, SerializeOperations.Save)
            );

            await Task.Run
            (
                () =>
                {
                    while (!ImperialSizesXmlOperationsQueue.IsCompleted())
                    {
                        System.Threading.Thread.Sleep(10);
                    }
                }
            );
        } 

        public override string FilePath => Path.Combine
        (
            Properties.Settings.Default.DefaultPropertiesFolder,
            Properties.Settings.Default.ImperialSizesFilename
        );
    }

    public static class ImperialSizesCache
    {
        public static UnifiedThreadStandard[] Table;

        public static bool IsBuilt()
        {
            return Table != null;
        }

        static ImperialSizesCache()
        {
            ImperialSizes table = new ImperialSizes();

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
    static class ImperialSizesXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<UnifiedThreadStandard>> _TaskQueue = new BlockingCollection<SerializeTask<UnifiedThreadStandard>>();

        static ImperialSizesXmlOperationsQueue()
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

        public static void Enqueue(SerializeTask<UnifiedThreadStandard> operation)
        {
            _TaskQueue.Add(operation);
        }

        public static bool IsCompleted()
        {
            return _TaskQueue.Count < 1;
        }
    }
}
