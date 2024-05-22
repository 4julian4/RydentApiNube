

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
                IConfigurationRoot configuration = new ConfigurationBuilder().AddEnvironmentVariables()
                   .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                   .AddJsonFile("appsettings.json")
                   .Build();
                string connectionString = configuration["CONEXIONDB"];
                optionsBuilder.UseSqlServer(configuration["CONEXIONDB"]);
            }
            
        }
        public DbSet<Clientes> TClientes { get; set; }
        public DbSet<HistorialDePagos> THistorialDePagos { get; set; }
        public DbSet<Historiales> THistoriales { get; set; }
        public DbSet<Sedes> TSedes { get; set; }
        public DbSet<SedesConectadas> TSedesConectadas { get; set; }
        public DbSet<Usuarios> TUsuarios { get; set; }
        //public DbSet<Citas> TCitas { get; set; }
        //public DbSet<DetalleCitas> TDetalleCitas { get; set; }
    }
}
