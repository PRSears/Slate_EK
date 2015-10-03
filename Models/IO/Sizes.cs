using Extender.IO;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml.Serialization;

namespace Slate_EK.Models.IO
{
    public class Sizes : SerializedArray<Size>
    {
        public Sizes()
        {
        }

        public Sizes(Size[] sourceList) : this()
        {
            SourceList = sourceList;
        }

        public override void Reload()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Load)
            );
        }

        public override void Save()
        {
            FileInfo xmlFile = new FileInfo(FilePath);

            SizesXmlOperationsQueue.Enqueue
            (
                new SerializeTask<Size>(xmlFile, this, SerializeOperations.Save)
            );
        }

        #region TestHarnesses
        public static void TestHarness()
        {
            Test_GenericImplementation();
        }

        private static void Test_Deserialze()
        {
            Sizes loaded = new Sizes();
            loaded.Reload();

            Extender.Debugging.Debug.WriteMessage("Deserialized: ");
            System.Threading.Thread.Sleep(100);

            foreach (var item in loaded.SourceList)
                Console.Write(item.OuterDiameter + @", ");
        }

        private static void Test_ArrayVsList()
        {
            // Perform 10,000 operations & time it
            Stopwatch timer = new Stopwatch();

            timer.Start();
            for (int x = 0; x < 500; x++)
            {
                // Create the initial SourceList
                Size[] sourceList = new Size[250];
                for (int i = 0; i < sourceList.Length; i++)
                    sourceList[i] = new Size(i);

                // Add a value to it
                Size[] appendedList = new Size[sourceList.Length + 1];
                Array.Copy(sourceList, appendedList, sourceList.Length);
                appendedList[sourceList.Length] = new Size(250);
                sourceList = appendedList;

                // Serialize
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Size));
                    foreach (Size size in sourceList)
                        xml.Serialize(stream, size);
                }

                if (x % 100 == 0) // rather hacky solution to lessen OutOfMemory errors
                {
                    timer.Stop();
                    GC.Collect();
                    timer.Start();
                }
            }
            timer.Stop();
            Extender.Debugging.Debug.WriteMessage
            (
                $"Array: {timer.ElapsedMilliseconds}ms",
                "info"
            );

            System.Threading.Thread.Sleep(2000);
            GC.Collect();

            timer.Restart();
            for (int x = 0; x < 500; x++)
            {
                // Create the initial SourceList
                List<Size> sourceList = new List<Size>();
                for (int i = 0; i < 250; i++)
                    sourceList.Add(new Size(i));

                // Add a value to it
                sourceList.Add(new Size(250));

                // Serialize
                using (MemoryStream stream = new MemoryStream())
                {
                    XmlSerializer xml = new XmlSerializer(typeof(Size));
                    foreach (Size size in sourceList)
                        xml.Serialize(stream, size);
                }

                if (x % 100 == 0) // rather hacky solution to lessen OutOfMemory errors
                {
                    timer.Stop();
                    GC.Collect();
                    timer.Start();
                }
            }
            timer.Stop();
            Extender.Debugging.Debug.WriteMessage
            (
                $"List : {timer.ElapsedMilliseconds}ms",
                "info"
            );
        }

        private static void Test_Serialization()
        {
            Size[] dummies = new Size[15];
            // Makeup some sizes
            for (int i = 0; i < 10; i++)
            {
                dummies[i] = new Size(i);
            }
            dummies[10] = new Size(6.75d);
            dummies[11] = new Size(8.25d);
            dummies[12] = new Size(4.33333d);
            dummies[13] = new Size(9.115d);
            dummies[14] = new Size(20.000000005d);

            Sizes list = new Sizes(dummies);


            XmlSerializer s = new XmlSerializer(typeof(Sizes));

            using (FileStream stream = new FileStream(list.FilePath, FileMode.Create, FileAccess.Write))
            {
                s.Serialize(stream, list);
            }
        }

        private static void Test_GenericImplementation()
        {
            Size[] dummies = new Size[20];

            for(int i = 0; i < dummies.Length; i++)
            {
                dummies[i] = new Size(i);
            }

            Sizes tester = new Sizes(dummies);

            tester.Save();
        }

        #endregion

        #region Settings.Settings aliases
        public string PropertiesPath => Properties.Settings.Default.DefaultPropertiesFolder;

        public string Filename       => Properties.Settings.Default.SizesFilename;

        #endregion

        public override string FilePath => Path.Combine(PropertiesPath, Filename);
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
    }
}
