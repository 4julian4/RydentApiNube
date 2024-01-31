using Microsoft.AspNetCore.SignalR;
using RydentWebApiNube.LogicaDeNegocio.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

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
app.UseRouting();

app.UseCors("RydentWebCORS");

app.UseEndpoints(endpoints =>
{
    endpoints.MapHub<RydentHub>("/rydenthub");
});

app.UseAuthorization();

app.MapControllers();

app.Run();
