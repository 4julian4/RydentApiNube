using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
	public class CatalogosServicios : ICatalogosServicios
	{
		public async Task<List<CodigosEps>> ConsultarEps()
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_EPS.AsNoTracking().ToListAsync();
			}
		}

		public async Task<List<CodigosDepartamento>> ConsultarDepartamentos()
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_DEPARTAMENTO.AsNoTracking().ToListAsync();
			}
		}

		public async Task<List<CodigosCiudad>> ConsultarCiudadesPorDepartamento(string codigoDepartamento)
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_CIUDAD.AsNoTracking()
					.Where(x => x.CODIGO_DEPARTAMENTO == codigoDepartamento)
					.ToListAsync();
			}
		}

		// ✅ NUEVO: todas las ciudades (espejo del PIN - primer paso)
		public async Task<List<CodigosCiudad>> ConsultarCiudadesAll()
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_CIUDAD.AsNoTracking().ToListAsync();
			}
		}

		public async Task<List<CodigosConsultas>> ConsultarConsultas()
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_CONSLUTAS.AsNoTracking().ToListAsync();
			}
		}

		// ✅ para NO bajar 9k completos en el login
		public async Task<List<CodigosProcedimientos>> BuscarProcedimientos(string term, int take)
		{
			term = (term ?? "").Trim();
			take = (take <= 0 || take > 200) ? 50 : take;

			using (var _dbcontext = new AppDbContext())
			{
				var q = _dbcontext.TCODIGOS_PROCEDIMIENTOS.AsNoTracking().AsQueryable();

				if (!string.IsNullOrWhiteSpace(term))
				{
					q = q.Where(x =>
						(x.CODIGO != null && x.CODIGO.Contains(term)) ||
						(x.NOMBRE != null && x.NOMBRE.Contains(term))
					);
				}

				return await q
					.OrderBy(x => x.CODIGO)
					.Take(take)
					.ToListAsync();
			}
		}

		// ✅ NUEVO: todos los procedimientos (espejo del PIN - primer paso)
		public async Task<List<CodigosProcedimientos>> ConsultarProcedimientosAll()
		{
			using (var _dbcontext = new AppDbContext())
			{
				return await _dbcontext.TCODIGOS_PROCEDIMIENTOS.AsNoTracking().ToListAsync();
			}
		}
	}

	public interface ICatalogosServicios
	{
		Task<List<CodigosEps>> ConsultarEps();
		Task<List<CodigosDepartamento>> ConsultarDepartamentos();

		Task<List<CodigosCiudad>> ConsultarCiudadesPorDepartamento(string codigoDepartamento);
		Task<List<CodigosCiudad>> ConsultarCiudadesAll(); // ✅ NUEVO

		Task<List<CodigosConsultas>> ConsultarConsultas();

		Task<List<CodigosProcedimientos>> BuscarProcedimientos(string term, int take);
		Task<List<CodigosProcedimientos>> ConsultarProcedimientosAll(); // ✅ NUEVO
	}
}