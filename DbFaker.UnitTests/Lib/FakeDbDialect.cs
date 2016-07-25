using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbFaker.UnitTests.Lib
{
    public class FakeDbDialect : IDatabaseDialect
    {
        public void DeleteAll(Stack<RecordIdentifier> recordIdentifiers)
        {
        }

        public void Dispose()
        {
        }

        public TableColumnInfo[] GetTableSchemaInfo(string tableName)
        {
            return null;
        }

        public IDictionary<string, object> GetValuesFromDatabase(string tableName, string name, object identityValue)
        {
            return null;
        }

        public IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values)
        {
            return null;
        }

        public bool RecordExists(string tableName, string columnName, object value)
        {
            return false;
        }
    }
}
