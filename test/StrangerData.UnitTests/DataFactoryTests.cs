using FluentAssertions;
using Moq;
using StrangerData.UnitTests.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace StrangerData.UnitTests
{
    public class DataFactoryTests
    {
        private readonly Mock<IDbDialect> _databaseDialect;
        private readonly DataFactory<FakeDbDialect> _dataFactory;

        public DataFactoryTests()
        {
            _databaseDialect = new Mock<IDbDialect>();

            _dataFactory = new DataFactory<FakeDbDialect>(_databaseDialect.Object);
        }

        [Fact]
        public void CreateOne_TableWithoutForeignKey_CreateRecord()
        {
            string testTable = $"dbo.MyTable{DateTime.Now.Ticks}";

            var tableSchemaInfo = new[]
                            {
                                new TableColumnInfo { ColumnType = ColumnType.String, Name = "MyColumn1", MaxLength = 20 },
                                new TableColumnInfo { ColumnType = ColumnType.Int, Name = "MyColumn2", Precision = 5 },
                            };

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(testTable))))
                            .Returns(tableSchemaInfo)
                            .Verifiable();

            _databaseDialect.Setup(t => t.RecordExists(It.Is<string>(table => table.Equals(testTable)),
                                                       It.IsAny<string>(), It.IsAny<object>()))
                            .Verifiable();

            IDictionary<string, object> insertData = new Dictionary<string, object>();
            insertData.Add("MyColumn1", "ABCDEF");
            insertData.Add("MyColumn2", 123);

            _databaseDialect.Setup(t => t.Insert(It.IsAny<string>(),
                                                 It.IsAny<TableColumnInfo[]>(),
                                                 It.IsAny<IDictionary<string, object>>()
                                   )).Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                    {
                                        table.Should().Be(testTable);

                                        schema.ElementAt(0).Name.Should().Be("MyColumn1");
                                        schema.ElementAt(1).Name.Should().Be("MyColumn2");

                                        values["MyColumn1"].Should().BeOfType<string>();
                                        values["MyColumn2"].Should().BeOfType<int>();
                                    })
                                    .Returns(insertData);

            _dataFactory.CreateOne(testTable);

            _databaseDialect.VerifyAll();
        }

        [Fact]
        public void CreateOne_TableWithForeignKey_CreateRecordForTableAndReferencedTable()
        {
            string tableName = $"dbo.MyTable{DateTime.Now.Ticks}";
            string referencedTableName = $"dbo.MyForeignTable{DateTime.Now.Ticks}";

            MockGetTableSchemaInfoWithForeignKey(tableName, referencedTableName);

            var foreignTableSchemaInfo = new[]
            {
                                new TableColumnInfo {
                                    ColumnType = ColumnType.Int,
                                    Name = "MyForeignId",
                                    Precision = 10
                                }
            };

            var foreignTableData = new Dictionary<string, object>();
            foreignTableData.Add("MyForeignId", 123);

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(referencedTableName))))
                            .Returns(foreignTableSchemaInfo)
                            .Verifiable();

            _databaseDialect.Setup(t => t.RecordExists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                            .Returns(false);

            var primaryTableData = new Dictionary<string, object>();
            primaryTableData.Add("MyColumn1", "TEST");
            primaryTableData.Add("MyForeignTableId", 123);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(tableName)),
                                                 It.IsAny<TableColumnInfo[]>(),
                                                 It.IsAny<IDictionary<string, object>>()
                                   ))
                                   .Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                   {
                                       table.Should().Be(tableName);
                                   })
                                   .Returns(primaryTableData);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(referencedTableName)),
                                     It.IsAny<TableColumnInfo[]>(),
                                     It.IsAny<IDictionary<string, object>>()
                                   ))
                                   .Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                   {
                                       table.Should().Be(referencedTableName);
                                   })
                                   .Returns(foreignTableData);

            var result = _dataFactory.CreateOne(tableName);
        }

        [Fact]
        public void TearDown_TableWithForeignKeys_GenerateRecordsToDelete()
        {
            string tableName = $"dbo.MyTable{DateTime.Now.Ticks}";
            string referencedTableName = $"dbo.MyForeignTable{DateTime.Now.Ticks}";

            MockGetTableSchemaInfoWithForeignKey(tableName, referencedTableName);

            var foreignTableSchemaInfo = new[]
            {
                                new TableColumnInfo {
                                    ColumnType = ColumnType.Int,
                                    Name = "MyForeignId",
                                    Precision = 10
                                }
            };

            var primaryTableData = new Dictionary<string, object>();
            primaryTableData.Add("MyColumn1", "TEST");
            primaryTableData.Add("MyForeignTableId", 123);

            var foreignTableData = new Dictionary<string, object>();
            foreignTableData.Add("MyForeignId", 123);

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(referencedTableName))))
                            .Returns(foreignTableSchemaInfo);

            _databaseDialect.Setup(t => t.RecordExists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                            .Returns(false);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(tableName)),
                                                 It.IsAny<TableColumnInfo[]>(),
                                                 It.IsAny<IDictionary<string, object>>()
                                   ))
                            .Returns(primaryTableData);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(referencedTableName)),
                                        It.IsAny<TableColumnInfo[]>(),
                                        It.IsAny<IDictionary<string, object>>()
                                   ))
                             .Returns(foreignTableData);

            _databaseDialect.Setup(t => t.DeleteAll(It.IsAny<Stack<RecordIdentifier>>()))
                            .Callback<Stack<RecordIdentifier>>((recordIdentifiers) =>
                            {
                                var first = recordIdentifiers.ElementAt(1);

                                first.TableName.Should().Be(referencedTableName);
                                first.ColumnName.Should().Be("MyForeignId");
                                first.IdentifierValue.Should().Be(123);

                                var next = recordIdentifiers.ElementAt(0);

                                next.TableName.Should().Be(tableName);
                                next.ColumnName.Should().Be("MyColumn1");
                            });

            var result = _dataFactory.CreateOne(tableName);

            _dataFactory.TearDown();
        }

        [Fact]
        public void CreateOne_WithCustomDefinitionsForForeignColumn_NotGenerateRandomValue()
        {
            string tableName = $"dbo.MyTable{DateTime.Now.Ticks}";
            string referencedTableName = $"dbo.MyForeignTable{DateTime.Now.Ticks}";

            MockGetTableSchemaInfoWithForeignKey(tableName, referencedTableName);

            // Setup insertion of generated table data
            var primaryTableData = new Dictionary<string, object>();
            primaryTableData.Add("MyColumn1", "TEST");
            primaryTableData.Add("MyForeignTableId", 123);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(tableName)),
                                     It.IsAny<TableColumnInfo[]>(),
                                     It.IsAny<IDictionary<string, object>>()
                       )).Returns(primaryTableData);

            // Setup insert with referenced table name, it shouldn't be called
            _databaseDialect.Setup(t => t.Insert(It.Is<string>(c => c == referencedTableName),
                                                  It.IsAny<IEnumerable<TableColumnInfo>>(),
                                                  It.IsAny<IDictionary<string, object>>()))
                            .Throws(new Exception("Shouldn't be called"));

            _dataFactory.CreateOne(tableName, o =>
            {
                o.WithValue("MyForeignTableId", 2);
            });
        }

        [Fact]
        public void CreateOne_TableWithUniqueKey_ChecksIfValueExists()
        {
            string tableName = $"dbo.MyTable{DateTime.Now.Ticks}";
            string columnName = "MyUniqueColumn";

            TableColumnInfo[] tableSchema = MockGetTableSchemaInfoWithUniqueKey(tableName, columnName);

            Dictionary<string, object> insertedValues = new Dictionary<string, object>();
            insertedValues["MyColumn1"] = "testString";

            _databaseDialect.Setup(t => t.RecordExists(tableName, columnName, It.IsAny<object>()))
                .Callback((string t, string column, object val) => { insertedValues[column] = val; })
                .Returns(false)
                .Verifiable();

            _databaseDialect.Setup(t => t.Insert(tableName, tableSchema, insertedValues))
                .Returns(insertedValues)
                .Verifiable();

            _dataFactory.CreateOne(tableName, o =>
            {
                o.WithValue("MyColumn1", "testString");
            });

            _databaseDialect.VerifyAll();
        }

        [Fact]
        public void CreateOne_TableWithLongTypeColumn_CreateRecord()
        {
            string testTable = $"dbo.MyTable{DateTime.Now.Ticks}";
            string columnName = "LongTypeColumn";

            TableColumnInfo[] tableSchemaInfo = new[] { new TableColumnInfo { ColumnType = ColumnType.Long, Name = columnName, Precision = 19 } };

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(testTable))
                            .Returns(tableSchemaInfo)
                            .Verifiable();

            IDictionary<string, object> insertData = new Dictionary<string, object>() { { columnName, long.MaxValue } };

            _databaseDialect.Setup(t => t.Insert(testTable, tableSchemaInfo, It.IsAny<IDictionary<string, object>>()))
                .Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                {
                    values[columnName].Should().BeOfType<long>();
                })
                .Returns(insertData);

            _dataFactory.CreateOne(testTable);

            _databaseDialect.VerifyAll();
        }

        private void MockGetTableSchemaInfoWithForeignKey(string tableName, string referencedTableName)
        {
            TableColumnInfo[] tableSchemaInfo = new[]
            {
                new TableColumnInfo { ColumnType = ColumnType.String, Name = "MyColumn1", MaxLength = 20 },
                new TableColumnInfo {
                    ColumnType = ColumnType.Int,
                    Name = "MyForeignTableId", IsForeignKey = true,
                    ForeignKeyTable = referencedTableName,
                    ForeignKeyColumn = "MyForeignId",
                    Precision = 10
                }
            };


            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(tableName))))
                            .Returns(tableSchemaInfo)
                            .Verifiable();
        }

        private TableColumnInfo[] MockGetTableSchemaInfoWithUniqueKey(string tableName, string uniqueColumnName)
        {
            TableColumnInfo[] tableSchemaInfo = new[]
            {
                new TableColumnInfo { ColumnType = ColumnType.String, Name = "MyColumn1", MaxLength = 20 },
                new TableColumnInfo {
                    ColumnType = ColumnType.Int,
                    Name = uniqueColumnName,
                    IsForeignKey = false,
                    Precision = 10,
                    IsUnique = true,
                }
            };

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(tableName))
                            .Returns(tableSchemaInfo)
                            .Verifiable();

            return tableSchemaInfo;
        }
    }
}
