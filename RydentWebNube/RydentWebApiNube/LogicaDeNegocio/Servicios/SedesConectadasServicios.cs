using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class SedesConectadasServicios : ISedesConectadasServicios
    {
        protected readonly AppDbContext _dbcontext;
        public SedesConectadasServicios()
        {

        }


        public async Task<long> Agregar(SedesConectadas sedesconectadas)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.TSedesConectadas.Add(sedesconectadas);
                await _dbcontext.SaveChangesAsync();
                return sedesconectadas.idSedeConectada;
            }
        }

        public async Task Borrar(long idSedeConectada)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedesConectadas.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);
                _dbcontext.TSedesConectadas.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<SedesConectadas> ConsultarPorId(long idSedeConectada)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedesConectadas.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);
                return obj == null ? new SedesConectadas() : obj;
            }
        }

        public async Task<SedesConectadas> ConsultarPorIdSignalR(string idActualSignalR)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedesConectadas.FirstOrDefaultAsync(x => x.idActualSignalR == idActualSignalR);
                return obj == null ? new SedesConectadas() : obj;
            }
        }

        public async Task<List<SedesConectadas>> ConsultarPorSedeConEstadoActivo(long idSede)
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TSedesConectadas.Where(x => x.idSede == idSede && x.activo == true).ToListAsync();
            }
        }

        

        public async Task<List<SedesConectadas>> ConsultarSedesConectadasActivasPorCliente(long idCliente)
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TSedesConectadas.Where(x => x.idCliente == idCliente && x.activo == true).ToListAsync();
            }
        }

        public async Task<List<SedesConectadas>> ConsultarTodos()
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TSedesConectadas.ToListAsync();
            }
        }

        public async Task<bool> Editar(long idSedeConectada, SedesConectadas sedesconectadas)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TSedesConectadas.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(sedesconectadas);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
            }
        }
    }

    public interface ISedesConectadasServicios
    {
        Task<long> Agregar(SedesConectadas sedesconectadas);
        Task<bool> Editar(long idSedeConectada, SedesConectadas sedesconectadas);
        Task<SedesConectadas> ConsultarPorId(long idSedeConectada);
        Task<List<SedesConectadas>> ConsultarTodos();
        Task<List<SedesConectadas>> ConsultarPorSedeConEstadoActivo(long idSede);
        Task<List<SedesConectadas>> ConsultarSedesConectadasActivasPorCliente(long idCliente);
        Task<SedesConectadas> ConsultarPorIdSignalR(string idActualSignalR);

        Task Borrar(long idSedeConectada);
    }
}

