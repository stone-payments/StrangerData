using StrangerData;
using StrangerData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData.Generator
{
    internal class TableGenerator
    {
        private readonly IDbDialect _dbDialect;
        private readonly string _tableName;
        private readonly IEnumerable<TableColumnInfo> _tableColumnInfoList;
        private readonly Stack<RecordIdentifier> _generatedRecords;
        private readonly int _depth;

        private TableGenerator(IDbDialect dbDialect, string tableName, Stack<RecordIdentifier> generatedRecords, int depth)
        {
            _dbDialect = dbDialect;
            _tableName = tableName;
            _generatedRecords = generatedRecords;
            _depth = depth;

            // Try get the column info for this table in memory cache, else, get from dialect and store on cache
            _tableColumnInfoList = MemoryCache.TryGetFromCache<IEnumerable<TableColumnInfo>>(tableName, () =>
            {
                return _dbDialect.GetTableSchemaInfo(tableName);
            });
        }

        public TableGenerator(IDbDialect dbDialect, string tableName)
            : this(dbDialect, tableName, new Stack<RecordIdentifier>(), 0)
        {
        }

        public IDictionary<string, object> GenerateValues(Action<FactoryDefinition> customDefinitions)
        {
            return null;
        }

        public IDictionary<string, object> GenerateValues()
        {
            IDictionary<string, object> generatedValues = new Dictionary<string, object>();

            if (!HasIdentityColumn())
            {
                /*
                 *   var firstColumn = tableSchemaInfo.First();

                object identityValue = GenerateValueForColumn(firstColumn);

                generatedValues[firstColumn.Name] = identityValue;

                if (_databaseDialect.RecordExists(tableName, firstColumn.Name, identityValue))
                {
                    fromDatabase = true;
                    return _databaseDialect.GetValuesFromDatabase(tableName, firstColumn.Name, identityValue);
                }
                 */
            }

            // For each column
            foreach (TableColumnInfo column in _tableColumnInfoList)
            {
                // If the column type is a supported column type, and it is not Nullable
                if (column.ColumnType != ColumnType.Unsuported && !column.IsNullable)
                {
                    if (column.IsForeignKey)
                    {
                        if (IsSelfReferencedForeignKey(column.ForeignKeyTable))
                        {
                            if (_depth == 0)
                            {
                                // creates a new table generator to generate data for this foreign key
                                TableGenerator foreignKeyTableGenerator = new TableGenerator(_dbDialect,
                                                                                             column.ForeignKeyTable,
                                                                                             _generatedRecords,
                                                                                             _depth + 1);

                                IDictionary<string, object> foreignKeyGeneratedData = foreignKeyTableGenerator.GenerateValues();

                                generatedValues[column.ForeignKeyTable] = foreignKeyGeneratedData[column.ForeignKeyTable];

                                // TODO: Inner from database?
                            }
                        } // if (IsSelfReferencedForeignKey(column.ForeignKeyTable))
                        else
                        {
                            // Try to get an existing generated value for this foreign key column
                            RecordIdentifier existingRecord = GetGeneratedRecord(column.ForeignKeyTable, column.ForeignKeyColumn);

                            // record exists?
                            if (existingRecord != null)
                            {
                                // Use the existing value
                                generatedValues[column.Name] = existingRecord.IdentifierValue;
                            }
                            else
                            {
                                if (column.IsNullable == false)
                                {
                                    // creates a new table generator to generate data for this foreign key
                                    TableGenerator foreignKeyTableGenerator = new TableGenerator(_dbDialect, column.ForeignKeyTable, _generatedRecords, _depth + 1);
                                    generatedValues[column.Name] = foreignKeyTableGenerator.GenerateValues()[column.ForeignKeyColumn];
                                }
                            }
                        }
                    } // if (column.IsForeignKey)
                    else
                    {
                        // Is not a foreign key
                        if (!column.IsIdentity)
                        {
                            // Nor a identity column
                            generatedValues[column.Name] = RandomValues.ForColumn(column);
                        }
                    }
                }
            }

            // Insert values into database
            return InsertInDatabase(generatedValues);
        }

        public Stack<RecordIdentifier> GetGeneratedRecords()
        {
            return _generatedRecords;
        }

        internal void TearDown()
        {
            _dbDialect.DeleteAll(_generatedRecords);
        }

        private IDictionary<string, object> InsertInDatabase(IDictionary<string, object> values)
        {
            IDictionary<string, object> insertResult = _dbDialect.Insert(_tableName, _tableColumnInfoList, values);

            // Adds the generated record identifier to list
            TableColumnInfo identityColumn = _tableColumnInfoList.FirstOrDefault(t => t.IsIdentity) ?? _tableColumnInfoList.First();
            _generatedRecords.Push(new RecordIdentifier
            {
                TableName = _tableName,
                ColumnName = identityColumn.Name,
                IdentifierValue = insertResult[identityColumn.Name]
            });

            return insertResult;
        }

        private RecordIdentifier GetGeneratedRecord(string table, string column)
        {
            return _generatedRecords.FirstOrDefault(t => t.TableName == table && t.ColumnName == column);
        }

        /// <summary>
        /// Returns true if the foreign key table name is the same table.
        /// </summary>
        /// <param name="foreignKeyTableName">Name of the foreign key table.</param>
        private bool IsSelfReferencedForeignKey(string foreignKeyTableName)
        {
            return foreignKeyTableName.Equals(_tableName, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns if this table has a identity column
        /// </summary>
        private bool HasIdentityColumn()
        {
            return _tableColumnInfoList.Any(t => t.IsIdentity);
        }
    }
}
