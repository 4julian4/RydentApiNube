using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
	public partial class RydentHub
	{


		// =========================================================
		// ESTADO DE CUENTA (NORMALIZADO A SEDEID -> WORKER)
		// =========================================================

		public async Task ConsultarEstadoCuenta(long sedeId, string modeloDatosParaConsultarEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"ConsultarEstadoCuenta",
						returnId,
						modeloDatosParaConsultarEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al ConsultarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaConsultarEstadoCuenta(string clienteId, string respuestaConsultarEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaConsultarEstadoCuenta",
					returnId,
					respuestaConsultarEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaConsultarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task PrepararEstadoCuenta(long sedeId, string modeloPrepararEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PrepararEstadoCuenta",
						returnId,
						modeloPrepararEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al PrepararEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararEstadoCuenta(string clienteId, string respuestaPrepararEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaPrepararEstadoCuenta",
					returnId,
					respuestaPrepararEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPrepararEstadoCuenta: {ex.Message}");
			}
		}

		public async Task CrearEstadoCuenta(long sedeId, string modeloCrearEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"CrearEstadoCuenta",
						returnId,
						modeloCrearEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al CrearEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaCrearEstadoCuenta(string clienteId, string respuestaCrearEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaCrearEstadoCuenta",
					returnId,
					respuestaCrearEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaCrearEstadoCuenta: {ex.Message}");
			}
		}

		public async Task PrepararEditarEstadoCuenta(long sedeId, string modeloPrepararEditarEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PrepararEditarEstadoCuenta",
						returnId,
						modeloPrepararEditarEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al PrepararEditarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararEditarEstadoCuenta(string clienteId, string respuestaPrepararEditarEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaPrepararEditarEstadoCuenta",
					returnId,
					respuestaPrepararEditarEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPrepararEditarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task EditarEstadoCuenta(long sedeId, string modeloEditarEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"EditarEstadoCuenta",
						returnId,
						modeloEditarEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al EditarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaEditarEstadoCuenta(string clienteId, string respuestaEditarEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaEditarEstadoCuenta",
					returnId,
					respuestaEditarEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaEditarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task BorrarEstadoCuenta(long sedeId, string modeloBorrarEstadoCuenta)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"BorrarEstadoCuenta",
						returnId,
						modeloBorrarEstadoCuenta
					);
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

				Console.Error.WriteLine($"Error al BorrarEstadoCuenta: {ex.Message}");
			}
		}

		public async Task RespuestaBorrarEstadoCuenta(string clienteId, string respuestaBorrarEstadoCuenta)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaBorrarEstadoCuenta",
					returnId,
					respuestaBorrarEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaBorrarEstadoCuenta: {ex.Message}");
			}
		}


		// ======================================================
		// SUGERIDOS ABONO (Front -> Hub -> Worker)  [TARGET = sedeId]
		// ======================================================
		public async Task ConsultarSugeridosAbono(long sedeId, string modeloConsultarSugeridosAbono)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"ConsultarSugeridosAbono",
						returnId,
						modeloConsultarSugeridosAbono
					);
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

				Console.Error.WriteLine($"Error al ConsultarSugeridosAbono: {ex.Message}");
			}
		}

		public async Task RespuestaConsultarSugeridosAbono(string clienteId, string respuestaConsultarSugeridosAbono)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaConsultarSugeridosAbono",
					returnId,
					respuestaConsultarSugeridosAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaConsultarSugeridosAbono: {ex.Message}");
			}
		}

		// ======================================================
		// PREPARAR INSERTAR ABONO (TARGET = sedeId)
		// ======================================================
		public async Task PrepararInsertarAbono(long sedeId, string modeloPrepararInsertarAbono)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PrepararInsertarAbono",
						returnId,
						modeloPrepararInsertarAbono
					);
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

				Console.Error.WriteLine($"Error al PrepararInsertarAbono: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararInsertarAbono(string clienteId, string respuestaPrepararInsertarAbono)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaPrepararInsertarAbono",
					returnId,
					respuestaPrepararInsertarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPrepararInsertarAbono: {ex.Message}");
			}
		}

		// ======================================================
		// INSERTAR ABONO (TARGET = sedeId)
		// ======================================================
		public async Task InsertarAbono(long sedeId, string modeloInsertarAbono)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"InsertarAbono",
						returnId,
						modeloInsertarAbono
					);
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

				Console.Error.WriteLine($"Error al InsertarAbono: {ex.Message}");
			}
		}

		public async Task RespuestaInsertarAbono(string clienteId, string respuestaInsertarAbono)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaInsertarAbono",
					returnId,
					respuestaInsertarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaInsertarAbono: {ex.Message}");
			}
		}

		// ======================================================
		// PREPARAR INSERTAR ADICIONAL (TARGET = sedeId)
		// ======================================================
		public async Task PrepararInsertarAdicional(long sedeId, string modeloPrepararInsertarAdicional)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PrepararInsertarAdicional",
						returnId,
						modeloPrepararInsertarAdicional
					);
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

				Console.Error.WriteLine($"Error al PrepararInsertarAdicional: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararInsertarAdicional(string clienteId, string respuestaPrepararInsertarAdicional)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaPrepararInsertarAdicional",
					returnId,
					respuestaPrepararInsertarAdicional
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPrepararInsertarAdicional: {ex.Message}");
			}
		}

		// ======================================================
		// INSERTAR ADICIONAL (TARGET = sedeId)
		// ======================================================
		public async Task InsertarAdicional(long sedeId, string modeloInsertarAdicional)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"InsertarAdicional",
						returnId,
						modeloInsertarAdicional
					);
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

				Console.Error.WriteLine($"Error al InsertarAdicional: {ex.Message}");
			}
		}

		public async Task RespuestaInsertarAdicional(string clienteId, string respuestaInsertarAdicional)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaInsertarAdicional",
					returnId,
					respuestaInsertarAdicional
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaInsertarAdicional: {ex.Message}");
			}
		}

		// ======================================================
		// PREPARAR BORRAR ABONO (TARGET = sedeId)
		// ======================================================
		public async Task PrepararBorrarAbono(long sedeId, string modeloPrepararBorrarAbono)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PrepararBorrarAbono",
						returnId,
						modeloPrepararBorrarAbono
					);
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

				Console.Error.WriteLine($"Error al PrepararBorrarAbono: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararBorrarAbono(string clienteId, string respuestaPrepararBorrarAbono)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaPrepararBorrarAbono",
					returnId,
					respuestaPrepararBorrarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPrepararBorrarAbono: {ex.Message}");
			}
		}

		// ======================================================
		// BORRAR ABONO (TARGET = sedeId)
		// ======================================================
		public async Task BorrarAbono(long sedeId, string modeloBorrarAbono)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"BorrarAbono",
						returnId,
						modeloBorrarAbono
					);
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

				Console.Error.WriteLine($"Error al BorrarAbono: {ex.Message}");
			}
		}

		public async Task RespuestaBorrarAbono(string clienteId, string respuestaBorrarAbono)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId).SendAsync(
					"RespuestaBorrarAbono",
					returnId,
					respuestaBorrarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaBorrarAbono: {ex.Message}");
			}
		}


	}
}
