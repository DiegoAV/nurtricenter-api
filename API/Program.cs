//using Aplication.UseCases.CrearContrato;
//using Aplication.Validators;
//using Domain.Interfaces;
//using FluentValidation;
//using Infraestructure.Persistence;
//using Infraestructure.Repositories;
//using MediatR;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.DependencyInjection;
//using System;
//using System.Reflection;

//var builder = WebApplication.CreateBuilder(args);


//// Add services to the container.

//builder.Services.AddControllers();
//// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();

//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddScoped<IContratoRepository, ContratoRepository>();
//builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
//builder.Services.AddScoped<ICalendarioEntregaRepository, CalendarioEntregaRepository>();
//builder.Services.AddValidatorsFromAssemblyContaining<ActualizarHorarioEntregaValidator>();
//builder.Services.AddValidatorsFromAssemblyContaining<CrearContratoValidator>();
//builder.Services.AddValidatorsFromAssemblyContaining<CrearServicioValidator>();

//builder.Services.AddMediatR(cfg =>
//    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

///// Agrega el registro del handler específico
//builder.Services.AddMediatR(cfg =>
//    cfg.RegisterServicesFromAssembly(typeof(CrearContratoHandler).Assembly));

////builder.Services.AddMediatR(typeof(CrearContratoHandler).Assembly);
////builder.Services.AddValidatorsFromAssemblyContaining<CrearContratoValidator>();


//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();


//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
//    app.UseSwagger();
//    app.UseSwaggerUI();
//}

//using (var scope = app.Services.CreateScope())
//{
//    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    dbContext.Database.EnsureCreated(); // Crea la base de datos si no existe
//    DbInitializer.Seed(dbContext);      // Inserta los datos iniciales
//}

//app.UseHttpsRedirection();

//app.UseAuthorization();

//app.MapControllers();

//app.Run();
//public partial class Program { } // Agrega esta línea para permitir pruebas de integración




using Aplication.UseCases.CrearContrato;
using Aplication.Validators;
using Domain.Interfaces;
using FluentValidation;
using Infraestructure.Persistence;
using Infraestructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// --- Servicios base ---
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- Detectar entorno de pruebas ---
var isTesting = builder.Environment.IsEnvironment("Testing");

// --- DbContext: InMemory para Testing, Sqlite para Dev/Prod ---
if (isTesting)
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseInMemoryDatabase("TestsDB"));
}
else
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// --- Repositorios ---
builder.Services.AddScoped<IContratoRepository, ContratoRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ICalendarioEntregaRepository, CalendarioEntregaRepository>();

// --- Validadores (FluentValidation) ---
builder.Services.AddValidatorsFromAssemblyContaining<ActualizarHorarioEntregaValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearContratoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearServicioValidator>();

// --- MediatR (una sola vez; registra handlers desde ensamblado de Application) ---
builder.Services.AddMediatR(cfg =>
{
    // Registra desde el ensamblado donde está el handler
    cfg.RegisterServicesFromAssembly(typeof(CrearContratoHandler).Assembly);
    // Si tienes más handlers en otros ensamblados, puedes sumar:
    // cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
});

var app = builder.Build();

// --- Swagger (habilitar también fuera de Development si quieres probar en Docker) ---
app.UseSwagger();
app.UseSwaggerUI();

// --- Inicialización DB (solo Dev/Prod) ---
if (!isTesting)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // Crea/migra y hace seeding solo en ambientes reales
    dbContext.Database.EnsureCreated(); // o dbContext.Database.Migrate();
    DbInitializer.Seed(dbContext);
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();

// Necesario para WebApplicationFactory<Program> en pruebas de integración
public partial class Program { }

