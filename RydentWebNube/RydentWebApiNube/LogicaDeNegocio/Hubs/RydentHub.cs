using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Services;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using RydentWebApiNube.Models.Google;
using RydentWebApiNube.Models.MSN;
using RydentWebApiNube.v2.Servicios;
using System;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
	public partial class RydentHub : Hub
	{
		private readonly ISedesServicios _sedesServicios;
		private readonly ISesionesUsuarioServicios _sesionesUsuarioServicios;
		private readonly ISedesConectadasServicios _sedesconectadasServicios;
		private static readonly HttpClient httpClient = new HttpClient();
		//private readonly HttpClient httpClient;
		private readonly IUsuariosServicios iUsuariosServicios;
		private readonly IConfiguration configuration;
		private readonly string GoogleRedirectURI;
		private readonly string GoogleClientId;
		private readonly string GoogleSecret;
		private readonly string GoogleTokenEndPoint;
		private readonly string GoogleAPI_EndPoint;
		private readonly string TokenEndPoint;
		private readonly string JWT_SECRET;
		private readonly string JWT;
		private readonly string RedirectURI;
		private readonly string ClientId;
		private readonly string Secret;
		private readonly string Scope;
		private readonly string API_EndPoint;
		private readonly WorkerPresenceRegistry _presence;

		private readonly IGestorAccionesWorkerService _gestorAccionesWorker;




		public RydentHub(
			ISedesServicios sedesServicios,
			ISedesConectadasServicios sedesconectadasServicios,
			ISesionesUsuarioServicios sesionesUsuarioServicios,
			IConfiguration configuration,
			IUsuariosServicios iUsuariosServicios,
			WorkerPresenceRegistry presence,
			IGestorAccionesWorkerService gestorAccionesWorker
			)
		{
			_sedesServicios = sedesServicios;
			_sedesconectadasServicios = sedesconectadasServicios;
			_sesionesUsuarioServicios = sesionesUsuarioServicios;
			this.iUsuariosServicios = iUsuariosServicios;
			_presence = presence;
			_gestorAccionesWorker = gestorAccionesWorker;
			this.GoogleTokenEndPoint = configuration["OAuthGoogle:TokenEndPoint"] ?? "";
			this.GoogleRedirectURI = configuration["OAuthGoogle:RedirectURI"] ?? "";
			this.GoogleClientId = configuration["OAUTH2_GOOGLE_CLIENTID"] ?? "";
			this.GoogleSecret = configuration["OAUTH2_GOOGLE_SECRET"] ?? "";
			this.GoogleAPI_EndPoint = configuration["OAuthGoogle:API_EndPoint"] ?? "";
			this.TokenEndPoint = configuration["OAuth:TokenEndPoint"] ?? "";
			this.JWT_SECRET = configuration["JWT_SECRET"] ?? "";
			this.JWT = configuration["Jwt:Issuer"] ?? "";
			this.RedirectURI = configuration["OAuth:RedirectURI"] ?? "";
			this.ClientId = configuration["OAUTH2_AZURE_CLIENTID"] ?? "";
			this.Secret = configuration["OAUTH2_AZURE_SECRET"] ?? "";
			this.Scope = configuration["OAuth:Scope"] ?? "";
			this.API_EndPoint = configuration["OAuth:API_EndPoint"] ?? "";
		}



		//autenticar google
		public async Task PostLoginCallbackGoogle(string clienteId, string code, string state, bool forzarCerrarAnterior = false)
		{
			// Diccionario con parámetros de autenticación
			string grant_type = "authorization_code";
			string stringURI = new Uri(this.GoogleRedirectURI).ToString();
			var BodyData = new Dictionary<string, string>
			{
				{ "code", code },
				{ "client_id", this.GoogleClientId },
				{ "client_secret", this.GoogleSecret },
				{ "redirect_uri", stringURI },
				{ "grant_type", grant_type }
			};


			var body = new FormUrlEncodedContent(BodyData);
			var response = await httpClient.PostAsync(this.GoogleTokenEndPoint, body).ConfigureAwait(false);
			var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);

			var result = new ExpandoObject() as IDictionary<string, Object>;
			result["respuesta"] = "";
			result["autenticado"] = false;

			if (response.IsSuccessStatusCode)
			{
				var accessToken = jsonContent.GetProperty("access_token").GetString();
				httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				string googleApiUrl = this.GoogleAPI_EndPoint + accessToken;

				var googleResponse = await httpClient.GetAsync(googleApiUrl).ConfigureAwait(false);
				if (googleResponse.IsSuccessStatusCode)
				{
					var googleData = await googleResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
					var googleUser = JsonSerializer.Deserialize<UsuarioGoogle>(googleData);

					if (!string.IsNullOrEmpty(googleUser?.email))
					{
						var usuario = await iUsuariosServicios.ConsultarPorCorreo(googleUser.email).ConfigureAwait(false);

						if (usuario == null || usuario.idUsuario <= 0)
						{
							result["respuesta"] = "";
							result["autenticado"] = false;
							result["mensaje"] = "Usuario no autorizado.";
						}
						else
						{
							var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
							var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();

							var sesion = await _sesionesUsuarioServicios.PrepararLoginAsync(
								usuario,
								forzarCerrarAnterior,
								ip,
								userAgent
							);

							if (sesion.RequiereConfirmacion)
							{
								result["respuesta"] = "";
								result["autenticado"] = false;
								result["requiereConfirmacion"] = true;
								result["mensaje"] = sesion.Mensaje;
							}
							else if (sesion.PuedeEntrar)
							{
								var jwtToken = generateJwtToken(usuario, sesion.SessionId);
								result["respuesta"] = jwtToken;
								result["autenticado"] = true;
								result["requiereConfirmacion"] = false;
							}
						}
					}
				}
			}

			// Emitir respuesta de autenticación a cliente específico
			string jsonResult = JsonSerializer.Serialize(result); // Convertir a string
			await Clients.Client(clienteId).SendAsync("RespuestaPostLoginCallbackGoogle", clienteId, jsonResult);
		}

		public async Task PostLoginCallback(
			string clienteId,
			string code,
			string state,
			bool forzarCerrarAnterior = false)
		{
			string grant_type = "authorization_code";

			var BodyData = new Dictionary<string, string>
			{
				{ "grant_type", grant_type },
				{ "code", code },
				{ "Redirect_uri", this.RedirectURI },
				{ "client_id", this.ClientId },
				{ "client_secret", this.Secret },
				{ "scope", this.Scope }
			};

			var result = new ExpandoObject() as IDictionary<string, object>;
			result["respuesta"] = "";
			result["autenticado"] = false;
			result["requiereConfirmacion"] = false;

			try
			{
				var body = new FormUrlEncodedContent(BodyData);
				var response = await httpClient.PostAsync(this.TokenEndPoint, body).ConfigureAwait(false);

				if (!response.IsSuccessStatusCode)
				{
					result["mensaje"] = "No fue posible obtener token de Microsoft.";
					await Clients.Client(clienteId).SendAsync(
						"RespuestaPostLoginCallback",
						clienteId,
						JsonSerializer.Serialize(result)
					);
					return;
				}

				var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);

				var accessToken = jsonContent.GetProperty("access_token").GetString();

				httpClient.DefaultRequestHeaders.Authorization =
					new AuthenticationHeaderValue("Bearer", accessToken);

				var response1 = await httpClient.GetAsync(API_EndPoint).ConfigureAwait(false);

				if (!response1.IsSuccessStatusCode)
				{
					result["mensaje"] = "No fue posible consultar el usuario de Microsoft.";
					await Clients.Client(clienteId).SendAsync(
						"RespuestaPostLoginCallback",
						clienteId,
						JsonSerializer.Serialize(result)
					);
					return;
				}

				var usrMSNAzure = await response1.Content.ReadAsStringAsync().ConfigureAwait(false);
				var jsUsuarioMSN = JsonSerializer.Deserialize<UsuarioMSN>(usrMSNAzure);

				var correo = jsUsuarioMSN?.mail;

				if (string.IsNullOrWhiteSpace(correo))
				{
					result["mensaje"] = "Microsoft no retornó correo del usuario.";
					await Clients.Client(clienteId).SendAsync(
						"RespuestaPostLoginCallback",
						clienteId,
						JsonSerializer.Serialize(result)
					);
					return;
				}

				var usuario = await iUsuariosServicios
					.ConsultarPorCorreo(correo)
					.ConfigureAwait(false);

				if (usuario == null || usuario.idUsuario <= 0)
				{
					result["mensaje"] = "Usuario no autorizado.";
					await Clients.Client(clienteId).SendAsync(
						"RespuestaPostLoginCallback",
						clienteId,
						JsonSerializer.Serialize(result)
					);
					return;
				}

				var ip = Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString();
				var userAgent = Context.GetHttpContext()?.Request.Headers["User-Agent"].ToString();

				var sesion = await _sesionesUsuarioServicios.PrepararLoginAsync(
					usuario,
					forzarCerrarAnterior,
					ip,
					userAgent
				);

				if (sesion.RequiereConfirmacion)
				{
					result["respuesta"] = "";
					result["autenticado"] = false;
					result["requiereConfirmacion"] = true;
					result["mensaje"] = sesion.Mensaje;
				}
				else if (sesion.PuedeEntrar)
				{
					var jwtToken = generateJwtToken(usuario, sesion.SessionId);

					result["respuesta"] = jwtToken;
					result["autenticado"] = true;
					result["requiereConfirmacion"] = false;
				}
			}
			catch (Exception ex)
			{
				result["respuesta"] = "";
				result["autenticado"] = false;
				result["requiereConfirmacion"] = false;
				result["mensaje"] = "Error iniciando sesión con Microsoft: " + ex.Message;
			}

			string jsonResult = JsonSerializer.Serialize(result);

			// Emitir respuesta de autenticación a cliente específico
			await Clients.Client(clienteId).SendAsync("RespuestaPostLoginCallback", clienteId, jsonResult);
		}



		/*private string generateJwtToken(Usuarios user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(this.JWT_SECRET);
            var lstClaims = new List<Claim>
            {
                new Claim("id", user.idUsuario.ToString()),
                new Claim("idCliente", user.idCliente.ToString()),
                new Claim("correo", user.correoUsuario.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(lstClaims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = this.JWT ?? "",
                Audience = this.JWT_SECRET ?? "",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }*/

		//cambio 24-04-2026

		private string generateJwtToken(Usuarios user, string sessionId)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(this.JWT_SECRET);

			var lstClaims = new List<Claim>
			{
				new Claim("id", user.idUsuario.ToString()),
				new Claim("idCliente", user.idCliente.ToString()),
				new Claim("correo", user.correoUsuario.ToString()),
				new Claim("sessionId", sessionId)
			};

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(lstClaims),
				Expires = DateTime.UtcNow.AddDays(7),
				Issuer = this.JWT ?? "",
				Audience = this.JWT_SECRET ?? "",
				SigningCredentials = new SigningCredentials(
					new SymmetricSecurityKey(key),
					SecurityAlgorithms.HmacSha256Signature
				)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}



		public override async Task OnDisconnectedAsync(Exception exception)
		{
			_presence.RemoveByConnection(Context.ConnectionId);

			try
			{
				var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(Context.ConnectionId);
				if (objSedesConectada?.idSedeConectada > 0 && (objSedesConectada.activo ?? false))
				{
					objSedesConectada.activo = false;
					await _sedesconectadasServicios.Editar(objSedesConectada.idSedeConectada, objSedesConectada);
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al manejar la desconexión: {ex.Message}");
			}
			await base.OnDisconnectedAsync(exception);
		}

		/// <summary>
		/// Heartbeat del worker: "estoy vivo".
		/// ✅ Versión definitiva: SIN parámetros.
		/// - El worker solo llama Heartbeat()
		/// - El hub usa Context.ConnectionId para resolver la sede en RAM
		/// - Marca el LastSeenUtc (MarkSeen)
		/// </summary>

		public Task Heartbeat()
		{
			var connId = Context.ConnectionId;

			// ✅ Si este connectionId ya está indexado, actualizamos LastSeen
			if (_presence.TryGetSedeByConnectionId(connId, out var idSede))
			{
				_presence.MarkSeen(idSede, connId);
			}

			// Si aún no está indexado, es porque el worker no se ha registrado todavía.
			// En ese caso, no hacemos nada: cuando se registre, ya quedará amarrado.

			return Task.CompletedTask;
		}



		private async Task<string> ValidarIdActualSignalR(string idSignalR)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(idSignalR))
					return "";

				// ✅ RAM primero
				if (_presence.TryResolveActiveByAnyConnectionId(idSignalR, out var activeConnId))
					return activeConnId;

				// ✅ SQL fallback
				var obj = await _sedesconectadasServicios.ConsultarPorIdSignalR(idSignalR);
				if (obj?.idCliente <= 0) return "";

				if (obj.activo == true && !string.IsNullOrWhiteSpace(obj.idActualSignalR))
					return obj.idActualSignalR!;

				var sedeId = obj.idSede ?? 0;
				if (sedeId <= 0) return "";

				var activos = await _sedesconectadasServicios.ConsultarPorSedeConEstadoActivo(sedeId);
				var actual = activos.OrderByDescending(x => x.fechaUltimoAcceso).FirstOrDefault();
				return actual?.idActualSignalR ?? "";
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al validar ID actual de SignalR: {ex.Message}");
				return "";
			}
		}

		private async Task<string?> ResolveWorkerConnIdBySedeAsync(long sedeId)
		{
			if (sedeId <= 0) return null;

			// 1) ✅ RAM primero (rápido)
			if (_presence.TryGetActiveConnectionBySede(sedeId, out var connIdRam) &&
				!string.IsNullOrWhiteSpace(connIdRam))
			{
				return connIdRam;
			}

			// 2) ✅ Fallback SQL (si el hub reinició o aún no hay latidos)
			// Usa el método nuevo que devuelve 1 fila (la más reciente)
			var row = await _sedesconectadasServicios.ConsultarActivoPorSede(sedeId);

			if (row?.idSedeConectada > 0 &&
				(row.activo ?? false) &&
				!string.IsNullOrWhiteSpace(row.idActualSignalR))
			{
				// ✅ rehidrata RAM para próximos llamados
				_presence.MarkSeen(sedeId, row.idActualSignalR!);
				return row.idActualSignalR;
			}

			return null;
		}

		public async Task<string> GetActiveConnectionIdByIdentificadorLocal(string identificadorLocal)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(identificadorLocal))
					return "";

				// ✅ 1) Primero RAM (rápido)
				if (_presence.TryGetActiveConnectionByIdentificadorLocal(identificadorLocal, out var connId))
					return connId;

				// ✅ 2) Fallback a SQL (solo si no está en RAM)
				var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
				if (sede?.idSede <= 0)
					return "";

				var activos = await _sedesconectadasServicios.ConsultarPorSedeConEstadoActivo(sede.idSede);
				var actual = activos.OrderByDescending(x => x.fechaUltimoAcceso).FirstOrDefault();

				// si existe en SQL, también lo cacheamos (por si el worker todavía no manda heartbeat)
				if (!string.IsNullOrWhiteSpace(actual?.idActualSignalR))
					_presence.Upsert(sede.idSede, identificadorLocal, actual.idActualSignalR);

				return actual?.idActualSignalR ?? "";
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error en GetActiveConnectionIdByIdentificadorLocal: {ex.Message}");
				return "";
			}
		}






		public async Task ErrorConexion(string clienteId, string errorConexion)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync("ErrorConexion", clienteId, errorConexion);
			}
			catch (Exception ex)
			{
				// Manejo de errores en la comunicación con el cliente
				Console.Error.WriteLine($"Error al enviar ErrorConexion: {ex.Message}");
			}
		}

		public async Task<bool> IsSedeActive(string identificadorLocal)
		{
			var activoId = await GetActiveConnectionIdByIdentificadorLocal(identificadorLocal);
			return !string.IsNullOrWhiteSpace(activoId);
		}

		// Método para registrar un dispositivo

		public async Task RegistrarEquipo(string idActualSignalR, string identificadorLocal)
		{
			try
			{
				// Mejor usar la conexión real de este caller (el worker)
				var workerConnId = Context.ConnectionId;

				var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
				if (sede?.idSede > 0)
				{
					// ✅ Actualiza "pizarra" en memoria
					_presence.Upsert(sede.idSede, identificadorLocal, workerConnId);

					// (opcional pero recomendado): agrupar por sede
					await Groups.AddToGroupAsync(workerConnId, $"SEDE:{sede.idSede}");

					// Desactivar conexiones previas
					await DesactivarConexionesPrevias(sede.idSede);

					// Registrar nuevo dispositivo
					await _sedesconectadasServicios.Agregar(new SedesConectadas
					{
						idCliente = sede.idCliente,
						idSede = sede.idSede,
						idActualSignalR = workerConnId,
						fechaUltimoAcceso = DateTime.Now,
						activo = true
					});

					Console.WriteLine($"Dispositivo registrado correctamente: {workerConnId}");
				}
				else
				{
					Console.WriteLine("Identificador local no válido o no se encontró la sede.");
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al registrar equipo: {ex.Message}");
			}
		}

		public async Task RegistrarEquipoV2(string identificadorLocal)
		{
			try
			{
				var workerConnId = Context.ConnectionId;

				var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
				if (sede?.idSede > 0)
				{
					_presence.Upsert(sede.idSede, identificadorLocal, workerConnId);

					await Groups.AddToGroupAsync(workerConnId, $"SEDE:{sede.idSede}");

					await DesactivarConexionesPrevias(sede.idSede);

					await _sedesconectadasServicios.Agregar(new SedesConectadas
					{
						idCliente = sede.idCliente,
						idSede = sede.idSede,
						idActualSignalR = workerConnId,
						fechaUltimoAcceso = DateTime.Now,
						activo = true
					});

					Console.WriteLine($"[RegistrarEquipo V2] OK: {workerConnId} ident={identificadorLocal}");
				}
				else
				{
					Console.WriteLine($"[RegistrarEquipo V2] Identificador no válido: {identificadorLocal}");
				}
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[RegistrarEquipo V2] Error: {ex.Message}");
				throw;
			}
		}

		// ✅ Nuevo: registra y devuelve la sede detectada (0 si falla)
		public async Task<long> RegistrarEquipoV3(string identificadorLocal)
		{
			try
			{
				var workerConnId = Context.ConnectionId;

				var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
				if (sede?.idSede <= 0) return 0;

				// RAM
				_presence.Upsert(sede.idSede, identificadorLocal, workerConnId);

				// Grupo por sede (para futuro enrutamiento pro)
				await Groups.AddToGroupAsync(workerConnId, $"SEDE:{sede.idSede}");

				// SQL
				await DesactivarConexionesPrevias(sede.idSede);

				await _sedesconectadasServicios.Agregar(new SedesConectadas
				{
					idCliente = sede.idCliente,
					idSede = sede.idSede,
					idActualSignalR = workerConnId,
					fechaUltimoAcceso = DateTime.Now,
					activo = true
				});

				return sede.idSede; // ✅ aquí lo devolvemos
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"[RegistrarEquipoV3] Error: {ex.Message}");
				return 0;
			}
		}

		private async Task DesactivarConexionesPrevias(long idSede)
		{
			var conexionesActivas = await _sedesconectadasServicios.ConsultarPorSedeConEstadoActivo(idSede);

			foreach (var conexion in conexionesActivas)
			{
				conexion.activo = false;
				await _sedesconectadasServicios.Editar(conexion.idSedeConectada, conexion);
			}
		}



		public async Task<List<SedesConectadas>> ObtenerActualizarSedesActivasPorCliente(long idCliente)
		{
			try
			{
				return await _sedesconectadasServicios.ConsultarSedesConectadasActivasPorCliente(idCliente);
			}
			catch (Exception ex)
			{
				// Manejo de errores en la obtención de sedes activas
				Console.Error.WriteLine($"Error al obtener sedes activas: {ex.Message}");
				// Retorna una lista vacía en caso de error
				return new List<SedesConectadas>();
			}
		}


	}
}
