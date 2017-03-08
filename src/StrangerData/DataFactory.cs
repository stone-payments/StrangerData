using StrangerData.Generator;
using StrangerData.Utils;
using System;
using System.Collections.Generic;

namespace StrangerData
{
    public class DataFactory<TDialect>
        where TDialect : DbDialect
    {
        private readonly IDbDialect _databaseDialect;
        private Stack<Action> _tearDownStack;

        public DataFactory(string nameOrConnectionString)
        {
            _tearDownStack = new Stack<Action>();

            string connectionString = ConfigurationProvider.GetConnectionString(nameOrConnectionString) ?? nameOrConnectionString;

            _databaseDialect = DbDialectResolver.Resolve<TDialect>(connectionString);
        }

        public DataFactory(IDbDialect databaseDialect)
        {
            _tearDownStack = new Stack<Action>();

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
            TableGenerator tableGenerator = new TableGenerator(_databaseDialect, tableName);

            var generatedValues = tableGenerator.GenerateValues();

            _tearDownStack.Push(() => tableGenerator.TearDown());

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
            TableGenerator tableGenerator = new TableGenerator(_databaseDialect, tableName);

            var generatedValues = tableGenerator.GenerateValues(customDefinitions);

            _tearDownStack.Push(() => tableGenerator.TearDown());

            return generatedValues;
        }

        public void TearDown()
        {
            // Run each teardown action
            foreach (Action tearDownAction in _tearDownStack)
                tearDownAction();
        }
    }
}
