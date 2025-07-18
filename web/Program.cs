using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using web.Data;
using Dapper;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);

ExcelPackage.License.SetNonCommercialPersonal(Environment.GetEnvironmentVariable("EPPLUS_LICENSE") ?? "non-commercial-placeholder");

builder.Services.AddRazorPages();
builder.Services.AddDbContext<AppDbContext>(options =>
   options.UseSqlServer(
       builder.Configuration.GetConnectionString("DefaultConnection"),
       sqlOptions =>
       {
           sqlOptions.EnableRetryOnFailure(
               maxRetryCount: 15,
               maxRetryDelay: TimeSpan.FromSeconds(30),
               errorNumbersToAdd: null);
       }));

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
   options.IdleTimeout = TimeSpan.FromMinutes(30);
   options.Cookie.HttpOnly = true;
   options.Cookie.IsEssential = true;
});

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
   app.UseExceptionHandler("/Error");
   app.UseHsts();
}

// app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.Use(async (context, next) =>
{
   context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
   await next();
});

// builder.Services.AddSession(options =>
// {
//     options.IdleTimeout = TimeSpan.FromMinutes(30);
//     options.Cookie.HttpOnly = true;
//     options.Cookie.IsEssential = true;
// });
app.UseSession();

app.UseAuthorization();
app.UseAntiforgery();
app.MapRazorPages();

try
{
    using var scope = app.Services.CreateScope();
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    Console.WriteLine($"Connection string: {connectionString}");

    using var connection = new SqlConnection(connectionString.Replace("Database=sabzor04_db", "Database=master"));
    await connection.OpenAsync();
    await connection.ExecuteAsync("IF DB_ID('sabzor04_db') IS NULL CREATE DATABASE sabzor04_db");
    await connection.CloseAsync();

    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Console.WriteLine("Checking migrations...");
    if (dbContext.Database.GetPendingMigrations().Any())
    {
        Console.WriteLine("Applying migrations...");
        for (int i = 1; i <= 5; i++)
        {
            try
            {
                await dbContext.Database.MigrateAsync();
                Console.WriteLine("Migrated successfully.");
                break;
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 2714)
            {
                Console.WriteLine($"Table already exists, skipping migration: {ex.Message}");
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {i}/5 failed: {ex.Message}");
                if (i == 5) throw;
                await Task.Delay(5000);
            }
        }
    }
    else
    {
        Console.WriteLine("No pending migrations.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Critical error during DB setup: {ex.Message}");
    throw;
}


app.Run();



// using Microsoft.EntityFrameworkCore;
// using OfficeOpenXml;
// using web.Data;

// var builder = WebApplication.CreateBuilder(args);

// ExcelPackage.License.SetNonCommercialPersonal(Environment.GetEnvironmentVariable("EPPLUS_LICENSE") ?? "non-commercial-placeholder");

// builder.Services.AddRazorPages();
// builder.Services.AddDbContext<AppDbContext>(options =>
//     options.UseSqlServer(
//         builder.Configuration.GetConnectionString("DefaultConnection"),
//         sqlServerOptions =>
//         {
//             sqlServerOptions.EnableRetryOnFailure(
//                 maxRetryCount: 5,
//                 maxRetryDelay: TimeSpan.FromSeconds(30),
//                 errorNumbersToAdd: null);
//         }));

// builder.Services.AddDistributedMemoryCache();
// builder.Services.AddSession(options =>
// {
//     options.IdleTimeout = TimeSpan.FromMinutes(30);
//     options.Cookie.HttpOnly = true;
//     options.Cookie.IsEssential = true;
// });

// builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");

// var app = builder.Build();

// if (!app.Environment.IsDevelopment())
// {
//     app.UseExceptionHandler("/Error");
//     app.UseHsts();
// }

// try
// {
//     using (var scope = app.Services.CreateScope())
//     {
//         var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//         dbContext.Database.Migrate();
//         Console.WriteLine("Database migrated and ready!");
//     }
// }
// catch (Exception ex)
// {
//     Console.WriteLine($"Migration failed: {ex.Message}");
//     throw;
// }

// app.UseHttpsRedirection();
// app.UseStaticFiles();
// app.UseRouting();
// //app.Use(async (context, next) =>
// //{
// //    context.Response.Headers.Add("Content-Type", "text/html; charset=utf-8");
// //    await next();
// //});
// app.UseSession();
// app.UseAuthorization();
// app.UseAntiforgery();
// app.MapRazorPages();

// app.Run();