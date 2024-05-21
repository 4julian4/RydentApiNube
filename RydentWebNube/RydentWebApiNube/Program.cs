using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Hubs;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IClientesServicios, ClientesServicios>();
builder.Services.AddScoped<IHistorialesServicios, HistorialesServicios>();
builder.Services.AddScoped<ISedesConectadasServicios, SedesConectadasServicios>();
builder.Services.AddScoped<ISedesServicios, SedesServicios>();
builder.Services.AddScoped<IUsuariosServicios, UsuariosServicios>();
builder.Services.AddScoped<IHistorialDePagosServicios, HistorialDePagosServicios>();




//var strDbConn = builder.Configuration.GetConnectionString("ConexionDb");


//builder.Services.AddDbContext<AppDbContext>(options =>
//                options.UseSqlServer(strDbConn, ef => ef.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)), ServiceLifetime.Scoped);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = null;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "RydentWebCORS",
                      builder =>
                      {
                          builder
                             .AllowAnyOrigin()
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .WithOrigins("http://localhost:4200", "https://localhost:4200")
                             .AllowCredentials(); // Add this line to allow credentials
                      });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("RydentWebCORS");

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<RydentHub>("/rydenthub");
});

app.UseAuthorization();

app.MapControllers();

app.Run();
