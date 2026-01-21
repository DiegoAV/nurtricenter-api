using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using NurTriCentro.IntegrationTests.TestHost;


namespace NurTriCentro.IntegrationTests.TestHost
{
    public abstract class TestBase : IAsyncLifetime
    {

        protected string DbFilePath = default!;
        protected CustomWebApplicationFactory<Program> Factory = default!;
        protected HttpClient Client = default!;

        public virtual Task InitializeAsync()
        {
            // Creamos un archivo SQLite único por test (aislamiento total)
            var fileName = $"testdb_{Guid.NewGuid():N}.sqlite";
            DbFilePath = Path.Combine(Path.GetTempPath(), fileName);

            Factory = new CustomWebApplicationFactory<Program>(DbFilePath);
            Client = Factory.CreateDefaultClient();
            return Task.CompletedTask;
        }

        public virtual Task DisposeAsync()
        {
            try
            {
                if (File.Exists(DbFilePath))
                    File.Delete(DbFilePath);
            }
            catch { /* ignorar issues de IO */ }

            Factory?.Dispose();
            return Task.CompletedTask;
        }
    }
}