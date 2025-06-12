using System.Text.Json;
using Application;
using Application.Exceptions;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- DI ----------
builder.Services
    .AddInfrastructure(builder.Configuration)   // Infrastructure (DB + repositories + UoW)
    .AddApplication()            // Application (MediatR + AutoMapper)  
    .AddControllers();           // MVC


// Swagger / Minimal API explorer
builder.Services.AddSwaggerGen(o => 
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "BasketSync API", Version = "v1" }));

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BasketSync v1"));
}

// ---------- DB connection check ----------
app.MapGet("/db-check", async (AppDbContext db) => 
{
    try
    {
        await db.Database.CanConnectAsync();
        return "DB connection OK";
    }
    catch (Exception ex)
    {
        return $"DB connection FAILED: {ex.Message}";
    }
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.UseRouting();
app.UseCors("AllowAll");

// ---------- Frontend ----------
app.UseDefaultFiles();
app.UseStaticFiles();

// ---------- Exception configuring ----------
app.UseExceptionHandler(a => a.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    
    context.Response.ContentType = "application/json";

    if (ex is ConflictException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        return;
    }
    
    // fallback
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = "Внутренняя ошибка сервера" }));
}));

await app.RunAsync();
