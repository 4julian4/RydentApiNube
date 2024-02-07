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
    public class SedesController : ControllerBase
    {
        private readonly ISedesServicios _sedesServicios;
        public SedesController(ISedesServicios sedesServicios)
        {
            _sedesServicios = sedesServicios;
        }
        [HttpGet]
        [Route("{idSede}")]
        public async Task<IActionResult> Get(int idSede)
        {
            return Ok(await _sedesServicios.ConsultarPorId(idSede));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _sedesServicios.ConsultarTodos());
        }
        [HttpGet]
        [Route("ConsultarSedePorIdentificadorLocal/{identificadorLocal}")]
        public async Task<IActionResult> ConsultarSedePorIdentificadorLocal(string identificadorLocal)
        {
            return Ok(await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal));
        }
        [HttpGet]
        [Route("ConsultarPorIdCliente/{idCliente}")]
        public async Task<IActionResult> ConsultarPorIdCliente(int idCliente)
        {
            return Ok(await _sedesServicios.ConsultarPorIdCliente(idCliente));
        }
        [HttpPut]
        [Route("{idSede}")]
        public async Task<IActionResult> Put(int idSede, [FromBody] Sedes obj)
        {
            return Ok(await _sedesServicios.Editar(idSede, obj));
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] Sedes obj)
        {
            return Ok(await _sedesServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idSede}")]
        public async Task<IActionResult> Delete(int idSede)
        {
            await _sedesServicios.Borrar(idSede);
            return Ok();
        }
    }
}
