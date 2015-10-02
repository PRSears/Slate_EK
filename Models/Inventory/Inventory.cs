using Extender.Debugging;
using System;
using System.Data.Linq;
using System.IO;
using System.Linq;

namespace Slate_EK.Models.Inventory
{
    public class Inventory : IDisposable
    {
        protected InventoryDataContext  Database;
        protected string                Filename;
        
        public string InventoryConnectionString
        {
            get
            {
                return $"Data Source=(LocalDB)\\v11.0;AttachDbFilename={this.Filename};Integrated Security=False;Pooling=false;";
            }
        }

        public Table<FastenerTableLayer> Fasteners
        {
            get
            {
                if (Database == null)
                    return null;

                return Database.Fasteners;
            }
        }

        /// <summary>
        /// Initializes a new instance of the Inventory for the database at the specified path.
        /// </summary>
        /// <param name="filename">Full name and path of the inventory database being managed.</param>
        public Inventory(string filename)
        {
            Database = new InventoryDataContext(InventoryConnectionString);

            if (!Database.DatabaseExists())
                Database.CreateDatabase();
        }

        public void SubmitChanges()
        {
            Database.SubmitChanges();
        }

        public void Add(Fastener fastener)
        {
            Add((FastenerTableLayer)fastener);
        }

        public void Add(FastenerTableLayer fastener)
        {
            if (Fasteners.Count((f) => f.UniqueID.Equals(fastener.UniqueID)) < 1)
            {
                Database.Fasteners.InsertOnSubmit(fastener);
            }
            // TODO If the fastener is already in the db, then check to see if the quantity 
            //      matches that of the one we're trying to add. If not, call Replace()
        }

        public Fastener Pull(Guid fastenerID)
        {
            return (Fastener)Database.Fasteners.FirstOrDefault((f) => f.UniqueID.Equals(fastenerID));
        }

        public void Replace(Fastener inDatabase, Fastener replacement)
        {
            Replace((FastenerTableLayer)inDatabase, (FastenerTableLayer)replacement);
        }

        public void Replace(FastenerTableLayer inDatabase, FastenerTableLayer replacement)
        {
            if (Fasteners.Count((f) => f.UniqueID.Equals(inDatabase.UniqueID)) > 0)
            {
                Remove(inDatabase);
            }

            Add(replacement);
        }

        public void Remove(Fastener fastener)
        {
            Remove(new FastenerTableLayer[] { (FastenerTableLayer)fastener });
        }

        public void Remove(FastenerTableLayer fastener)
        {
            Remove(new FastenerTableLayer[] { fastener });
        }

        public void Remove(FastenerTableLayer[] fasteners)
        {
            var matches = Database.Fasteners.Where
            ( 
                (inTable) => fasteners.Select( (f) => f.UniqueID)
                                      .Contains(inTable.UniqueID)
            );

            if (matches.Count() < 1)
            {
                Debug.WriteMessage("Remove could not find any matching items in the database.", "info");
                return;
            }

            Database.Fasteners.DeleteAllOnSubmit(matches);
        }

        public void Export(string filename)
        {
            Fastener[] allFasteners = Dump();

            Extender.IO.CsvSerializer<Fastener> csv = new Extender.IO.CsvSerializer<Fastener>();

            using (FileStream stream = new FileStream(filename, FileMode.OpenOrCreate,
                                                                FileAccess.ReadWrite,
                                                                FileShare.Read))
            {
                csv.Serialize(stream, allFasteners);
            }
        }

        public Fastener[] Dump()
        {
            Fastener[] dumped = new Fastener[Fasteners.Count()];

            int i = 0;
            foreach(FastenerTableLayer f in Fasteners)
            {
                dumped[i++] = (Fastener)f; // explicit cast operator performs a shallow copy
            }

            return dumped;
        }








        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    //  dispose managed state (managed objects).
                    Database.SubmitChanges(); // THOUGHT Make sure I should actually do this
                    Database.Dispose();
                }

                //  free unmanaged resources (unmanaged objects) and override a finalizer below.
                //  set large fields to null.

                disposedValue = true;
            }
        }

        //  override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Archive() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion

    }
}
