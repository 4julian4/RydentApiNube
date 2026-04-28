using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public partial class RydentHub
    {

        public async Task ObtenerPin(long sedeId, string pin, int maxIdAnamnesis)
        {
            try
            {
                var returnId = Context.ConnectionId; // Guardamos el ID del Angular que pidió los datos

                // 1. Buscamos al Worker local de la clínica
                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);
                if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerPin", returnId, pin, maxIdAnamnesis);
				}
				else
				{
					await Clients.Client(returnId)
						.SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
				}

                //if (!string.IsNullOrWhiteSpace(workerConnId))
                //{
                //    // 2. Empaquetamos los parámetros que Angular nos dio en un Diccionario
                //    var parametros = new Dictionary<string, object>
                //    {
                //        { "pin", pin },
                //        { "maxIdAnamnesis", maxIdAnamnesis }
                //    };

                //    // 3. LA MAGIA: El Gestor busca el archivo "AccionObtenerPin.cs" basándose en el nombre "OBTENERPIN"
                //    // y ejecuta la función GenerarLote() que escribimos, devolviendo el Mega-SQL listo.
                //    var lote = _gestorAccionesWorker.GenerarLote("OBTENERPIN", parametros);

                //    // 4. Enviamos el SQL al Worker local (V2) por SignalR
                //    await Clients.Client(workerConnId).SendAsync("EjecutarLoteSQLDesdeLaNube", returnId, lote);
                //}
                //else
                //{
                //    await Clients.Client(returnId).SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
                //}
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);
            }
        }

        public async Task RespuestaObtenerPin(string clienteId, string respuestaPin)
        {
            // clienteId AQUI realmente es el "RETURN ID" (connectionId del browser que pidió)
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerPin", returnId, respuestaPin);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerPin: {ex.Message}");
            }
        }




        public async Task ObtenerDoctor(string clienteId, string idDoctor)
        {
            // clienteId aquí es el TARGET (idActualSignalR / sede a la que quiero llegar)
            var targetId = clienteId;

            // returnId real del browser
            var returnId = Context.ConnectionId;

            try
            {
                var workerConnId = await ValidarIdActualSignalR(targetId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    // Worker recibe: (returnId, idDoctor)
                    await Clients.Client(workerConnId)
                        .SendAsync("ObtenerDoctor", returnId, idDoctor);
                }
                else
                {
                    // ERROR hacia el browser (marcado con returnId)
                    await Clients.Client(returnId)
                        .SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(returnId)
                    .SendAsync("ErrorConexion", returnId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerDoctor: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerDoctor(string clienteId, string respuestaObtenerDoctor)
        {
            // clienteId aquí es el RETURN ID (connectionId del browser que pidió)
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerDoctor", returnId, respuestaObtenerDoctor);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerDoctor: {ex.Message}");
            }
        }



        public async Task ObtenerDoctorSiLoCambian(long sedeId, string idDoctor)
        {
            var returnId = Context.ConnectionId;

            try
            {
                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("ObtenerDoctorSiLoCambian", returnId, idDoctor);
                }
                else
                {
                    await Clients.Client(returnId)
                        .SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(returnId)
                    .SendAsync("ErrorConexion", returnId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerDoctorSiLoCambian: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerDoctorSiLoCambian(string clienteId, string respuestaObtenerDoctor)
        {
            // clienteId = RETURN-ID (connectionId del browser que pidió)
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerDoctorSiLoCambian", returnId, respuestaObtenerDoctor);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerDoctorSiLoCambian: {ex.Message}");
            }
        }


        // =========================================================
        // OBTENER CODIGOS EPS
        // =========================================================
        public async Task ObtenerCodigosEps(string clienteId)
        {
            // clienteId = TARGET (idActualSignalR / sede destino)
            var targetId = clienteId;

            try
            {
                var returnId = Context.ConnectionId;

                string workerConnId = await ValidarIdActualSignalR(targetId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("ObtenerCodigosEps", returnId);
                }
                else
                {
                    await Clients.Client(returnId)
                        .SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerCodigosEps: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerCodigosEps(string clienteId, string listadoeps)
        {
            // clienteId = RETURN-ID
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerCodigosEps", returnId, listadoeps);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerCodigosEps: {ex.Message}");
            }
        }




        // =========================================================
        // OBTENER DATOS ADMINISTRATIVOS
        // =========================================================
        public async Task ObtenerDatosAdministrativos(long sedeId, int idDoctor, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                var returnId = Context.ConnectionId;

                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("ObtenerDatosAdministrativos", returnId, idDoctor, fechaInicio, fechaFin);
                }
                else
                {
                    await Clients.Client(returnId)
                        .SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerDatosAdministrativos: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerDatosAdministrativos(string clienteId, string respuesta)
        {
            // clienteId = RETURN-ID (ConnectionId del front que pidió)
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerDatosAdministrativos", returnId, respuesta);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerDatosAdministrativos: {ex.Message}");
            }
        }



    }
}
