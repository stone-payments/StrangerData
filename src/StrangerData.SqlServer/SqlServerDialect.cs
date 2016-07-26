using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData.SqlServer
{
    public class SqlServerDialect : DbDialect
    {
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
        {
            string sql = @"SELECT
	OUTCOLUMNS.NAME                                         Name, 
	TYPES.NAME                                              ColumnType, 
	OUTCOLUMNS.[MAX_LENGTH]                                 [MaxLength],
	OUTCOLUMNS.[PRECISION]                                  [Precision],
	OUTCOLUMNS.SCALE                                        Scale,
	OUTCOLUMNS.IS_NULLABLE                                  IsNullable,
	OUTCOLUMNS.IS_IDENTITY                                  IsIdentity,
    IIF(REFERENCED_COLUMN_NAME IS NULL, 0, 1)               IsForeignKey,
	CONCAT(REFERENCED_SCHEMA, '.', REFERENCED_TABLE_NAME)   ForeignKeyTable,
	REFERENCED_COLUMN_NAME                                  ForeignKeyColumn
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
WHERE OBJECT_ID in (OBJECT_ID(@tableName))
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

                    switch (((string)dr["ColumnType"]).ToUpper())
                    {
                        case "CHAR":
                        case "VARCHAR":
                            tableColumnInfo.ColumnType = ColumnType.String;
                            break;
                        case "INT":
                        case "SMALLINT":
                            tableColumnInfo.ColumnType = ColumnType.Int;
                            break;
                        case "BIGINT":
                            tableColumnInfo.ColumnType = ColumnType.Long;
                            break;
                        case "UNIQUEIDENTIFIER":
                            tableColumnInfo.ColumnType = ColumnType.Guid;
                            break;
                        case "DECIMAL":
                            tableColumnInfo.ColumnType = ColumnType.Decimal;
                            break;
                        case "DATE":
                            tableColumnInfo.ColumnType = ColumnType.Date;
                            break;
                        case "DATETIME":
                            tableColumnInfo.ColumnType = ColumnType.Datetime;
                            break;
                        default:
                            tableColumnInfo.ColumnType = ColumnType.Unsuported;
                            break;
                    }

                    tableColumns.Add(tableColumnInfo);
                }
                dr.Close();

                return tableColumns.ToArray();
            }
            throw new Exception("Table not found!");
        }

        public override IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values)
        {
            bool hasIdentity = tableSchemaInfo.Any(t => t.IsIdentity);

            StringBuilder insertStatementBuilder = new StringBuilder()
                                                    .AppendFormat("INSERT INTO {0}", SanitizeTableName(tableName))
                                                    .AppendFormat("({0})", string.Join(",", values.Keys))
                                                    .AppendFormat(" VALUES ({0});", string.Join(",", values.Keys.Select(c => "@" + c)));
            // .AppendFormat(" SELECT SCOPE_IDENTITY();")
            //  .ToString();

            if (hasIdentity)
            {
                insertStatementBuilder.Append(" SELECT SCOPE_IDENTITY();");
            }

            SqlCommand insertCmd = new SqlCommand(insertStatementBuilder.ToString(), _sqlConnection);
            insertCmd.CommandType = System.Data.CommandType.Text;

            foreach (string column in values.Keys)
            {
                insertCmd.Parameters.Add(new SqlParameter
                {
                    Direction = System.Data.ParameterDirection.Input,
                    ParameterName = "@" + column,
                    Value = values[column] ?? DBNull.Value
                });
            }

            if (hasIdentity)
            {
                string identityColumn = tableSchemaInfo.First(t => t.IsIdentity).Name;

                values[identityColumn] = insertCmd.ExecuteScalar();
            }
            else
            {
                insertCmd.ExecuteNonQuery();
            }

            return values;
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

                DataTable schemaTable = getRecordCmdResult.GetSchemaTable();

                for (var i = 0; i < getRecordCmdResult.FieldCount; i++)
                {
                    valuesFromDatabase[getRecordCmdResult.GetName(i)] = getRecordCmdResult[i];
                }

                if (!getRecordCmdResult.IsClosed) getRecordCmdResult.Close();

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

        public override void Dispose()
        {
            _sqlConnection.Dispose();
        }
    }
}
