using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientesController : ControllerBase
    {   
        private readonly IClientesServicios _clientesServicios;
        public ClientesController(IClientesServicios clientesServicios)
        {
            _clientesServicios = clientesServicios;
        }
        [HttpGet]
        [Route("{idCliente}")]
        public async Task<IActionResult> Get(int idCliente)
        {
            return Ok(await _clientesServicios.ConsultarPorId(idCliente));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _clientesServicios.ConsultarTodos());
        }
        [HttpPut]
        [Route("{idCliente}")]
        public async Task<IActionResult> Put(int idCliente, [FromBody] Clientes obj)
        {
            return Ok(await _clientesServicios.Editar(idCliente, obj));
        }
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] Clientes obj)
        {
            return Ok(await _clientesServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idCliente}")]
        public async Task<IActionResult> Delete(int idCliente)
        {
            await _clientesServicios.Borrar(idCliente);
            return Ok();
        }
    }
}




