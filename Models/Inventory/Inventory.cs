using Extender.Debugging;
using Extender.IO;
using System;
using System.Data.Linq;
using System.Linq;

namespace Slate_EK.Models.Inventory
{
    public sealed class Inventory : IDisposable
    {
        private InventoryDataContext _Database;
        public readonly string        Filename;

        private string InventoryConnectionString   => $"Data Source=(LocalDB)\\v11.0;AttachDbFilename={Filename};Integrated Security=False;Pooling=false;";
        public Table<UnifiedFastener> Fasteners    => _Database?.Fasteners;

        /// <summary>
        /// Initializes a new instance of the Inventory for the database at the specified path.
        /// </summary>
        /// <param name="filename">Full name and path of the inventory database being managed.</param>
        public Inventory(string filename)
        {
            Filename = filename;

            InitDataContext();

            if (!_Database.DatabaseExists())
                _Database.CreateDatabase(); 
        }

        public void DEBUG_DropDatabase()
        {
            _Database.DeleteDatabase();
        }

        public void SubmitChanges()
        {
            _Database.SubmitChanges();
        }

        public void InitDataContext()
        {
            _Database?.Dispose();
            _Database     = new InventoryDataContext(InventoryConnectionString);
            _Database.Log = new ActionTextWriter
            (
                (text) =>
                {
                    if (!string.IsNullOrWhiteSpace(text))
                        Debug.WriteMessage($" => \n{text}", "SQL");
                }
            );
        }

        public void Add(UnifiedFastener fastener)
        {
            fastener.ForceNewUniqueID();

            var inTable = Fasteners.FirstOrDefault(f => f.UniqueID.Equals(fastener.UniqueID));

            if (inTable != null && !inTable.Equals(default(UnifiedFastener)))
            {
                // It was in the table
                if (inTable.Quantity != fastener.Quantity) // Only replace if the quantities are different
                {
                    _Database.Fasteners.DeleteOnSubmit(inTable);
                    _Database.Fasteners.InsertOnSubmit(fastener);
                }
            }
            else
            {
                // It was not in the table
                _Database.Fasteners.InsertOnSubmit(fastener);
            }
        }

        public bool Remove(UnifiedFastener fastener)
        {
            return Remove(new[] { fastener }) > 0;
        }

        public int Remove(UnifiedFastener[] fasteners)
        {
            var matches = _Database.Fasteners.Where
            ( 
                inTable => fasteners.Select(f => f.UniqueID)
                                    .Contains(inTable.UniqueID)
            );

            if (!matches.Any())
            {
                Debug.WriteMessage("Remove could not find any matching items in the database.", "info");
                return 0;
            }

            _Database.Fasteners.DeleteAllOnSubmit(matches);

            return matches.Count();
        }

        public UnifiedFastener[] Dump()
        {
            UnifiedFastener[] dumped = new UnifiedFastener[Fasteners.Count()];

            _Database.Refresh(RefreshMode.OverwriteCurrentValues, Fasteners);

            int i = 0;
            foreach (UnifiedFastener f in Fasteners)
            {
                dumped[i++] = f; // explicit cast operator performs a shallow copy
            }
            
            return dumped;
        }

        #region IDisposable Support
        private bool _DisposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!_DisposedValue)
            {
                if (disposing)
                {
                    //  dispose managed state (managed objects).
                    _Database.Dispose();
                }

                //  free unmanaged resources (unmanaged objects) and override a finalizer below.
                //  set large fields to null.

                _DisposedValue = true;
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
