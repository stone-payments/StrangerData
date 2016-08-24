using StrangerData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    public class ForeignKeyDependentTableItem 
    {
        public string itemId { get; set; } 

        public ForeignKeyDependentTables[] ForeignKeyDependentTableList { get; set; }
    }
}
