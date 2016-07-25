using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DbFaker
{
    public interface IDatabaseDialect : IDisposable
    {
        TableColumnInfo[] GetTableSchemaInfo(string tableName);

        IDictionary<string, object> Insert(string tableName, IEnumerable<TableColumnInfo> tableSchemaInfo, IDictionary<string, object> values);

        bool RecordExists(string tableName, string columnName, object value);

        IDictionary<string, object> GetValuesFromDatabase(string tableName, string name, object identityValue);

        void DeleteAll(Stack<RecordIdentifier> recordIdentifiers);
    }
}
