using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

namespace RydentWebApiNube.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CatalogosController : ControllerBase
	{
		private readonly ICatalogosServicios _catalogos;

		public CatalogosController(ICatalogosServicios catalogos)
		{
			_catalogos = catalogos;
		}

		// =========================
		// EPS
		// =========================
		[HttpGet("eps")]
		public async Task<IActionResult> Eps()
		{
			return Ok(await _catalogos.ConsultarEps());
		}

		// =========================
		// DEPARTAMENTOS
		// =========================
		[HttpGet("departamentos")]
		public async Task<IActionResult> Departamentos()
		{
			return Ok(await _catalogos.ConsultarDepartamentos());
		}

		// =========================
		// CIUDADES (por departamento)
		// =========================
		[HttpGet("ciudades")]
		public async Task<IActionResult> Ciudades([FromQuery] string codigoDepartamento)
		{
			return Ok(await _catalogos.ConsultarCiudadesPorDepartamento(codigoDepartamento));
		}

		// ✅ NUEVO: CIUDADES (todas) - para espejo del PIN (primer paso)
		[HttpGet("ciudades/all")]
		public async Task<IActionResult> CiudadesAll()
		{
			return Ok(await _catalogos.ConsultarCiudadesAll());
		}

		// =========================
		// CONSULTAS
		// =========================
		[HttpGet("consultas")]
		public async Task<IActionResult> Consultas()
		{
			return Ok(await _catalogos.ConsultarConsultas());
		}

		// =========================
		// PROCEDIMIENTOS (search)
		// =========================
		[HttpGet("procedimientos/search")]
		public async Task<IActionResult> BuscarProcedimientos([FromQuery] string term = "", [FromQuery] int take = 50)
		{
			return Ok(await _catalogos.BuscarProcedimientos(term, take));
		}

		// ✅ NUEVO: PROCEDIMIENTOS (todos) - para espejo del PIN (primer paso)
		[HttpGet("procedimientos/all")]
		public async Task<IActionResult> ProcedimientosAll()
		{
			return Ok(await _catalogos.ConsultarProcedimientosAll());
		}
	}
}