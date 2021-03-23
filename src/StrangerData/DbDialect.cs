using System;
using System.Collections.Generic;

namespace StrangerData
{
    public abstract class DbDialect : IDbDialect
    {
        public string ConnectionString { get; }

        protected DbDialect(string connectionString)
        {
            this.ConnectionString = connectionString;
        }

        public virtual void DeleteAll(Stack<RecordIdentifier> recordIdentifiers)
        {
            throw new NotImplementedException();
        }

        public virtual TableColumnInfo[] GetTableSchemaInfo(string tableName)
        {
            throw new NotImplementedException();
        }

        public virtual IDictionary<string, object> GetValuesFromDatabase(string tableName, string name, object identityValue)
        {
            throw new NotImplementedException();
        }

        public virtual IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values)
        {
            throw new NotImplementedException();
        }

        public virtual bool RecordExists(string tableName, string columnName, object value)
        {
            throw new NotImplementedException();
        }

        public virtual void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
