using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public partial class RydentHub
    {
		public async Task ConsultarRdaControl(long sedeId, string filtroJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarRdaControl", returnId, filtroJson);
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

		public async Task RespuestaConsultarRdaControl(string clienteId, string payload)
		{
			await Clients.Client(clienteId).SendAsync("RespuestaConsultarRdaControl", clienteId, payload);
		}
		public async Task ReenviarRda(long sedeId, int idRda)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ReenviarRda", returnId, idRda);
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

		public async Task RespuestaReenviarRda(string clienteId, string payload)
		{
			await Clients.Client(clienteId).SendAsync("RespuestaReenviarRda", clienteId, payload);
		}

		public async Task RegenerarRda(long sedeId, int idRda)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("RegenerarRda", returnId, idRda);
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

		public async Task RespuestaRegenerarRda(string clienteId, string payload)
		{
			await Clients.Client(clienteId).SendAsync("RespuestaRegenerarRda", clienteId, payload);
		}

		public async Task ConsultarDetalleRda(long sedeId, int idRda)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarDetalleRda", returnId, idRda);
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

		public async Task RespuestaConsultarDetalleRda(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarDetalleRda", clienteId, payload);
		}

		public async Task ConsultarHistorialRda(long sedeId, int idRda)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarHistorialRda", returnId, idRda);
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

		public async Task RespuestaConsultarHistorialRda(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarHistorialRda", clienteId, payload);
		}

		public async Task ReenviarRdaLote(long sedeId, string idsJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ReenviarRdaLote", returnId, idsJson);
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

		public async Task RespuestaReenviarRdaLote(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaReenviarRdaLote", clienteId, payload);
		}

		public async Task RegenerarRdaLote(long sedeId, string idsJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("RegenerarRdaLote", returnId, idsJson);
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

		public async Task RespuestaRegenerarRdaLote(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaRegenerarRdaLote", clienteId, payload);
		}

		public async Task ProgresoRda(string clienteId, string progresoJson)
		{
			try
			{
				var returnId = clienteId;

				await Clients.Client(returnId)
					.SendAsync("ProgresoRda", returnId, progresoJson);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar ProgresoRda: {ex.Message}");
			}
		}
        public async Task ConsultarPacienteInteroperabilidadExacto(long sedeId, string filtroJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarPacienteInteroperabilidadExacto", returnId, filtroJson);
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

		public async Task RespuestaConsultarPacienteInteroperabilidadExacto(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarPacienteInteroperabilidadExacto", clienteId, payload);
		}

		public async Task ConsultarPacienteInteroperabilidadSimilar(long sedeId, string filtroJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarPacienteInteroperabilidadSimilar", returnId, filtroJson);
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

		public async Task RespuestaConsultarPacienteInteroperabilidadSimilar(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarPacienteInteroperabilidadSimilar", clienteId, payload);
		}

		public async Task ConsultarRdaPacienteInteroperabilidad(long sedeId, string filtroJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarRdaPacienteInteroperabilidad", returnId, filtroJson);
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

		public async Task RespuestaConsultarRdaPacienteInteroperabilidad(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarRdaPacienteInteroperabilidad", clienteId, payload);
		}

		public async Task ConsultarEncuentrosPacienteInteroperabilidad(long sedeId, string filtroJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarEncuentrosPacienteInteroperabilidad", returnId, filtroJson);
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

		public async Task RespuestaConsultarEncuentrosPacienteInteroperabilidad(string clienteId, string payload)
		{
			await Clients.Client(clienteId)
				.SendAsync("RespuestaConsultarEncuentrosPacienteInteroperabilidad", clienteId, payload);
		}

		public async Task GenerarRdaDesdeRipsExistente(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("GenerarRdaDesdeRipsExistente", returnId, payloadJson);
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

				Console.Error.WriteLine($"Error al GenerarRdaDesdeRipsExistente: {ex.Message}");
			}
		}

		public async Task RespuestaGenerarRdaDesdeRipsExistente(string clienteId, bool respuesta, string? mensaje)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaGenerarRdaDesdeRipsExistente", returnId, respuesta, mensaje);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaGenerarRdaDesdeRipsExistente: {ex.Message}");
			}
		}
        
    }
}