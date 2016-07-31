using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    public class ForeignKeyDependentTables
    {
        /// <summary>
        /// Schema of the referenced table
        /// </summary>
        public string SelfSchemaName { get; set; }
        /// <summary>
        /// TableName fo the referenced table
        /// </summary>
        public string SelfTableName { get; set; }
        /// <summary>
        /// Referenced Column Name
        /// </summary>
        public string SelfColumnName { get; set; }
        /// <summary>
        /// Foreign Key Schema Name
        /// </summary>
        public string ForeignSchemaName { get; set; }
        /// <summary>
        /// ForeignKey Table Name
        /// </summary>
        public string ForeignTableName { get; set; }
        /// <summary>
        /// Foreign Column
        /// </summary>
        public string ForeignColumnName { get; set; }
    }
}
