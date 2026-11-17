using System.Data.Common;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TodoApi.Data;

namespace TodoApi.Tests;

public class InternalErrorTests : IClassFixture<InternalErrorTests.FailingDbFactory>
{
    private const string Secret = "SECRETO-INTERNO-12345";
    private readonly FailingDbFactory _factory;

    public InternalErrorTests(FailingDbFactory factory) => _factory = factory;

    /// <summary>Simula un fallo real de base de datos en las consultas de los endpoints (no en las migraciones).</summary>
    public class FailingDbFactory : TodoApiFactory
    {
        public ThrowingInterceptor Interceptor { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
                services.AddDbContext<AppDbContext>(o => o.AddInterceptors(Interceptor)));
        }
    }

    public class ThrowingInterceptor : DbCommandInterceptor
    {
        public volatile bool Enabled;

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
            => Enabled ? throw new InvalidOperationException(Secret) : result;

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
            => Enabled ? throw new InvalidOperationException(Secret) : new(result);
    }

    [Fact]
    public async Task Un_error_interno_devuelve_500_ProblemDetails_sin_filtrar_detalles()
    {
        var client = _factory.CreateClient();   // fuerza el arranque (migraciones) con el interceptor desactivado
        _factory.Interceptor.Enabled = true;
        try
        {
            var response = await client.GetAsync("/api/tasks/1");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            using var json = JsonDocument.Parse(body);
            Assert.Equal(500, json.RootElement.GetProperty("status").GetInt32());
            Assert.DoesNotContain(Secret, body);
            Assert.DoesNotContain("InvalidOperationException", body);
            Assert.DoesNotContain(" at TodoApi", body);   // sin stack trace
        }
        finally
        {
            _factory.Interceptor.Enabled = false;
        }
    }
}
