using System.Text;
using System.Text.Json;
using Application;
using Application.Exceptions;
using Infrastructure;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- DI ----------
builder.Services
    .AddInfrastructure(builder.Configuration)   // Infrastructure (DB + repositories + UoW)
    .AddApplication()            // Application (MediatR + AutoMapper)
    .AddControllers();           // MVC

// ---------- JWT Authentication ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSection["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();

// Swagger / Minimal API explorer
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo { Title = "BasketSync API", Version = "v1" });
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

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

// ---------- Exception configuring ----------
app.UseExceptionHandler(a => a.Run(async context =>
{
    var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;

    context.Response.ContentType = "application/json";

    if (ex is KeyNotFoundException)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        return;
    }

    if (ex is ConflictException)
    {
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        return;
    }

    if (ex is UnauthorizedAccessException)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        return;
    }

    // fallback
    Console.Error.WriteLine($"[500] Unhandled exception: {ex}");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex?.Message ?? "Внутренняя ошибка сервера" }));
}));

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

// ---------- Frontend ----------
app.UseDefaultFiles();
app.UseStaticFiles();

// Serve uploaded card images from /uploads
var uploadsPath = Path.Combine(app.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads"
});

app.MapControllers();

// ---------- Seed default category ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (!await db.Categories.AnyAsync(c => c.Name == "Без категории"))
    {
        db.Categories.Add(new Domain.Entities.Category("Без категории"));
        await db.SaveChangesAsync();
    }
}

await app.RunAsync();
