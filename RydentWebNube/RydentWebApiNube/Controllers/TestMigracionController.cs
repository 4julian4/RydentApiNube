using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using RydentWebApiNube.LogicaDeNegocio.Hubs;
using RydentWebApiNube.LogicaDeNegocio.Servicios; // Donde viva tu ISedesServicios

namespace RydentWebApiNube.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestMigracionController : ControllerBase
    {
        private readonly IHubContext<RydentHub> _hubContext;
        private readonly ISedesServicios _sedesServicios;
        // Si tu Hub usa PresenceRegistry para buscar a los clientes, inyéctalo:
        // private readonly WorkerPresenceRegistry _presence;

        public TestMigracionController(IHubContext<RydentHub> hubContext, ISedesServicios sedesServicios)
        {
            _hubContext = hubContext;
            _sedesServicios = sedesServicios;
        }

        [HttpGet("ProbarPin")]
        public async Task<IActionResult> ProbarPin(string pin = "1234") // Pon un pin real que exista en tu BD
        {
            // 1. Buscamos el ID de la Sede de prueba (Tu propia computadora)
            string identificadorLocal = Environment.MachineName; 
            var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);

            if (sede == null || sede.idSede <= 0)
                return BadRequest($"La Sede (PC: {identificadorLocal}) no está registrada en la BD de la Nube.");

            // 2. Aquí tendríamos que buscar el ID de conexión del Worker, 
            // pero como estamos simulando el Hub desde afuera (HTTP), es mejor invocar al método directamente 
            // usando la lógica que ya pusimos en RydentHub.
            
            // Para simplificar la prueba y no simular un contexto de SignalR falso,
            // vamos a mandar un mensaje BROADCAST a todos los workers que pertenezcan a ese Grupo de Sede:
            // Recuerda que en tu Hub pusiste: await Groups.AddToGroupAsync(connId, $"SEDE:{sede.idSede}");

            // ARMAMOS EL MEGA-SOBRE PARA EL WORKER V2
            var fabrica = new v2.Servicios.GestorAccionesWorkerService(new[] { new v2.Acciones.AccionObtenerPin() });
            var parametros = new Dictionary<string, object> 
            { 
                { "pin", pin }, 
                { "maxIdAnamnesis", 0 } // Ponemos 0 para que traiga los pacientes
            };

            var lote = fabrica.GenerarLote("OBTENERPIN", parametros);

            // MANDAMOS LA ORDEN AL GRUPO DE LA SEDE
            // Como este es un request HTTP, no tenemos un "returnId" de Angular. 
            // Mandaremos "HTTP_TEST" para que el Worker le responda al aire (solo nos interesa ver si el Worker lo ejecuta).
            await _hubContext.Clients.Group($"SEDE:{sede.idSede}").SendAsync("EjecutarLoteSQLDesdeLaNube", "HTTP_TEST", lote);

            return Ok($"¡Orden V2 enviada al Worker de la sede {sede.idSede}! Revisa la consola negra del Worker local.");
        }
    }
}