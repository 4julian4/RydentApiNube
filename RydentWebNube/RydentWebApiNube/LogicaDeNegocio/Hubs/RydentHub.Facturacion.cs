using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
	public partial class RydentHub
	{
// listar facturas entre fechas por id

		public async Task ObtenerFacturasPorIdEntreFechas(long sedeId, string modeloDatosParaConsultarFacturasEntreFechas)
		{
			try
			{
				// ✅ ReturnId real del browser que invocó
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sede
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Enviamos al worker: (returnId, payload)
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerFacturasPorIdEntreFechas", returnId, modeloDatosParaConsultarFacturasEntreFechas);
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

				Console.Error.WriteLine($"Error al ObtenerFacturasPorIdEntreFechas: {ex.Message}");
			}
		}

		public async Task RespuestaObtenerFacturasPorIdEntreFechas(string clienteId, string respuesta)
		{
			// clienteId = RETURN-ID (connectionId del browser que pidió)
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerFacturasPorIdEntreFechas", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerFacturasPorIdEntreFechas: {ex.Message}");
			}
		}


		// =========================================================


		// =========================================================
		// OBTENER FACTURAS PENDIENTES
		// =========================================================

		public async Task ObtenerFacturasPendientes(string clienteId)
		{
			// clienteId AQUÍ es el TARGET (idActualSignalR / sede destino)
			var targetId = clienteId;

			try
			{
				var returnId = Context.ConnectionId;

				string workerConnId = await ValidarIdActualSignalR(targetId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// Nota: aquí el worker debe saber a quién responder => returnId
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerFacturasPendientes", returnId);
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

				Console.Error.WriteLine($"Error al ObtenerFacturasPendientes: {ex.Message}");
			}
		}

		public async Task RespuestaObtenerFacturasPendientes(string clienteId, string facturasPendientes)
		{
			// clienteId AQUÍ es el RETURN-ID (connectionId del browser que pidió)
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerFacturasPendientes", returnId, facturasPendientes);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerFacturasPendientes: {ex.Message}");
			}
		}


		// =========================================================
		// OBTENER FACTURAS CREADAS
		// =========================================================

		public async Task ObtenerFacturasCreadas(string clienteId, string factura)
		{
			// clienteId AQUÍ es el TARGET (idActualSignalR / sede destino)
			var targetId = clienteId;

			try
			{
				var returnId = Context.ConnectionId;

				string workerConnId = await ValidarIdActualSignalR(targetId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerFacturasCreadas", returnId, factura ?? "");
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

				Console.Error.WriteLine($"Error al ObtenerFacturasCreadas: {ex.Message}");
			}
		}

		public async Task RespuestaObtenerFacturasCreadas(string clienteId, string facturasCreadas)
		{
			// clienteId AQUÍ es el RETURN-ID (connectionId del browser que pidió)
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaObtenerFacturasCreadas", returnId, facturasCreadas);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaObtenerFacturasCreadas: {ex.Message}");
			}
		}


		// =========================================================
		// PRESENTAR FACTURAS EN DIAN (ya lo tenías bien; lo ajusto al estándar)
		// =========================================================


		public async Task PresentarFacturasEnDian(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;

				// ✅ Resuelve connId real del worker por sede (RAM -> SQL fallback)
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId).SendAsync(
						"PresentarFacturasEnDian",
						returnId,   // a quién devolver / a quién enviar progreso
						payloadJson
					);
				}
				else
				{
					await Clients.Client(returnId)
						.SendAsync("ErrorConexion", returnId, "No se encontró conexión activa para la sede");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

				Console.Error.WriteLine($"Error en PresentarFacturasEnDian: {ex.Message}");
			}
		}

		// =========================================================
		// WORKER -> HUB -> FRONT (PROGRESO)
		// returnId = connectionId del browser
		// =========================================================
		public async Task RespuestaProgresoPresentacion(string clienteIdDestino, string progresoJson)
		{
			var returnId = clienteIdDestino;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("ProgresoPresentacionFactura", returnId, progresoJson);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al reenviar progreso: {ex.Message}");
			}
		}

		// =========================================================
		// WORKER -> HUB -> FRONT (RESUMEN FINAL)
		// returnId = connectionId del browser
		// =========================================================
		public async Task RespuestaPresentarFacturasEnDian(string clienteIdDestino, string resumenJson)
		{
			var returnId = clienteIdDestino;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaPresentarFacturasEnDian", returnId, resumenJson);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar resumen de presentación: {ex.Message}");
			}
		}
        

		// =========================================================
		// FRONT -> HUB -> WORKER
		// Descargar JSON factura pendiente
		// TARGET = sedeId
		// =========================================================
		public async Task DescargarJsonFacturaPendiente(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;

				// ✅ Resuelve connId real del worker por sede (RAM -> SQL fallback)
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(returnId)
						.SendAsync("ErrorConexion", returnId, "No se encontró conexión activa para la sede");
					return;
				}

				await Clients.Client(workerConnId).SendAsync(
					"DescargarJsonFacturaPendiente",
					returnId,     // a quién devolver (front)
					payloadJson
				);
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);
			}
		}

		// =========================================================
		// WORKER -> HUB -> FRONT (RESPUESTA JSON)
		// returnId = connectionId del browser
		// =========================================================
		public async Task RespuestaDescargarJsonFacturaPendiente(string clienteIdDestino, string jsonFactura)
		{
			var returnId = clienteIdDestino;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaDescargarJsonFacturaPendiente", returnId, jsonFactura);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar JSON: {ex.Message}");
			}
		}


		// =========================================================
		// GENERAR RIPS
		// =========================================================

		// =========================================================
		// RIPS - GENERAR (Front -> Hub -> Worker)
		// =========================================================
		public async Task GenerarRips(long sedeId, int identificador, string objGenerarRips)
		{
			try
			{
				// ✅ ReturnId real del browser que invocó
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sedeId (RAM/SQL)
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("GenerarRips", returnId, identificador, objGenerarRips);
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

				Console.Error.WriteLine($"Error al GenerarRips: {ex.Message}");
			}
		}

		// =========================================================
		// WORKER -> CLOUD -> FRONT (RESPUESTA FINAL)
		// =========================================================
		public async Task RespuestaGenerarRips(string clienteId, string respuesta)
		{
			// clienteId AQUÍ es el RETURN-ID (connectionId del browser que pidió)
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaGenerarRips", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaGenerarRips: {ex.Message}");
			}
		}


		// =========================================================
		// RIPS - PRESENTAR (Front -> Hub -> Worker)
		// =========================================================
		public async Task PresentarRips(long sedeId, int identificador, string objPresentarRips)
		{
			try
			{
				// ✅ ReturnId real del browser que invocó
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sedeId (RAM/SQL)
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Enviamos al worker: (returnId, identificador, payload)
					await Clients.Client(workerConnId)
						.SendAsync("PresentarRips", returnId, identificador, objPresentarRips);
				}
				else
				{
					// ✅ Error hacia el browser (returnId)
					await Clients.Client(returnId)
						.SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

				Console.Error.WriteLine($"Error al PresentarRips: {ex.Message}");
			}
		}

		// =========================================================
		// WORKER -> CLOUD -> FRONT (RESPUESTA FINAL)
		// =========================================================
		public async Task RespuestaPresentarRips(string clienteId, string respuesta)
		{
			// clienteId AQUÍ es el RETURN-ID (connectionId del browser que pidió)
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaPresentarRips", returnId, respuesta);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaPresentarRips: {ex.Message}");
			}
		}

		// =========================================================
		// WORKER -> CLOUD -> FRONT (PROGRESO)
		// =========================================================
		public async Task ProgresoRips(string clienteId, string progresoJson)
		{
			try
			{
				// clienteId aquí ES el RETURN-ID (ConnectionId del navegador Angular)
				var returnId = clienteId;

				await Clients.Client(returnId)
					.SendAsync("ProgresoRips", returnId, progresoJson);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar ProgresoRips: {ex.Message}");
			}
		}



		public async Task GuardarDatosRips(long sedeId, string datosRips)
		{
			try
			{
				// ✅ ReturnId real del browser que invocó
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por sede
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Enviamos al worker: (returnId, datosRips)
					await Clients.Client(workerConnId)
						.SendAsync("GuardarDatosRips", returnId, datosRips);
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

				Console.Error.WriteLine($"Error al GuardarDatosRips: {ex.Message}");
			}
		}

		public async Task RespuestaGuardarDatosRips(string clienteId, bool respuesta, string? mensaje)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaGuardarDatosRips", returnId, respuesta, mensaje);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar RespuestaGuardarDatosRips: {ex.Message}");
			}
		}

		public async Task ConsultarRipsExistentes(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarRipsExistentes", returnId, payloadJson);
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

				Console.Error.WriteLine($"Error ConsultarRipsExistentes: {ex.Message}");
			}
		}
		public async Task RespuestaConsultarRipsExistentes(string clienteId, string payload)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaConsultarRipsExistentes", returnId, payload);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error RespuestaConsultarRipsExistentes: {ex.Message}");
			}
		}
		public async Task EliminarRipsPorLlave(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("EliminarRipsPorLlave", returnId, payloadJson);
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

				Console.Error.WriteLine($"Error EliminarRipsPorLlave: {ex.Message}");
			}
		}

		public async Task RespuestaEliminarRipsPorLlave(string clienteId, bool ok, string mensaje)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaEliminarRipsPorLlave", returnId, ok, mensaje);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error RespuestaEliminarRipsPorLlave: {ex.Message}");
			}
		}

		public async Task ConsultarRipsDetallePorLlave(long sedeId, string payloadJson)
		{
			try
			{
				var returnId = Context.ConnectionId;

				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					await Clients.Client(workerConnId)
						.SendAsync("ConsultarRipsDetallePorLlave", returnId, payloadJson);
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

				Console.Error.WriteLine($"Error ConsultarRipsDetallePorLlave: {ex.Message}");
			}
		}

		public async Task RespuestaConsultarRipsDetallePorLlave(string clienteId, string payload)
		{
			var returnId = clienteId;

			try
			{
				await Clients.Client(returnId)
					.SendAsync("RespuestaConsultarRipsDetallePorLlave", returnId, payload);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error RespuestaConsultarRipsDetallePorLlave: {ex.Message}");
			}
		}
	}
}
