using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

namespace RydentWebApiNube.Controllers
{
	[ApiController]
	[Route("api/productos")]
	public class ProductoController : ControllerBase
	{
		private readonly IProductoService _productoService;

		public ProductoController(IProductoService productoService)
		{
			_productoService = productoService;
		}

		[HttpGet("activos")]
		public async Task<IActionResult> ListarActivos()
		{
			try
			{
				var productos = await _productoService.ListarProductosActivosAsync();
				return Ok(productos);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					mensaje = "Ocurrió un error consultando los productos activos.",
					detalle = ex.Message
				});
			}
		}

		[HttpGet("{idProducto:long}")]
		public async Task<IActionResult> ObtenerPorId(long idProducto)
		{
			try
			{
				var producto = await _productoService.ObtenerProductoPorIdAsync(idProducto);

				if (producto == null)
				{
					return NotFound(new
					{
						mensaje = "Producto no encontrado."
					});
				}

				return Ok(producto);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					mensaje = "Ocurrió un error consultando el producto.",
					detalle = ex.Message
				});
			}
		}

		[HttpGet("codigo/{codigo}")]
		public async Task<IActionResult> ObtenerPorCodigo(string codigo)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(codigo))
				{
					return BadRequest(new
					{
						mensaje = "Debe enviar un código válido."
					});
				}

				var producto = await _productoService.ObtenerProductoPorCodigoAsync(codigo);

				if (producto == null)
				{
					return NotFound(new
					{
						mensaje = "Producto no encontrado."
					});
				}

				return Ok(producto);
			}
			catch (Exception ex)
			{
				return StatusCode(500, new
				{
					mensaje = "Ocurrió un error consultando el producto por código.",
					detalle = ex.Message
				});
			}
		}
	}
}
