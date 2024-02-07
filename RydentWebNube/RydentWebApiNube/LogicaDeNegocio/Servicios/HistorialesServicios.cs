using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class HistorialesServicios : IHistorialesServicios
    {
        protected readonly AppDbContext _dbcontext;
        public HistorialesServicios()
        {

        }


        public async Task<long> Agregar(Historiales historiales)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.THistoriales.Add(historiales);
                await _dbcontext.SaveChangesAsync();
                return historiales.idHistorial;
            }
        }

        public async Task Borrar(long idHistorial)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistoriales.FirstOrDefaultAsync(x => x.idHistorial == idHistorial);
                _dbcontext.THistoriales.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<Historiales> ConsultarPorId(long idHistorial)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistoriales.FirstOrDefaultAsync(x => x.idHistorial == idHistorial);
                return obj == null ? new Historiales() : obj;
            }
        }
        public async Task<List<Historiales>> ConsultarTodos()
        {
            return await _dbcontext.THistoriales.ToListAsync();
        }


        public async Task<bool> Editar(long idHistorial, Historiales historiales)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.THistoriales.FirstOrDefaultAsync(x => x.idHistorial == idHistorial);
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(historiales);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
            }
        }
    }

    public interface IHistorialesServicios
    {
        Task<long> Agregar(Historiales historiales);
        Task<bool> Editar(long idHistorial, Historiales historiales);
        Task<Historiales> ConsultarPorId(long idHistorial);
        Task<List<Historiales>> ConsultarTodos();
        Task Borrar(long idHistorial);
    }
}