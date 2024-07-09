using Microsoft.Extensions.Configuration;

namespace Snowflake
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // Determine the base path
            var basePath = Directory.GetCurrentDirectory();

            // Build configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)  // Set base path to the current directory
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // Get SnowflakeIdConfig section
            var snowflakeConfig = configuration.GetSection("SnowflakeIdConfig");
            string? workerId = snowflakeConfig.GetSection("WorkerId").Value;
            string? datacenterId = snowflakeConfig.GetSection("DatacenterId").Value;

            // Initialize SnowflakeIdGenerator
            SnowflakeIdGenerator.Initialize(Convert.ToInt32(workerId), Convert.ToInt32(datacenterId));

            // Generate and print Snowflake IDs
            for (int i = 0; i < 10; i++)
            {
                long id = SnowflakeIdGenerator.NextId();
                Console.WriteLine(id);
            }
        }
    }
}
