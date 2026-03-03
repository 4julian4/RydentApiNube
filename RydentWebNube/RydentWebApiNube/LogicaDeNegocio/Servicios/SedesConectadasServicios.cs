using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Modelos;

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
				var obj = await _dbcontext.TSedesConectadas
					.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);

				if (obj != null)
				{
					_dbcontext.TSedesConectadas.Remove(obj);
					await _dbcontext.SaveChangesAsync();
				}
			}
		}

		public async Task<SedesConectadas> ConsultarPorId(long idSedeConectada)
		{
			using (var _dbcontext = new AppDbContext())
			{
				var obj = await _dbcontext.TSedesConectadas
					.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);

				return obj == null ? new SedesConectadas() : obj;
			}
		}

		/// <summary>
		/// ✅ Recomendado: si hay varias filas activas para la sede, toma la MÁS RECIENTE.
		/// </summary>
		public async Task<SedesConectadas> ConsultarSedePorId(long idSede)
		{
			using (var _dbcontext = new AppDbContext())
			{
				var obj = await _dbcontext.TSedesConectadas
					.Where(x => x.idSede == idSede && x.activo == true)
					.OrderByDescending(x => x.fechaUltimoAcceso)
					.FirstOrDefaultAsync();

				return obj == null ? new SedesConectadas() : obj;
			}
		}

		/// <summary>
		/// ✅ NUEVO: devuelve 1 fila "activa" por sede (la más reciente) y con connId.
		/// Ideal para fallback cuando RAM (WorkerPresenceRegistry) no tiene el dato.
		/// </summary>
		public async Task<SedesConectadas> ConsultarActivoPorSede(long idSede)
		{
			using (var _dbcontext = new AppDbContext())
			{
				var obj = await _dbcontext.TSedesConectadas
					.Where(x =>
						x.idSede == idSede &&
						x.activo == true &&
						x.idActualSignalR != null)
					.OrderByDescending(x => x.fechaUltimoAcceso)
					.FirstOrDefaultAsync();

				return obj == null ? new SedesConectadas() : obj;
			}
		}

		public async Task<SedesConectadas> ConsultarPorIdSignalR(string idActualSignalR)
		{
			using (var _dbcontext = new AppDbContext())
			{
				var obj = await _dbcontext.TSedesConectadas
					.FirstOrDefaultAsync(x => x.idActualSignalR == idActualSignalR);

				return obj == null ? new SedesConectadas() : obj;
			}
		}

		public async Task<List<SedesConectadas>> ConsultarPorSedeConEstadoActivo(long idSede)
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TSedesConectadas
					.Where(x => x.idSede == idSede && x.activo == true)
					.ToListAsync();
			}
		}

		public async Task<List<SedesConectadas>> ConsultarSedesConectadasActivasPorCliente(long idCliente)
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TSedesConectadas
					.Where(x => x.idCliente == idCliente && x.activo == true)
					.ToListAsync();
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
				var obj = await _dbcontext.TSedesConectadas
					.FirstOrDefaultAsync(x => x.idSedeConectada == idSedeConectada);

				if (obj == null)
				{
					return false;
				}

				_dbcontext.Entry(obj).CurrentValues.SetValues(sedesconectadas);
				await _dbcontext.SaveChangesAsync();
				return true;
			}
		}

		public async Task<List<SedeConectadaView>> ConsultarSedesActivasConCliente(int minutos)
		{
			using (var _dbcontext = new AppDbContext())
			{
				// ✅ filtro: activo=true y ultimo acceso dentro de ventana
				var desde = DateTime.Now.AddMinutes(-Math.Abs(minutos <= 0 ? 10 : minutos));

				// JOIN TSedesConectadas con TClientes por idCliente
				var query =
					from s in _dbcontext.TSedesConectadas
					join c in _dbcontext.TClientes on s.idCliente equals c.idCliente into cj
					from c in cj.DefaultIfEmpty()
					where s.activo == true
						  && s.fechaUltimoAcceso != null
						  && s.fechaUltimoAcceso >= desde
					orderby s.fechaUltimoAcceso descending
					select new SedeConectadaView
					{
						idSedeConectada = s.idSedeConectada,
						idCliente = s.idCliente,
						nombreCliente = c != null ? c.nombreCliente : null,
						idSede = s.idSede,
						idActualSignalR = s.idActualSignalR,
						fechaUltimoAcceso = s.fechaUltimoAcceso,
						activo = s.activo
					};

				return await query.ToListAsync();
			}
		}
	}

	public interface ISedesConectadasServicios
	{
		Task<long> Agregar(SedesConectadas sedesconectadas);
		Task<bool> Editar(long idSedeConectada, SedesConectadas sedesconectadas);

		Task<SedesConectadas> ConsultarPorId(long idSedeConectada);
		Task<SedesConectadas> ConsultarPorIdSignalR(string idActualSignalR);

		Task<SedesConectadas> ConsultarSedePorId(long idSede);

		// ✅ NUEVO
		Task<SedesConectadas> ConsultarActivoPorSede(long idSede);

		Task<List<SedesConectadas>> ConsultarTodos();
		Task<List<SedesConectadas>> ConsultarPorSedeConEstadoActivo(long idSede);
		Task<List<SedesConectadas>> ConsultarSedesConectadasActivasPorCliente(long idCliente);

		Task Borrar(long idSedeConectada);
		Task<List<RydentWebApiNube.LogicaDeNegocio.Modelos.SedeConectadaView>> ConsultarSedesActivasConCliente(int minutos);
	}
}