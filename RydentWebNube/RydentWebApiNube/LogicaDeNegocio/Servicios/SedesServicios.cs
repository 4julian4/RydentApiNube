using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class SedesServicios : ISedesServicios
    {
        protected readonly AppDbContext _dbcontext;
        public SedesServicios()
        {

        }


        public async Task<long> Agregar(Sedes sedes)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.TSedes.Add(sedes);
                await _dbcontext.SaveChangesAsync();
                return sedes.idSede;
            }
        }

        public async Task Borrar(long idSede)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedes.FirstOrDefaultAsync(x => x.idSede == idSede);
                _dbcontext.TSedes.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<Sedes> ConsultarPorId(long idSede)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedes.FirstOrDefaultAsync(x => x.idSede == idSede);
                return obj == null ? new Sedes() : obj;
            }
        }

        public async Task<Sedes> ConsultarSedePorIdentificadorLocal(string identificadorLocal)
        {
            using (var _dbcontext = new AppDbContext())
            {
                try
                {
                    var obj = await _dbcontext.TSedes.FirstOrDefaultAsync(x => x.identificadorLocal == identificadorLocal);
                    return obj == null ? new Sedes() : obj;
                }
                catch (Exception e)
                {

                    return new Sedes();
                }
            }
        }

        public async Task<List<Sedes>> ConsultarTodos()
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TSedes.ToListAsync();
            }
        }

        public async Task<List<Sedes>> ConsultarPorIdCliente(long idCliente)
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TSedes.Where(x => x.idCliente == idCliente).ToListAsync();
            }
        }


        public async Task<bool> Editar(long idSede, Sedes sedes)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedes.FirstOrDefaultAsync(x => x.idSede == idSede);
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(sedes);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
            }
        }
    }

    public interface ISedesServicios
    {
        Task<long> Agregar(Sedes sedes);
        Task<bool> Editar(long idSede, Sedes sedes);
        Task<Sedes> ConsultarPorId(long idSede);
        Task<Sedes> ConsultarSedePorIdentificadorLocal(string identificadorLocal);
        Task<List<Sedes>> ConsultarTodos();
        Task<List<Sedes>> ConsultarPorIdCliente(long idCliente);
        Task Borrar(long idSede);
    }
}

