using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class HistorialDePagosServicios : IHistorialDePagosServicios
    {
        protected readonly AppDbContext _dbcontext;
        public HistorialDePagosServicios()
        {

        }


        public async Task<long> Agregar(HistorialDePagos historialdepagos)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.THistorialDePagos.Add(historialdepagos);
                await _dbcontext.SaveChangesAsync();
                return historialdepagos.idHistorialDePago;
            }
        }

        public async Task Borrar(long idHistorialDePago)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistorialDePagos.FirstOrDefaultAsync(x => x.idHistorialDePago == idHistorialDePago);
                _dbcontext.THistorialDePagos.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<HistorialDePagos> ConsultarPorId(long idHistorialDePago)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistorialDePagos.FirstOrDefaultAsync(x => x.idHistorialDePago == idHistorialDePago);
                return obj == null ? new HistorialDePagos() : obj;
            }
        }

        public async Task<List<HistorialDePagos>> ConsultarTodos()
        {
            return await _dbcontext.THistorialDePagos.ToListAsync();
        }

        public async Task<bool> Editar(long idHistorialDePago, HistorialDePagos historialdepagos)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistorialDePagos.FirstOrDefaultAsync(x => x.idHistorialDePago == idHistorialDePago);
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(historialdepagos);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
            }
        }
    }

    public interface IHistorialDePagosServicios
    {
        Task<long> Agregar(HistorialDePagos historialdepagos);
        Task<bool> Editar(long idHistorialDePago, HistorialDePagos historialdepagos);
        Task<HistorialDePagos> ConsultarPorId(long idHistorialDePago);
        Task<List<HistorialDePagos>> ConsultarTodos();
        Task Borrar(long idHistorialDePago);
    }
}