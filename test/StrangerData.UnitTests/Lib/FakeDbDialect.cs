using StrangerData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData.UnitTests.Lib
{
    public class FakeDbDialect : DbDialect
    {
        public FakeDbDialect(string connectionString)
           : base(connectionString)
        {
        }

        public override void DeleteAll(Stack<RecordIdentifier> recordIdentifiers)
        {
        }

        public override TableColumnInfo[] GetTableSchemaInfo(string tableName)
        {
            return null;
        }

        public override IDictionary<string, object> GetValuesFromDatabase(string tableName, string name, object identityValue)
        {
            return null;
        }

        public override IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values)
        {
            return null;
        }

        public override bool RecordExists(string tableName, string columnName, object value)
        {
            return false;
        }

        public override void Dispose()
        {
        }
    }
}
