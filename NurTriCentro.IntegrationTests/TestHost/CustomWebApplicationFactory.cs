
using Infraestructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net.Http;


namespace NurTriCentro.IntegrationTests.TestHost
{

    // Nota: 'Program' debe ser la clase de entrada de tu API (top-level program).
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {

        private readonly string _dbPath;
        private SqliteConnection? _keepAliveConnection;

        public CustomWebApplicationFactory(string dbPath) => _dbPath = dbPath;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Test");

            // 1) Inyectamos una ConnectionString "Default" apuntando al archivo SQLite del test
            builder.ConfigureAppConfiguration((ctx, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = $"Data Source={_dbPath};"
                });
            });

            // 2) Después de que la app registre sus servicios, nosotros sobreescribimos el DbContext
            builder.ConfigureTestServices(services =>
            {
                // Si alguna vez cambias a :memory:, dejamos el patrón para mantener viva la conexión
                if (_dbPath == ":memory:")
                {
                    _keepAliveConnection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
                    _keepAliveConnection.Open();
                }

                // --- Limpieza robusta de registros previos del contexto ---
                services.RemoveAll<DbContextOptions<AppDbContext>>();
                services.RemoveAll<AppDbContext>();
                // Si usaran Factory también la removemos
                var factoryDescriptor = services.FirstOrDefault(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition() == typeof(IDbContextFactory<>));
                if (factoryDescriptor is not null) services.Remove(factoryDescriptor);

                // --- Re-registro del contexto con SQLite (archivo por test) ---
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlite($"Data Source={_dbPath};"));

                // --- Migraciones (crear esquema antes de correr los tests) ---
                using var sp = services.BuildServiceProvider();
                using var scope = sp.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // db.Database.EnsureDeleted(); // solo si reusaras el mismo archivo entre tests
                // db.Database.Migrate();
                db.Database.EnsureCreated();
            });
        }

        public HttpClient CreateDefaultClient() =>
            CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            _keepAliveConnection?.Dispose();
        }

    }

}
