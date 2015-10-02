using System.Data.Linq;

namespace Slate_EK.Models.Inventory
{
    public class InventoryDataContext : DataContext
    {
        public Table<FastenerTableLayer> Fasteners;

        public InventoryDataContext(string ConnectionString) : base (ConnectionString)
        {

        }
    }
}
