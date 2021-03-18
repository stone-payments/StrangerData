using System;

namespace StrangerData.Utils
{
    internal static class RandomValues
    {
        public static object ForColumn(TableColumnInfo columnInfo)
        {
            try
            {
                return GenerateForColumn(columnInfo);
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not generate value for column Name=[{columnInfo.Name}] Type=[{columnInfo.ColumnType}]", ex);
            }
        }

        private static object GenerateForColumn(TableColumnInfo columnInfo)
        {
            switch (columnInfo.ColumnType)
            {
                case ColumnType.String:
                    // generates a random string
                    return Any.String(columnInfo.MaxLength);
                case ColumnType.Int:
                    // generates a random integer
                    long maxValue = (int)Math.Pow(10, columnInfo.Precision - 1);
                    if (maxValue > int.MaxValue)
                    {
                        return Any.Long(1, maxValue);
                    }
                    return Any.Int(1, (int)maxValue);
                case ColumnType.Decimal:
                    // generates a random decimal
                    return Any.Double(columnInfo.Precision, columnInfo.Scale);
                case ColumnType.Double:
                    // generates a random double
                    return Any.Double(columnInfo.Precision, columnInfo.Scale);
                case ColumnType.Long:
                    // generates a random long
                    return Any.Long(1, (long)Math.Pow(10, columnInfo.Precision - 1));
                case ColumnType.Boolean:
                    // generates a random boolean
                    return Any.Boolean();
                case ColumnType.Guid:
                    // generates a random guid
                    return Guid.NewGuid();
                case ColumnType.Date:
                    // generates a random date
                    return Any.DateTime().Date;
                case ColumnType.Datetime:
                    // generates a random DateTime
                    return Any.DateTime();
                default:
                    return null;
            }
        }
    }
}
