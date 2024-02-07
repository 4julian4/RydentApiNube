﻿

using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.DbContexts
{
    public class AppDbContext: DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfigurationRoot configuration = new ConfigurationBuilder()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile("appsettings.json")
                   .Build();
                optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            }
            
        }
        public DbSet<Clientes> TClientes { get; set; }
        public DbSet<HistorialDePagos> THistorialDePagos { get; set; }
        public DbSet<Historiales> THistoriales { get; set; }
        public DbSet<Sedes> TSedes { get; set; }
        public DbSet<SedesConectadas> TSedesConectadas { get; set; }
        public DbSet<Usuarios> TUsuarios { get; set; }
    }
}
