using Extender.IO;
using Slate_EK.Models.ThreadParameters;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Slate_EK.Models.IO
//TODO We might have to bake units into the size (and maybe pitch) lists / objects
//     could store them as a separate field?
{
    public sealed class Sizes : SerializedArray<Size>
    {
        public Sizes()
        {
        }

        public Sizes(Size[] sourceList) : this()
        {
            SourceList = sourceList;
        }

        public override async Task ReloadAsync()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Load)
            );

            await Task.Run
            (
                () =>
                {
                    while (!SizesXmlOperationsQueue.IsCompleted() || SourceList == null)
                        System.Threading.Thread.Sleep(10);
                }
            );
        }

        public override async Task SaveAsync()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Save)
            );

            await Task.Run
            (
                () =>
                {
                    while (!SizesXmlOperationsQueue.IsCompleted())
                        System.Threading.Thread.Sleep(10);
                }
            );
        }

        public override string FilePath => Path.Combine
        (
            Properties.Settings.Default.DefaultPropertiesFolder,
            Properties.Settings.Default.SizesFilename
        );
    }

    public static class SizesCache 
    {
        public static Size[] Table;

        public static bool IsBuilt()
        {
            return Table != null;
        }

        static SizesCache()
        {
            Sizes table = new Sizes();

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
    static class SizesXmlOperationsQueue
    {
        private static BlockingCollection<SerializeTask<Size>> _TaskQueue = new BlockingCollection<SerializeTask<Size>>();

        static SizesXmlOperationsQueue()
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

        public static void Enqueue(SerializeTask<Size> operation)
        {
            _TaskQueue.Add(operation);
        }

        public static bool IsCompleted()
        {
            return _TaskQueue.Count < 1;
        }
    }
}
