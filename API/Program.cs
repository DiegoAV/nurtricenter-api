using Aplication.UseCases.CrearContrato;
using Aplication.Validators;
using Domain.Interfaces;
using FluentValidation;
using Infraestructure.Persistence;
using Infraestructure.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IContratoRepository, ContratoRepository>();
builder.Services.AddScoped<IServicioRepository, ServicioRepository>();
builder.Services.AddScoped<ICalendarioEntregaRepository, CalendarioEntregaRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<ActualizarHorarioEntregaValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearContratoValidator>();
builder.Services.AddValidatorsFromAssemblyContaining<CrearServicioValidator>();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

/// Agrega el registro del handler específico
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CrearContratoHandler).Assembly));

//builder.Services.AddMediatR(typeof(CrearContratoHandler).Assembly);
//builder.Services.AddValidatorsFromAssemblyContaining<CrearContratoValidator>();


builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Crea la base de datos si no existe
    DbInitializer.Seed(dbContext);      // Inserta los datos iniciales
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
