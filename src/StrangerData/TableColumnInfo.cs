using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    public class TableColumnInfo
    {
        /// <summary>
        /// Column of the table.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Type of the column.
        /// </summary>
        public ColumnType ColumnType { get; set; }

        /// <summary>
        /// Precision of the column, for numeric types.
        /// </summary>
        public int Precision { get; set; }

        /// <summary>
        /// Scale of the column, for numeric types.
        /// </summary>
        public int Scale { get; set; }

        /// <summary>
        /// Max length of the column, for text types.
        /// </summary>
        public int MaxLength { get; set; }

        /// <summary>
        /// Indicates if the column value is database generated, identity or autoincrement for example.
        /// </summary>
        public bool IsIdentity { get; set; }

        /// <summary>
        /// Indicates if the column is a Foreign Key column.
        /// </summary>
        public bool IsForeignKey { get; set; }

        public bool IsNullable { get; set; }

        /// <summary>
        /// Related key table, if the column is a foreign key.
        /// </summary>
        public string ForeignKeyTable { get; set; }

        /// <summary>
        /// Related key column, if the column is a foreign key.
        /// </summary>
        public string ForeignKeyColumn { get; set; }

        /// <summary>
        /// Indicates if the column has a unique constraint. Either Primary Key or Unique Constraint applies.
        /// </summary>
        public bool IsUnique { get; set; }
    }
}
