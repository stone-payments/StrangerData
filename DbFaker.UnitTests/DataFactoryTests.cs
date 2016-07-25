using Moq;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using DbFaker;
using DbFaker.UnitTests.Lib;

namespace DbFaker.UnitTests
{
    public class DataFactoryTests
    {
        private readonly Mock<IDatabaseDialect> _databaseDialect;
        private readonly DataFactory<FakeDbDialect> _dataFactory;

        public DataFactoryTests()
        {
            _databaseDialect = new Mock<IDatabaseDialect>();

            _dataFactory = new DataFactory<FakeDbDialect>("fakeConnectionString");
        }

        [Fact]
        public void CreateOne_TableWithoutForeignKey_CreateRecord()
        {
            string tableName = "dbo.MyTable";

            var tableSchemaInfo = new[]
                            {
                                new TableColumnInfo { ColumnType = ColumnType.String, Name = "MyColumn1", MaxLength = 20 },
                                new TableColumnInfo { ColumnType = ColumnType.Int, Name = "MyColumn2", Scale = 5 },
                            };

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(tableName))))
                            .Returns(tableSchemaInfo)
                            .Verifiable();

            _databaseDialect.Setup(t => t.RecordExists(It.Is<string>(table => table.Equals(tableName)),
                                                       It.IsAny<string>(), It.IsAny<object>()))
                            .Verifiable();

            _databaseDialect.Setup(t => t.Insert(It.IsAny<string>(),
                                                 It.IsAny<TableColumnInfo[]>(),
                                                 It.IsAny<IDictionary<string, object>>()
                                   )).Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                    {
                                        table.Should().Be(tableName);

                                        schema.ElementAt(0).Name.Should().Be("MyColumn1");
                                        schema.ElementAt(1).Name.Should().Be("MyColumn2");

                                        values["MyColumn1"].Should().BeOfType<string>();
                                        values["MyColumn2"].Should().BeOfType<int>();
                                    });

            _databaseDialect.VerifyAll();
        }

        [Fact]
        public void CreateOne_TableWithForeignKey_CreateRecordForTableAndReferencedTable()
        {
            string tableName = "dbo.MyTable";
            string referencedTableName = "dbo.MyForeignTable";

            var tableSchemaInfo = new[]
                            {
                                new TableColumnInfo { ColumnType = ColumnType.String, Name = "MyColumn1", MaxLength = 20 },
                                new TableColumnInfo {
                                    ColumnType = ColumnType.Int,
                                    Name = "MyForeignTableId", IsForeignKey = true,
                                    ForeignKeyTable = referencedTableName,
                                    ForeignKeyColumn = "MyForeignId"
                                }
                            };

            var foreignTableSchemaInfo = new[]
            {
                                new TableColumnInfo {
                                    ColumnType = ColumnType.Int,
                                    Name = "MyForeignId",
                                    Scale = 10
                                }
            };

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(referencedTableName))))
                            .Returns(foreignTableSchemaInfo)
                            .Verifiable();

            _databaseDialect.Setup(t => t.GetTableSchemaInfo(It.Is<string>(table => table.Equals(tableName))))
                            .Returns(tableSchemaInfo)
                            .Verifiable();

            _databaseDialect.Setup(t => t.RecordExists(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>()))
                            .Returns(false);

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(tableName)),
                                                 It.IsAny<TableColumnInfo[]>(),
                                                 It.IsAny<IDictionary<string, object>>()
                                   ))
                                   .Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                   {
                                       table.Should().Be(tableName);
                                   });

            _databaseDialect.Setup(t => t.Insert(It.Is<string>(table => table.Equals(referencedTableName)),
                                     It.IsAny<TableColumnInfo[]>(),
                                     It.IsAny<IDictionary<string, object>>()
                                   ))
                                   .Callback<string, IEnumerable<TableColumnInfo>, IDictionary<string, object>>((table, schema, values) =>
                                   {
                                       table.Should().Be(referencedTableName);
                                   });

            var result = _dataFactory.CreateOne(tableName);
        }
    }
}
