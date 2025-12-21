using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NutriCentro.ContractTests.Provider
{

    public sealed class ApiFixture : IDisposable
    {
        public Uri BaseUri { get; } = new Uri("http://127.0.0.1:7160");
        private readonly Process _proc;

        public ApiFixture()
        {
            // Ajusta rutas si tu solución está en otro nivel
            var psi = new ProcessStartInfo("dotnet",
                $"run --project ../../API/API.csproj --urls={BaseUri}")
            {
                WorkingDirectory = System.IO.Path.GetFullPath("../../"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.Environment["ASPNETCORE_ENVIRONMENT"] = "Testing";

            _proc = Process.Start(psi)!;

            // Espera a que la API responda
            using var http = new HttpClient();
            var deadline = DateTime.UtcNow.AddSeconds(30);
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    var resp = http.GetAsync(new Uri(BaseUri, "/swagger")).Result;
                    if (resp.IsSuccessStatusCode) break;
                }
                catch { /* aún levantando */ }
                Thread.Sleep(500);
            }
        }

        public void Dispose()
        {
            try
            {
                if (!_proc.HasExited)
                {
                    _proc.Kill(entireProcessTree: true);
                    _proc.WaitForExit();
                }
            }
            finally
            {
                _proc.Dispose();
            }
        }
    }

}
