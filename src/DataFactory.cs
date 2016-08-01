using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

namespace StrangerData
{
    public class DataFactory<TDialect>
        where TDialect : DbDialect
    {
        private readonly IDbDialect _databaseDialect;
        private readonly IDictionary<string, IEnumerable<TableColumnInfo>> _tableInfoCache;
        private readonly Stack<RecordIdentifier> _generatedRecordIdentifiers;

        public DataFactory(string nameOrConnectionString)
        {
            _tableInfoCache = new Dictionary<string, IEnumerable<TableColumnInfo>>();
            _generatedRecordIdentifiers = new Stack<RecordIdentifier>();

            string connectionString = (ConfigurationManager.ConnectionStrings[nameOrConnectionString] != null) ?
                                            ConfigurationManager.ConnectionStrings[nameOrConnectionString].ConnectionString :
                                            nameOrConnectionString;

            _databaseDialect = (IDbDialect)Activator.CreateInstance(typeof(TDialect), connectionString);
        }

        public DataFactory(IDbDialect databaseDialect)
        {
            _tableInfoCache = new Dictionary<string, IEnumerable<TableColumnInfo>>();
            _generatedRecordIdentifiers = new Stack<RecordIdentifier>();

            _databaseDialect = databaseDialect;
        }

        /// <summary>
        /// Creates one record in the table.
        /// </summary>
        /// <example>
        /// var dataFactory = new DataFactory("myConfiguredConnection");
        /// 
        /// var generatedData = dataFactory.CreateOne("dbo.MyTable");
        /// 
        /// // Print the id
        /// Console.WriteLine(generatedData["Id"]);
        /// </example>
        /// <param name="tableName">Table's name to create a record.</param>
        /// <returns>A dictionary including all generated values for this record, including their Id.</returns>
        public IDictionary<string, object> CreateOne(string tableName)
        {
            IEnumerable<TableColumnInfo> tableSchemaInfo = GetTableInfo(tableName);

            bool fromDatabase = false;

            IDictionary<string, object> generatedValues = GenerateRandomValues(tableName, tableSchemaInfo, out fromDatabase);

            if (!fromDatabase)
            {
                return InsertValuesInDatabase(tableName, tableSchemaInfo, generatedValues);
            }

            return generatedValues;
        }

        /// <summary>
        /// Creates one record in the table, and applies the customDefinitions action to the record
        /// to specify values explicity.
        /// </summary>
        /// <example>
        /// var dataFactory = new DataFactory("myConfiguredConnection");
        /// 
        /// var generatedData = dataFactory.CreateOne("dbo.MyTable", t => {
        ///     t.WithValue("MyColumn", "ABCDE");
        /// });
        /// 
        /// // Print the id
        /// Console.WriteLine(generatedData["Id"]);
        /// </example>
        /// <param name="tableName">Table's name to create a record.</param>
        /// <returns>A dictionary including all generated values for this record, including their Id.</returns>
        public IDictionary<string, object> CreateOne(string tableName, Action<FactoryDefinition> customDefinitions)
        {
            IEnumerable<TableColumnInfo> tableSchemaInfo = GetTableInfo(tableName);

            bool fromDatabase = false;

            IDictionary<string, object> generatedValues = GenerateRandomValues(tableName, tableSchemaInfo, out fromDatabase);

            customDefinitions(new FactoryDefinition(generatedValues));

            if (!fromDatabase)
                return InsertValuesInDatabase(tableName, tableSchemaInfo, generatedValues);

            return generatedValues;
        }

        private IDictionary<string, object> GenerateRandomValues(string tableName,
                                                                 IEnumerable<TableColumnInfo> tableSchemaInfo,
                                                                 out bool fromDatabase,
                                                                 int depth = 0)
        {
            IDictionary<string, object> generatedValues = new Dictionary<string, object>();

            // If the tables has no identity column
            if (!tableSchemaInfo.Any(t => t.IsIdentity))
            {
                var firstColumn = tableSchemaInfo.First();

                object identityValue = GenerateValueForColumn(firstColumn);

                generatedValues[firstColumn.Name] = identityValue;

                if (_databaseDialect.RecordExists(tableName, firstColumn.Name, identityValue))
                {
                    fromDatabase = true;
                    return _databaseDialect.GetValuesFromDatabase(tableName, firstColumn.Name, identityValue);
                }
            }

            // For each column of the table
            foreach (TableColumnInfo column in tableSchemaInfo)
            {
                // If the column type is a supported column type, and it is not Nullable
                if (column.ColumnType != ColumnType.Unsuported && column.IsNullable == false)
                {
                    // If is a foreign key column
                    if (column.IsForeignKey)
                    {
                        // If is a self-referenced foreignkey
                        if (column.ForeignKeyTable.Equals(tableName, StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (depth == 0)
                            {
                                bool innerFromDatabase = false;
                                var innerGeneratedValues = GenerateRandomValues(column.ForeignKeyTable, tableSchemaInfo, out innerFromDatabase, depth: 1);

                                if (!innerFromDatabase)
                                    generatedValues[column.Name] = InsertValuesInDatabase(column.ForeignKeyTable, tableSchemaInfo, innerGeneratedValues)[column.ForeignKeyColumn];
                                else
                                    generatedValues[column.Name] = innerGeneratedValues[column.ForeignKeyColumn];
                            }
                        }
                        else
                        {
                            var existingGeneratedRelatedRecord = _generatedRecordIdentifiers.FirstOrDefault(t => t.TableName == column.ForeignKeyTable && t.ColumnName == column.ForeignKeyColumn);
                            if (existingGeneratedRelatedRecord != null)
                            {
                                generatedValues[column.Name] = existingGeneratedRelatedRecord.IdentifierValue;
                            }
                            else
                            {
                                generatedValues[column.Name] = CreateOne(column.ForeignKeyTable)[column.ForeignKeyColumn];
                            }
                        }
                    }
                    else
                    {
                        // If not is a identity column
                        if (!column.IsIdentity)
                        {
                            generatedValues[column.Name] = GenerateValueForColumn(column);
                        }
                    }
                }
            }

            fromDatabase = false;

            return generatedValues;
        }

        public void TearDown()
        {
            _databaseDialect.DeleteAll(_generatedRecordIdentifiers);
        }

        private object GenerateValueForColumn(TableColumnInfo column)
        {
            switch (column.ColumnType)
            {
                case ColumnType.String:
                    return Any.String(column.MaxLength);
                case ColumnType.Int:
                    long maxValue = 10 ^ column.Precision - 2;
                    if (maxValue > int.MaxValue)
                    {
                        return Any.Long(1, column.Precision - 2);
                    }
                    return Any.Int(1, 10 ^ column.Precision - 2);
                case ColumnType.Decimal:
                    return Any.Decimal();
                case ColumnType.Double:
                    return Any.Double();
                case ColumnType.Long:
                    return Any.Long(1, 10 ^ column.Precision - 2);
                case ColumnType.Boolean:
                    return Any.Boolean();
                case ColumnType.Guid:
                    return Guid.NewGuid();
                case ColumnType.Date:
                    return Any.DateTime().Date;
                case ColumnType.Datetime:
                    return Any.DateTime();
                default:
                    return null;
            }
        }

        private IEnumerable<TableColumnInfo> GetTableInfo(string tableName)
        {
            if (!_tableInfoCache.ContainsKey(tableName))
            {
                _tableInfoCache[tableName] = _databaseDialect.GetTableSchemaInfo(tableName);
            }

            return _tableInfoCache[tableName];
        }

        private IDictionary<string, object> InsertValuesInDatabase(string tableName,
                                                                   IEnumerable<TableColumnInfo> tableSchemaInfo,
                                                                   IDictionary<string, object> values)
        {
            IDictionary<string, object> insertResult = _databaseDialect.Insert(tableName, tableSchemaInfo, values);

            // Adds the generated record identifier to list
            TableColumnInfo identityColumn = tableSchemaInfo.FirstOrDefault(t => t.IsIdentity) ?? tableSchemaInfo.First();
            _generatedRecordIdentifiers.Push(new RecordIdentifier
            {
                TableName = tableName,
                ColumnName = identityColumn.Name,
                IdentifierValue = insertResult[identityColumn.Name]
            });

            return insertResult;
        }
    }
}
