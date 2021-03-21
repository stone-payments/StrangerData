using StrangerData.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StrangerData.UnitTests")]
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
            _tableColumnInfoList = MemoryCache.TryGetFromCache<IEnumerable<TableColumnInfo>>(_dbDialect.ConnectionString, tableName, () => this._dbDialect.GetTableSchemaInfo(tableName));
        }

        public TableGenerator(IDbDialect dbDialect, string tableName)
            : this(dbDialect, tableName, new Stack<RecordIdentifier>(), 0)
        {
        }

        public IDictionary<string, object> GenerateValues(Action<FactoryDefinition> customDefinitions)
        {
            IDictionary<string, object> generatedValuesDict = new Dictionary<string, object>();

            FactoryDefinition definitions = new FactoryDefinition(generatedValuesDict);

            // Apply custom definition
            customDefinitions(definitions);

            // Generate the values
            GenerateValuesForColumns(generatedValuesDict);

            // Insert values into database
            return InsertInDatabase(generatedValuesDict);
        }

        public IDictionary<string, object> GenerateValues()
        {
            IDictionary<string, object> generatedValues = new Dictionary<string, object>();

            if (!HasIdentityColumn())
            {
                var firstNonFKColumn = _tableColumnInfoList.First(c => c.IsForeignKey == false);

                object identityValue = RandomValues.ForColumn(firstNonFKColumn);

                generatedValues[firstNonFKColumn.Name] = identityValue;

                if (_dbDialect.RecordExists(_tableName, firstNonFKColumn.Name, identityValue))
                {
                    return _dbDialect.GetValuesFromDatabase(_tableName, firstNonFKColumn.Name, identityValue);
                }
            }

            // Generate the values
            GenerateValuesForColumns(generatedValues);            

            // Insert values into database
            return InsertInDatabase(generatedValues);
        }

        private void GenerateValuesForColumns(IDictionary<string, object> generatedValuesDict)
        {
            // For each column
            foreach (TableColumnInfo column in _tableColumnInfoList)
            {
                // Skip the column if it was already been generated.
                if (generatedValuesDict.ContainsKey(column.Name))
                {
                    continue;
                }

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

                                generatedValuesDict[column.ForeignKeyTable] = foreignKeyGeneratedData[column.ForeignKeyTable];

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
                                generatedValuesDict[column.Name] = existingRecord.IdentifierValue;
                            }
                            else
                            {
                                if (column.IsNullable == false)
                                {
                                    // creates a new table generator to generate data for this foreign key
                                    TableGenerator foreignKeyTableGenerator = new TableGenerator(_dbDialect, column.ForeignKeyTable, _generatedRecords, _depth + 1);
                                    generatedValuesDict[column.Name] = foreignKeyTableGenerator.GenerateValues()[column.ForeignKeyColumn];
                                }
                            }
                        }
                    } // if (column.IsForeignKey)
                    else if (column.IsIdentity == false)
                    {
                        object random = RandomValues.ForColumn(column);

                        if (column.IsUnique)
                        {
                            // Keep generating random value while they are not unique
                            while (_dbDialect.RecordExists(_tableName, column.Name, random))
                            {
                                random = RandomValues.ForColumn(column);
                            }
                        }

                        generatedValuesDict[column.Name] = random;
                    }
                }
            }
        }

        public Stack<RecordIdentifier> GetGeneratedRecords()
        {
            return _generatedRecords;
        }

        /// <summary>
        /// Deletes all generated records.
        /// </summary>
        internal void TearDown()
        {
            _dbDialect.DeleteAll(_generatedRecords);
        }

        /// <summary>
        /// Inserts the generated value into database.
        /// </summary>
        /// <param name="values">Generated values dictionary.</param>
        /// <returns>The dictionary itself.</returns>
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

        /// <summary>
        /// Gets a generated record by their table name and column name.
        /// </summary>
        /// <param name="table">Table name.</param>
        /// <param name="column">Column name.</param>
        /// <returns>The record identifier associated with the record found.</returns>
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
            return foreignKeyTableName.Equals(_tableName, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns if this table has a identity column.
        /// </summary>
        private bool HasIdentityColumn()
        {
            return _tableColumnInfoList.Any(t => t.IsIdentity);
        }
    }
}
