using Microsoft.EntityFrameworkCore;
using SukruUyarBackend.Data;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// 🔑 appsettings.json içindeki DefaultConnection'ı okuyup SQLite'a bağlıyoruz
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors();

// 🚀 SWAGGER AYARI: if şartını kaldırdık, böylece Render'da da aslanlar gibi açılacak!
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SukruUyar API v1");
    c.RoutePrefix = string.Empty; // Sonuna /swagger yazmana gerek kalmadan direkt ana linkte açılacak!
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();