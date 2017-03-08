#if NET452
using System.Configuration;
#else
using Microsoft.Extensions.Configuration;
using System.IO;
#endif

namespace StrangerData.Utils
{
    public class ConfigurationProvider
    {
#if NET452
        public static string GetConnectionString(string name)
        {
            return ConfigurationManager.ConnectionStrings[name]?.ConnectionString;
        }
#else
        private static IConfigurationRoot _configuration;

        private static IConfigurationRoot LoadConfiguration()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            return builder.Build();
        }

        public static string GetConnectionString(string name)
        {
            if (_configuration == null)
            {
                _configuration = LoadConfiguration();
            }

            return _configuration.GetConnectionString(name);
        }
#endif
    }
}
