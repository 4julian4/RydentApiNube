using RydentWebApiNube.LogicaDeNegocio.Hubs;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using Microsoft.AspNetCore.ResponseCompression;

var builder = WebApplication.CreateBuilder(args);

// Cargar configuración de variables de entorno
builder.Configuration.AddEnvironmentVariables();

// Registrar servicios
builder.Services.AddScoped<IClientesServicios, ClientesServicios>();
builder.Services.AddScoped<IHistorialesServicios, HistorialesServicios>();
builder.Services.AddScoped<ISedesConectadasServicios, SedesConectadasServicios>();
builder.Services.AddScoped<ISedesServicios, SedesServicios>();
builder.Services.AddScoped<IUsuariosServicios, UsuariosServicios>();
builder.Services.AddScoped<IHistorialDePagosServicios, HistorialDePagosServicios>();

// Agregar controladores y documentación Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuración de SignalR
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors = true;
    o.MaximumReceiveMessageSize = null;
});

// Configuración de CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: "RydentWebCORS", policyBuilder =>
    {
        policyBuilder     
            .WithOrigins(
						 "http://rydentweb-001-site3.jtempurl.com", "https://rydentweb-001-site3.jtempurl.com",
						 "http://rydentweb-001-site2.jtempurl.com", "https://rydentweb-001-site2.jtempurl.com",
						 "http://rydentweb-001-site1.jtempurl.com", "https://rydentweb-001-site1.jtempurl.com",
                         "http://localhost:4200", "https://localhost:4200", "https://rydentclient.azurewebsites.net"
                        ) // Especificar dominios permitidos
            .AllowAnyMethod()   // Permitir cualquier método (GET, POST, etc.)
            .AllowAnyHeader()   // Permitir cualquier encabezado
            .AllowCredentials(); // Permitir credenciales (cookies, cabeceras de autenticación, etc.)
    });
});

// Habilitar compresión de respuestas
builder.Services.AddResponseCompression(options =>
{
    options.Providers.Add<GzipCompressionProvider>(); // Agregar proveedor de compresión Gzip
    options.EnableForHttps = true; // Habilitar compresión solo para HTTPS
});

var app = builder.Build();

// Configuración de entorno de desarrollo (Swagger)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configuración de la aplicación
app.UseHttpsRedirection();   // Redirigir todas las solicitudes HTTP a HTTPS
app.UseStaticFiles();        // Servir archivos estáticos (como imágenes, CSS, JS)
app.UseRouting();             // Habilitar el enrutamiento



// Habilitar CORS antes de la configuración de routing
app.UseCors("RydentWebCORS");  // Aplicar la política CORS definida



app.UseAuthorization();       // Habilitar autorización

// Middleware para manejar excepciones globalmente
app.UseExceptionHandler("/error");

// Configuración de las rutas de los endpoints
app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<RydentHub>("/rydenthub");  // Rutas para SignalR (comunicación en tiempo real)
    endpoints.MapControllers();                  // Rutas para los controladores de la API
});

app.Run();
