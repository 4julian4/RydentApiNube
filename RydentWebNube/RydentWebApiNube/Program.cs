using RydentWebApiNube.LogicaDeNegocio.Hubs;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddScoped<IClientesServicios, ClientesServicios>();
builder.Services.AddScoped<IHistorialesServicios, HistorialesServicios>();
builder.Services.AddScoped<ISedesConectadasServicios, SedesConectadasServicios>();
builder.Services.AddScoped<ISedesServicios, SedesServicios>();
builder.Services.AddScoped<IUsuariosServicios, UsuariosServicios>();
builder.Services.AddScoped<IHistorialDePagosServicios, HistorialDePagosServicios>();

// Cargar configuración de variables de entorno

//var strDbConn = builder.Configuration.GetConnectionString("ConexionDb");


//builder.Services.AddDbContext<AppDbContext>(options =>
//                options.UseSqlServer(strDbConn, ef => ef.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)), ServiceLifetime.Scoped);
// Configurar servicios y demás

// Add services to the container.

builder.Services.AddControllers();
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
                      policyBuilder =>
                      {
                          policyBuilder
                             .WithOrigins("http://localhost:4200", "https://localhost:4200", "https://rydentclient.azurewebsites.net")
                             .AllowAnyMethod()
                             .AllowAnyHeader()
                             .AllowCredentials();
                      });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseCors("RydentWebCORS"); // Posiciona esto antes de UseEndpoints

app.UseAuthorization();

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<RydentHub>("/rydenthub");
    endpoints.MapControllers();
});

app.Run();
