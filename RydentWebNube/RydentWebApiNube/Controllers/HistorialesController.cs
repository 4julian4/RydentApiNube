using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;



namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HistorialesController : ControllerBase
    {
        private readonly IHistorialesServicios _historialesServicios;
        public HistorialesController(IHistorialesServicios historialesServicios)
        {
            _historialesServicios = historialesServicios;
        }
        [HttpGet]
        [Route("{idHistorial}")]
        public async Task<IActionResult> Get(int idHistorial)
        {
            return Ok(await _historialesServicios.ConsultarPorId(idHistorial));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _historialesServicios.ConsultarTodos());
        }
        [HttpPut]
        [Route("{idHistorial}")]
        public async Task<IActionResult> Put(int idHistorial, [FromBody] Historiales obj)
        {
            return Ok(await _historialesServicios.Editar(idHistorial, obj));
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] Historiales obj)
        {
            return Ok(await _historialesServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idHistorial}")]
        public async Task<IActionResult> Delete(int idHistorial)
        {
            await _historialesServicios.Borrar(idHistorial);
            return Ok();
        }
    }
}
