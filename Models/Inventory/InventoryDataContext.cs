using System.Data.Linq;

namespace Slate_EK.Models.Inventory
{
    public class InventoryDataContext : DataContext
    {
        public Table<UnifiedFastener> Fasteners;

        public InventoryDataContext(string connectionString) : base (connectionString)
        {

        }
    }
}
