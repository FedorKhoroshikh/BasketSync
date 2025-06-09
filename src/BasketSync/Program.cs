using Application;
using Infrastructure;
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

var app = builder.Build();

// ---------- Middleware ----------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => 
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "BasketSync v1"));
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();
