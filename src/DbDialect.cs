using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbFaker
{
    public abstract class DbDialect : IDbDialect
    {
        public DbDialect(string connectionString)
        {
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
