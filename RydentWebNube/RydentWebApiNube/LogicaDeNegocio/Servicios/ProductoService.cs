using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Modelos;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
	public class ProductoService : IProductoService
	{
		public async Task<List<ProductoDto>> ListarProductosActivosAsync()
		{
			using var db = new AppDbContext();

			return await db.Producto
				.AsNoTracking()
				.Where(x => x.Activo)
				.OrderBy(x => x.IdProducto)
				.Select(x => new ProductoDto
				{
					IdProducto = x.IdProducto,
					Codigo = x.Codigo,
					Nombre = x.Nombre,
					Descripcion = x.Descripcion,
					Precio = x.Precio,
					Moneda = x.Moneda,
					Activo = x.Activo,
					FechaCreacion = x.FechaCreacion,

					Beneficios = x.Beneficios
						.Where(b => b.Activo)
						.Select(b => new ProductoBeneficioDto
						{
							IdBeneficio = b.IdBeneficio,
							IdProducto = b.IdProducto,
							TipoBeneficio = b.TipoBeneficio,
							CantidadFacturas = b.CantidadFacturas,
							DiasAcceso = b.DiasAcceso,
							Activo = b.Activo
						})
						.ToList()
				})
				.ToListAsync();
		}

		public async Task<ProductoDto?> ObtenerProductoPorIdAsync(long idProducto)
		{
			using var db = new AppDbContext();

			return await db.Producto
				.AsNoTracking()
				.Where(x => x.IdProducto == idProducto && x.Activo)
				.Select(x => new ProductoDto
				{
					IdProducto = x.IdProducto,
					Codigo = x.Codigo,
					Nombre = x.Nombre,
					Descripcion = x.Descripcion,
					Precio = x.Precio,
					Moneda = x.Moneda,
					Activo = x.Activo,
					FechaCreacion = x.FechaCreacion,

					Beneficios = x.Beneficios
						.Where(b => b.Activo)
						.Select(b => new ProductoBeneficioDto
						{
							IdBeneficio = b.IdBeneficio,
							IdProducto = b.IdProducto,
							TipoBeneficio = b.TipoBeneficio,
							CantidadFacturas = b.CantidadFacturas,
							DiasAcceso = b.DiasAcceso,
							Activo = b.Activo
						})
						.ToList()
				})
				.FirstOrDefaultAsync();
		}

		public async Task<ProductoDto?> ObtenerProductoPorCodigoAsync(string codigo)
		{
			using var db = new AppDbContext();

			return await db.Producto
				.AsNoTracking()
				.Where(x => x.Codigo == codigo && x.Activo)
				.Select(x => new ProductoDto
				{
					IdProducto = x.IdProducto,
					Codigo = x.Codigo,
					Nombre = x.Nombre,
					Descripcion = x.Descripcion,
					Precio = x.Precio,
					Moneda = x.Moneda,
					Activo = x.Activo,
					FechaCreacion = x.FechaCreacion,

					Beneficios = x.Beneficios
						.Where(b => b.Activo)
						.Select(b => new ProductoBeneficioDto
						{
							IdBeneficio = b.IdBeneficio,
							IdProducto = b.IdProducto,
							TipoBeneficio = b.TipoBeneficio,
							CantidadFacturas = b.CantidadFacturas,
							DiasAcceso = b.DiasAcceso,
							Activo = b.Activo
						})
						.ToList()
				})
				.FirstOrDefaultAsync();
		}
	}
	public interface IProductoService
	{
		Task<List<ProductoDto>> ListarProductosActivosAsync();
		Task<ProductoDto?> ObtenerProductoPorIdAsync(long idProducto);
		Task<ProductoDto?> ObtenerProductoPorCodigoAsync(string codigo);
	}
}
