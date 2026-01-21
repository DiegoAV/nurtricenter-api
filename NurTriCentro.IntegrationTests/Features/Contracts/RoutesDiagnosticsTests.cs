using FluentAssertions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using NurTriCentro.IntegrationTests.TestHost;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace NurTriCentro.IntegrationTests.Features.Contracts
{

    public class RoutesDiagnosticsTests : TestBase
    {

        private readonly ITestOutputHelper _output;

        public RoutesDiagnosticsTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact(DisplayName = "Diagnóstico: listar rutas disponibles")]
        public void List_All_Registered_Endpoints()
        {
            using var scope = Factory.Services.CreateScope();
            var dataSource = scope.ServiceProvider.GetRequiredService<EndpointDataSource>();

            var endpoints = dataSource.Endpoints
                .OfType<RouteEndpoint>()
                .Select(e => e.RoutePattern.RawText)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            foreach (var endpoint in endpoints)
            {
                _output.WriteLine($"[ENDPOINT] {endpoint}");
            }

            endpoints.Should().NotBeEmpty();
        }

    }
}
