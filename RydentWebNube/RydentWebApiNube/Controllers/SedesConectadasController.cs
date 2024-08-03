using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;


namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SedesConectadasController : ControllerBase
    {
        private readonly ISedesConectadasServicios _sedesconectadasServicios;
        public SedesConectadasController(ISedesConectadasServicios sedesconectadasServicios)
        {
            _sedesconectadasServicios = sedesconectadasServicios;
        }
        [HttpGet]
        [Route("{idSedeConectada}")]
        public async Task<IActionResult> Get(int idSedeConectada)
        {
            return Ok(await _sedesconectadasServicios.ConsultarPorId(idSedeConectada));
        }
        [HttpGet]
        [Route("ConsultarSedePorId/{idSede}")]
        public async Task<IActionResult> ConsultarSedePorId(int idSede)
        {
            return Ok(await _sedesconectadasServicios.ConsultarSedePorId(idSede));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _sedesconectadasServicios.ConsultarTodos());
        }
        
        [HttpGet]
        [Route("ConsultarSedesConectadasActivasPorCliente/{idCliente}")]
        public async Task<IActionResult> ConsultarSedesConectadasActivasPorCliente(int idCliente)
        {
            return Ok(await _sedesconectadasServicios.ConsultarSedesConectadasActivasPorCliente(idCliente));
        }
       
        [HttpPut]
        [Route("{idSedeConectada}")]
        public async Task<IActionResult> Put(int idSedeConectada, [FromBody] SedesConectadas obj)
        {
            return Ok(await _sedesconectadasServicios.Editar(idSedeConectada, obj));
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] SedesConectadas obj)
        {
            return Ok(await _sedesconectadasServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idSedeConectada}")]
        public async Task<IActionResult> Delete(int idSedeConectada)
        {
            await _sedesconectadasServicios.Borrar(idSedeConectada);
            return Ok();
        }
    }
}
