using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public partial class RydentHub
    {
        public async Task BuscarPaciente(long sedeId, string tipoBuqueda, string valorDeBusqueda)
        {
            try
            {
                var returnId = Context.ConnectionId; // ✅ browser que pidió

                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId); // ✅ worker por sedeId

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("BuscarPaciente", returnId, tipoBuqueda, valorDeBusqueda);
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

                Console.Error.WriteLine($"Error al BuscarPaciente: {ex.Message}");
            }
        }

        public async Task RespuestaBuscarPaciente(string clienteId, string listPacientes)
        {
            // clienteId aquí = RETURN-ID
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaBuscarPaciente", returnId, listPacientes);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaBuscarPaciente: {ex.Message}");
            }
        }



        public async Task ObtenerDatosPersonalesCompletosPaciente(long sedeId, string idAnanesis)
        {
            try
            {
                var returnId = Context.ConnectionId;

                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("ObtenerDatosPersonalesCompletosPaciente", returnId, idAnanesis);
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

                Console.Error.WriteLine($"Error al ObtenerDatosPersonalesCompletosPaciente: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerDatosPersonalesCompletosPaciente(string clienteId, string paciente)
        {
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaObtenerDatosPersonalesCompletosPaciente", returnId, paciente);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaObtenerDatosPersonalesCompletosPaciente: {ex.Message}");
            }
        }




        public async Task GuardarDatosPersonales(long sedeId, string datosPersonales)
        {
            try
            {
                var returnId = Context.ConnectionId;

                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("GuardarDatosPersonales", returnId, datosPersonales);
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

                Console.Error.WriteLine($"Error al GuardarDatosPersonales: {ex.Message}");
            }
        }

        public async Task RespuestaGuardarDatosPersonales(string clienteId, string respuesta)
        {
            var returnId = clienteId;

            try
            {
                await Clients.Client(returnId)
                    .SendAsync("RespuestaGuardarDatosPersonales", returnId, respuesta);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar RespuestaGuardarDatosPersonales: {ex.Message}");
            }
        }


		public async Task EditarDatosPersonales(long sedeId, string datosPersonales)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("EditarDatosPersonales", returnId, datosPersonales);
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

				Console.Error.WriteLine($"Error al EditarDatosPersonales: {ex.Message}");
			}
		}

		public async Task RespuestaEditarDatosPersonales(string clienteId, string respuesta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaEditarDatosPersonales", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaEditarDatosPersonales: {ex.Message}");
			}
		}



		public async Task ObtenerAntecedentesPaciente(long sedeId, string idAnanesis)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerAntecedentesPaciente", returnId, idAnanesis);
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

				Console.Error.WriteLine($"Error al ObtenerAntecedentesPaciente: {ex.Message}");
			}
		}

		public async Task RespuestaObtenerAntecedentesPaciente(string clienteId, string paciente)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerAntecedentesPaciente", returnId, paciente);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerAntecedentesPaciente: {ex.Message}");
			}
		}


// Editar antecedentes

		public async Task EditarAntecedentes(long sedeId, string antecedentesPaciente)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("EditarAntecedentes", returnId, antecedentesPaciente);
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

				Console.Error.WriteLine($"Error al EditarAntecedentes: {ex.Message}");
			}
		}

		public async Task RespuestaEditarAntecedentes(string clienteId, string respuesta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaEditarAntecedentes", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaEditarAntecedentes: {ex.Message}");
			}
		}



		public async Task ObtenerDatosEvolucion(long sedeId, string idAnanesis)
		{
			try
			{
				// ✅ ReturnId real del browser
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sedeId
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Worker recibe: (returnId, idAnanesis)
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerDatosEvolucion", returnId, idAnanesis);
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

				Console.Error.WriteLine($"Error al ObtenerDatosEvolucion: {ex.Message}");
			}
		}

		public async Task RespuestaObtenerDatosEvolucion(string clienteId, string evolucion)
		{
			// clienteId aquí = RETURN-ID
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerDatosEvolucion", returnId, evolucion);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerDatosEvolucion: {ex.Message}");
			}
		}


		public async Task GuardarDatosEvolucion(long sedeId, string evolucion)
		{
			try
			{
				// ✅ ReturnId real del browser
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sedeId
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Worker recibe: (returnId, evolucion)
					await Clients.Client(workerConnId)
						.SendAsync("GuardarDatosEvolucion", returnId, evolucion);
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

				Console.Error.WriteLine($"Error al GuardarDatosEvolucion: {ex.Message}");
			}
		}

		public async Task RespuestaGuardarDatosEvolucion(string clienteId, string respuesta)
		{
			// clienteId aquí = RETURN-ID
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaGuardarDatosEvolucion", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaGuardarDatosEvolucion: {ex.Message}");
			}
		}

		
		public async Task ObtenerLotePacientesAgenda(long sedeId, int maxIdAnamnesis)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("RecibirLotePacientesAgenda", returnId, maxIdAnamnesis);
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
			}
		}

		public async Task RespuestaLotePacientesAgenda(string clienteId, string respuestaPacientes)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaLotePacientesAgenda", returnId, respuestaPacientes);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaLotePacientesAgenda: {ex.Message}");
			}
		}

    }
}
