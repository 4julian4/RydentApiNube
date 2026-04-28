using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public partial class RydentHub
    {
        public async Task BuscarCitasPacienteAgenda(long sedeId, string valorBuscarAgenda)
        {
            var returnId = Context.ConnectionId;

            try
            {
                var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

                if (!string.IsNullOrWhiteSpace(workerConnId))
                {
                    await Clients.Client(workerConnId)
                        .SendAsync("BuscarCitasPacienteAgenda", returnId, valorBuscarAgenda);
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

                Console.Error.WriteLine($"Error en BuscarCitasPacienteAgenda: {ex.Message}");
            }
        }





		public async Task RespuestaBuscarCitasPacienteAgenda(string clienteId, string listPacientes)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaBuscarCitasPacienteAgenda", returnId, listPacientes);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaBuscarCitasPacienteAgenda: {ex.Message}");
			}
		}

        // =========================================================
		// OBTENER CONSULTA POR DIA Y POR UNIDAD
		// =========================================================
		public async Task ObtenerConsultaPorDiaYPorUnidad(long sedeId, string silla, DateTime fecha)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerConsultaPorDiaYPorUnidad", returnId, silla, fecha);
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

				Console.Error.WriteLine($"Error al ObtenerConsultaPorDiaYPorUnidad: {ex.Message}");
			}
		}
        

		// clienteId aquí = RETURN-ID (ConnectionId del front que pidió)
		public async Task RespuestaObtenerConsultaPorDiaYPorUnidad(string clienteId, string respuesta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerConsultaPorDiaYPorUnidad", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerConsultaPorDiaYPorUnidad: {ex.Message}");
			}
		}

        public async Task AgendarCita(long sedeId, string modelocrearcita)
		{
			var returnId = Context.ConnectionId;

			try
			{
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("AgendarCita", returnId, modelocrearcita);
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

				Console.Error.WriteLine($"Error en AgendarCita: {ex.Message}");
			}
		}

        
		public async Task RespuestaAgendarCita(string clienteId, string modelocrearcita)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaAgendarCita", returnId, modelocrearcita);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaAgendarCita: {ex.Message}");
			}
		}

        // =========================================================
		// REALIZAR ACCIONES EN CITA AGENDADA
		// =========================================================
		public async Task RealizarAccionesEnCitaAgendada(long sedeId, string modelorealizaraccionesenlacitaagendada)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("RealizarAccionesEnCitaAgendada", returnId, modelorealizaraccionesenlacitaagendada);
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

				Console.Error.WriteLine($"Error al RealizarAccionesEnCitaAgendada: {ex.Message}");
			}
		}

		
		public async Task RespuestaRealizarAccionesEnCitaAgendada(string clienteId, string modelorealizaraccionesenlacitaagendada)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaRealizarAccionesEnCitaAgendada", returnId, modelorealizaraccionesenlacitaagendada);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaRealizarAccionesEnCitaAgendada: {ex.Message}");
			}
		}


    }
}
