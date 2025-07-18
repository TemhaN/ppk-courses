//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;
//using Microsoft.Extensions.Configuration;

//namespace web.Data
//{
//   public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
//   {
//       public AppDbContext CreateDbContext(string[] args)
//       {
//           IConfigurationRoot configuration = new ConfigurationBuilder()
//               .SetBasePath(Directory.GetCurrentDirectory())
//               .AddJsonFile("appsettings.json")
//               .Build();

//           var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
//           var connectionString = configuration.GetConnectionString("DefaultConnection");
//           optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
//           {
//               sqlOptions.EnableRetryOnFailure(
//                   maxRetryCount: 15,
//                   maxRetryDelay: TimeSpan.FromSeconds(30),
//                   errorNumbersToAdd: null);
//           });

//           return new AppDbContext(optionsBuilder.Options);
//       }
//   }
//}

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace web.Data
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            optionsBuilder.UseSqlServer(connectionString, sqlServerOptions =>
            {
                sqlServerOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            });

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}