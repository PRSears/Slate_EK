﻿using Extender.Debugging;
using Extender.IO;
using Slate_EK.Models.ThreadParameters;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Slate_EK.Models.IO
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

        public override void QueueReload()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void QueueSave()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Save)
            );
        }

        public override async Task ReloadAsync()
        {
            QueueReload();

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
            QueueSave();

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
                Debug.WriteMessage(IsBuilt() ? "Sizes cache built." : "Building Sizes cache.", "info");
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
