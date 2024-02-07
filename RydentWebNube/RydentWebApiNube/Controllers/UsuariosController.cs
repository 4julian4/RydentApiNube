using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;


namespace RydentWebApiNube.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsuariosController : ControllerBase
    {
        private readonly IUsuariosServicios _usuariosServicios;
        public UsuariosController(IUsuariosServicios usuariosServicios)
        {
            _usuariosServicios = usuariosServicios;
        }
        [HttpGet]
        [Route("{idUsuario}")]
        public async Task<IActionResult> Get(int idUsuario)
        {
            return Ok(await _usuariosServicios.ConsultarPorId(idUsuario));
        }
        [HttpGet]
        [Route("ConsultarPorCorreo/{correoUsuario}")]
        public async Task<IActionResult> ConsultarPorCorreo(string correoUsuario)
        {
            return Ok(await _usuariosServicios.ConsultarPorCorreo(correoUsuario));
        }
        [HttpGet]
        [Route("")]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _usuariosServicios.ConsultarTodos());
        }
        [HttpGet]
        [Route("ConsultarCorreoyFechaActivo/{correoUsuario}")] 
        public async Task<IActionResult> ConsultarCorreoyFechaActivo(string correoUsuario)
        {
            var result = await _usuariosServicios.ConsultarCorreoyFechaActivo(correoUsuario);
            
            var strMessage = "";
            if (result == 2)
            {
                strMessage = "No existe el Usuario";
            }
            else
            {
                if (result == 1)
                {
                    strMessage = "El usuario esta Activo";
                }
                else
                {
                    strMessage = "El usuario no esta Activo";
                }
            }
           // creamos objeto anonimo para enviar la respuesta y podemos enviar el estado y el mesanje sguen el estado
            var response = new { status = result, message = strMessage };
            return Ok(response);
        }

        [HttpPut]
        [Route("{idUsuario}")]
        public async Task<IActionResult> Put(int idUsuario, [FromBody] Usuarios obj)
        {
            return Ok(await _usuariosServicios.Editar(idUsuario, obj));
        }

        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Post([FromBody] Usuarios obj)
        {
            return Ok(await _usuariosServicios.Agregar(obj));
        }
        [HttpDelete]
        [Route("{idUsuario}")]
        public async Task<IActionResult> Delete(int idUsuario)
        {
            await _usuariosServicios.Borrar(idUsuario);
            return Ok();
        }
    }
}
