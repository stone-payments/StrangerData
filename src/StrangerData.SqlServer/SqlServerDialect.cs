#if NET8_0_OR_GREATER
using Microsoft.Data.SqlClient;
#else
using System.Data.SqlClient;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace StrangerData.SqlServer
{
    public class SqlServerDialect : DbDialect
    {
        /// <summary>
        /// SQL Server allows some character columns (VARCHAR and NVARCHAR) to have unlimited size (MAX).
        /// This default value will be used as length for randomly generated strings to insert on these columns.
        /// </summary>
        private const int CharacterColumnsDefaultLength = 256;
        
        private readonly SqlConnection _sqlConnection;

        public SqlServerDialect(string connectionString)
            : base(connectionString)
        {
            _sqlConnection = new SqlConnection(connectionString);

            if (_sqlConnection.State != System.Data.ConnectionState.Open)
            {
                _sqlConnection.Open();
            }
        }

        public override TableColumnInfo[] GetTableSchemaInfo(string tableName)
        { //MaxLength is cut to half when the column is a N type. Those types use double space. 
            string sql = @"
            SELECT
	            OUTCOLUMNS.NAME                                         Name, 
	            TYPES.NAME                                              ColumnType, 
	            FLOOR(OUTCOLUMNS.[MAX_LENGTH] * 
	            CASE WHEN  TYPES.NAME IN ('nvarchar','nchar','ntext') 
		            then .5	
		            else 1 end) [MaxLength],
	            OUTCOLUMNS.[PRECISION]                                  [Precision],
	            OUTCOLUMNS.SCALE                                        Scale,
	            OUTCOLUMNS.IS_NULLABLE                                  IsNullable,
	            OUTCOLUMNS.IS_IDENTITY                                  IsIdentity,
                IIF(REFERENCED_COLUMN_NAME IS NULL, 0, 1)               IsForeignKey,
	            CONCAT(REFERENCED_SCHEMA, '.', REFERENCED_TABLE_NAME)   ForeignKeyTable,
	            REFERENCED_COLUMN_NAME                                  ForeignKeyColumn,
	            IS_UNIQUE                                               IsUnique
            FROM SYS.COLUMNS OUTCOLUMNS
            INNER JOIN SYS.TYPES ON OUTCOLUMNS.user_type_id = TYPES.user_type_id
            outer apply(

		            SELECT DISTINCT tables.schemaName FK_SCHEMA,
						            tables.name FK_TABLE_NAME,
						            fkc.name FK_COLUMN_NAME,
						            rpk.schemaname REFERENCED_SCHEMA,
						            rpk.table_name REFERENCED_TABLE_NAME,
						            rpk.name REFERENCED_COLUMN_NAME
		            FROM sys.foreign_keys 
			            CROSS apply
				            (SELECT INCOLUMNS.name, referenced_column_id, referenced_object_id
				               FROM sys.foreign_key_columns
				               INNER JOIN sys.columns INCOLUMNS ON INCOLUMNS.column_id = foreign_key_columns.parent_column_id AND INCOLUMNS.[object_id] = foreign_key_columns.parent_object_id and OUTCOLUMNS.column_id = INCOLUMNS.column_id
				               WHERE foreign_key_columns.constraint_object_id = foreign_keys.[object_id]) fkc 
			            CROSS apply
				            (SELECT schema_name(tables.schema_id) schemaname, object_name(tables.object_id) TABLE_NAME, columns.name
				               FROM sys.tables
				               INNER JOIN sys.columns ON tables.object_id = columns.object_id
				               AND columns.column_id = referenced_column_id
				               WHERE tables.object_id = fkc.referenced_object_id) rpk 
			            CROSS apply
				            (SELECT schema_name(tables.schema_id) schemaname, name
				               FROM sys.tables
				               WHERE tables.object_id = foreign_keys.parent_object_id) tables

		            WHERE foreign_keys.parent_object_id = OUTCOLUMNS.object_id
            ) outerapply
			OUTER APPLY(
				SELECT CAST(
					CASE WHEN EXISTS (
						SELECT * from sys.key_constraints C
						WHERE PARENT_OBJECT_ID in (OBJECT_ID(@tableName))
						AND unique_index_id = OUTCOLUMNS.column_id) THEN 1
					ELSE 0
					END
				AS BIT) AS IS_UNIQUE
			) PK
            WHERE OBJECT_ID in (OBJECT_ID(@tableName))
            AND OUTCOLUMNS.IS_COMPUTED = 0
            ORDER BY OUTCOLUMNS.column_id
";
            var informationSchemaCmd = new SqlCommand(sql);
            informationSchemaCmd.Parameters.Add(new SqlParameter
            {
                ParameterName = "@tableName",
                Value = tableName
            });

            informationSchemaCmd.Connection = _sqlConnection;

            var dr = informationSchemaCmd.ExecuteReader();

            var tableColumns = new List<TableColumnInfo>();
            if (dr.HasRows)
            {
                while (dr.Read())
                {
                    var tableColumnInfo = new TableColumnInfo();

                    tableColumnInfo.Name = (string)dr["Name"];
                    tableColumnInfo.Precision = Convert.ToInt32(dr["Precision"]);
                    tableColumnInfo.Scale = Convert.ToInt32(dr["Scale"]);
                    tableColumnInfo.MaxLength = Convert.ToInt32(dr["MaxLength"]);
                    tableColumnInfo.IsIdentity = Convert.ToBoolean(dr["IsIdentity"]);
                    tableColumnInfo.IsNullable = Convert.ToBoolean(dr["IsNullable"]);
                    tableColumnInfo.IsForeignKey = Convert.ToBoolean(dr["IsForeignKey"]);
                    tableColumnInfo.ForeignKeyTable = (string)dr["ForeignKeyTable"];
                    tableColumnInfo.ForeignKeyColumn = dr["ForeignKeyColumn"] is DBNull ? null : (string)dr["ForeignKeyColumn"];
                    tableColumnInfo.IsUnique = Convert.ToBoolean(dr["IsUnique"]);
                    switch (((string)dr["ColumnType"]).ToUpper())
                    {
                        case "REAL":
                        case "SMALLMONEY":
                        case "MONEY":
                        case "NUMERIC":
                        case "DECIMAL":
                            tableColumnInfo.ColumnType = ColumnType.Decimal;
                            break;
                        case "CHAR":
                        case "NCHAR":
                        case "TEXT":
                        case "NTEXT":
                            tableColumnInfo.ColumnType = ColumnType.String;
                            break;
                        case "NVARCHAR":
                        case "VARCHAR":
                            tableColumnInfo.ColumnType = ColumnType.String;
                            if (tableColumnInfo.MaxLength == -1) // SQL Server uses -1 as length for unlimited columns (NVARCHAR(MAX) and VARCHAR(MAX))
                                tableColumnInfo.MaxLength = SqlServerDialect.CharacterColumnsDefaultLength;
                            break;
                        case "ROWVERSION":
                        case "FILESTREAM":
                        case "VARBINARY":
                        case "IMAGE":
                            tableColumnInfo.ColumnType = ColumnType.Byte;
                            break;
                        case "FLOAT":
                            tableColumnInfo.ColumnType = ColumnType.Double;
                            break;
                        case "BINARY":
                            tableColumnInfo.ColumnType = ColumnType.Binary;
                            break;
                        case "BIT":
                            tableColumnInfo.ColumnType = ColumnType.Boolean;
                            break;
                        case "TINYINT":
                        case "INT":
                        case "SMALLINT":
                            tableColumnInfo.ColumnType = ColumnType.Int;
                            break;
                        case "BIGINT":
                            tableColumnInfo.ColumnType = ColumnType.Long;
                            break;
                        case "UNIQUEIDENTIFIER":
                            tableColumnInfo.ColumnType = ColumnType.Guid;
                            break; ;
                        case "DATE":
                            tableColumnInfo.ColumnType = ColumnType.Date;
                            break;
                        case "TIME":
                        case "SMALLDATETIME":
                        case "DATETIMEOFFSET":
                        case "DATETIME2":
                        case "DATETIME":
                            tableColumnInfo.ColumnType = ColumnType.Datetime;
                            break;
                        default:
                            tableColumnInfo.ColumnType = ColumnType.Unsuported;
                            break;
                    }

                    tableColumns.Add(tableColumnInfo);
                }
                dr.Dispose();

                return tableColumns.ToArray();
            }
            throw new Exception("Table not found!");
        }

        public override IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values)
        {
            bool hasIdentity = tableSchemaInfo.Any(t => t.IsIdentity);

            var sanitizedValues = SanitizeDictionaryKeys(values);
            StringBuilder insertStatementBuilder = new StringBuilder()
                                                    .AppendFormat("INSERT INTO {0}", SanitizeTableName(tableName))
                                                    .AppendFormat("({0})", string.Join(",", sanitizedValues.Keys.Select(key => $"[{key}]" )))
                                                    .AppendFormat(" VALUES ({0});", string.Join(",", sanitizedValues.Keys.Select(c => "@" + c)));
            // .AppendFormat(" SELECT SCOPE_IDENTITY();")
            //  .ToString();

            if (hasIdentity)
            {
                insertStatementBuilder.Append(" SELECT SCOPE_IDENTITY();");
            }

            SqlCommand insertCmd = new SqlCommand(insertStatementBuilder.ToString(), _sqlConnection);
            insertCmd.CommandType = System.Data.CommandType.Text;

            foreach (string column in sanitizedValues.Keys)
            {
                insertCmd.Parameters.Add(new SqlParameter
                {
                    Direction = System.Data.ParameterDirection.Input,
                    ParameterName = "@" + column,
                    Value = sanitizedValues[column] ?? DBNull.Value
                });
            }

            if (hasIdentity)
            {
                string identityColumn = tableSchemaInfo.First(t => t.IsIdentity).Name;

                sanitizedValues[identityColumn] = insertCmd.ExecuteScalar();
            }
            else
            {
                insertCmd.ExecuteNonQuery();
            }

            return sanitizedValues;
        }

        public override bool RecordExists(string tableName, string columnName, object value)
        {
            string existRecordSql = string.Format("SELECT 1 FROM {0} WHERE {1} = @Value", SanitizeTableName(tableName), columnName);
            SqlCommand existRecordCmd = new SqlCommand(existRecordSql, _sqlConnection);

            existRecordCmd.Parameters.Add(new SqlParameter { ParameterName = "@Value", Value = value });

            return Convert.ToBoolean(existRecordCmd.ExecuteScalar());
        }

        public override IDictionary<string, object> GetValuesFromDatabase(string tableName, string columnName, object value)
        {
            string getRecordSql = string.Format("SELECT DbGener123.* FROM {0} AS DbGener123 WHERE {1} = @Value", tableName, columnName);

            SqlCommand getRecordCmd = new SqlCommand(getRecordSql, _sqlConnection);

            getRecordCmd.Parameters.Add(new SqlParameter { ParameterName = "@Value", Value = value });

            SqlDataReader getRecordCmdResult = getRecordCmd.ExecuteReader();

            if (getRecordCmdResult.HasRows)
            {
                getRecordCmdResult.Read();

                IDictionary<string, object> valuesFromDatabase = new Dictionary<string, object>();

                for (var i = 0; i < getRecordCmdResult.FieldCount; i++)
                {
                    valuesFromDatabase[getRecordCmdResult.GetName(i)] = getRecordCmdResult[i];
                }

                if (!getRecordCmdResult.IsClosed) getRecordCmdResult.Dispose();

                return valuesFromDatabase;
            }

            throw new InvalidOperationException("Record not found.");
        }

        public override void DeleteAll(Stack<RecordIdentifier> recordIdentifiers)
        {
            if (recordIdentifiers.Any())
            {
                using (var transaction = _sqlConnection.BeginTransaction())
                {
                    while (recordIdentifiers.Count > 0)
                    {
                        RecordIdentifier recordIdentifier = recordIdentifiers.Pop();

                        string deleteStmt = string.Format("DELETE FROM {0} WHERE {1} = @Id", SanitizeTableName(recordIdentifier.TableName), recordIdentifier.ColumnName);

                        SqlCommand deleteCommand = new SqlCommand(deleteStmt, _sqlConnection, transaction);
                        deleteCommand.Parameters.Add(new SqlParameter { ParameterName = "@Id", Value = recordIdentifier.IdentifierValue });

                        deleteCommand.ExecuteNonQuery();
                    }

                    transaction.Commit();
                }
            }
        }

        private string SanitizeTableName(string tableName)
        {
            if (tableName.Contains('.'))
                return string.Format("[{0}].[{1}]", tableName.Split('.')[0], tableName.Split('.')[1]);
            else
                return string.Format("[{0}]", tableName);
        }
        
        private static IDictionary<string, object> SanitizeDictionaryKeys(IDictionary<string, object> valuesDict)
        {
            var sanitizedDict = new Dictionary<string, object>();
            var acceptedCharactersRegex = new Regex(@"[^a-zA-Z0-9_\-]");
            foreach (var kvPair in valuesDict)
            {
                var sanitizedKey = acceptedCharactersRegex.Replace(kvPair.Key, "");
                sanitizedDict[sanitizedKey] = kvPair.Value;
            }

            return sanitizedDict;
        }

        public override void Dispose()
        {
            _sqlConnection.Dispose();
        }
    }
}
