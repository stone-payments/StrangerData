using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    public class RecordIdentifier
    {
        public string TableName { get; set; }

        public string ColumnName { get; set; }

        public object IdentifierValue { get; set; }
    }
}
