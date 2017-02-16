using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StrangerData
{
    internal static class DbDialectResolver
    {
        public static IDbDialect Resolve<TDialect>(string connectionString)
            where TDialect : class, IDbDialect
        {
            return (IDbDialect)Activator.CreateInstance(typeof(TDialect), connectionString);
        }
    }
}
