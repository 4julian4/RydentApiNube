using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;



namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialDePagosController : ControllerBase
    {
        private readonly IHistorialDePagosServicios _historialdepagosServicios;
        public HistorialDePagosController(IHistorialDePagosServicios historialdepagosServicios)
        {
            _historialdepagosServicios = historialdepagosServicios;
        }
        [HttpGet]
        [Route("{idHistorialDePago}")]
        public async Task<IActionResult> Get(int idHistorialDePago)
        {
            return Ok(await _historialdepagosServicios.ConsultarPorId(idHistorialDePago));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _historialdepagosServicios.ConsultarTodos());
        }
        [HttpPut]
        [Route("{idHistorialDePago}")]
        public async Task<IActionResult> Put(int idHistorialDePago, [FromBody] HistorialDePagos obj)
        {
            return Ok(await _historialdepagosServicios.Editar(idHistorialDePago, obj));
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] HistorialDePagos obj)
        {
            return Ok(await _historialdepagosServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idHistorialDePago}")]
        public async Task<IActionResult> Delete(int idHistorialDePago)
        {
            await _historialdepagosServicios.Borrar(idHistorialDePago);
            return Ok();
        }
    }
}