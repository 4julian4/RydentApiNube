using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Services;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using RydentWebApiNube.Models.Google;
using RydentWebApiNube.Models.MSN;
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
    public class RydentHub : Hub
    {
        private readonly ISedesServicios _sedesServicios;
		private readonly ISesionesUsuarioServicios _sesionesUsuarioServicios;
		//private readonly IConfiguration configuration;
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




		public RydentHub(
            ISedesServicios sedesServicios,
            ISedesConectadasServicios sedesconectadasServicios,
			ISesionesUsuarioServicios sesionesUsuarioServicios,
			IConfiguration configuration,
            IUsuariosServicios iUsuariosServicios,
			WorkerPresenceRegistry presence
			)
        {
            _sedesServicios = sedesServicios;
            _sedesconectadasServicios = sedesconectadasServicios;
			_sesionesUsuarioServicios = sesionesUsuarioServicios;
			this.iUsuariosServicios = iUsuariosServicios;
			_presence = presence;
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

			await Clients.Client(clienteId).SendAsync(
				"RespuestaPostLoginCallback",
				clienteId,
				jsonResult
			);
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


        // Método que verifica si el dispositivo ya está registrado
        /*public async Task<bool> IsDeviceRegistered(string idActualSignalR)
        {
            try
            {
                // Consultar si ya existe un dispositivo con este SignalR ID activo
                var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(idActualSignalR);
                return objSedesConectada != null && (objSedesConectada.activo ?? false);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la consulta
                Console.Error.WriteLine($"Error al verificar si el dispositivo está registrado: {ex.Message}");
                return false; // Si ocurre un error, considera que no está registrado
            }
        }*/
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



		/*public async Task ObtenerPin(string clienteId, string pin, int maxIdAnamnesis)
		{
			// clienteId AQUI realmente es el "TARGET" (idActualSignalR / sede a la que quiero llegar)
			var targetId = clienteId;

			try
			{
				// ✅ El "return address" del browser SIEMPRE es Context.ConnectionId
				var returnId = Context.ConnectionId;

				// ✅ Resolver worker activo por target (RAM/SQL)
				string workerConnId = await ValidarIdActualSignalR(targetId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
					// ✅ Enviamos al worker: (returnId, pin, maxIdAnamnesis)
					await Clients.Client(workerConnId)
						.SendAsync("ObtenerPin", returnId, pin, maxIdAnamnesis);
				}
				else
				{
					// ✅ Error hacia el browser, etiquetado con returnId (NO con targetId)
					await Clients.Client(returnId)
						.SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");
				}
			}
			catch (Exception ex)
			{
				// ✅ Error hacia el browser, etiquetado con returnId
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

				Console.Error.WriteLine($"Error al ObtenerPin: {ex.Message}");
			}
		}*/

		public async Task ObtenerPin(long sedeId, string pin, int maxIdAnamnesis)
		{
			try
			{
				var returnId = Context.ConnectionId;

				// 1) resolver connectionId del worker por sedeId (RAM/SQL)
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
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);
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


		//----------------ConsultarPorDiaYPorUnidad en Base de Datos Rydent Local----------------
		//ClienteId: Identificador del cliente local que tiene el worked por medio del cual se realizara la consulta en la bd rydent local
		//Este dato de clienteId queda guardado en sedes conectadas

		//Cuando se invoca es que se ejecuta estas funciones del servidor SR

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


		public async Task GuardarDatosRips(long sedeId, string datosRips)
		{
			try
			{
				var returnId = Context.ConnectionId;
				var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

				if (!string.IsNullOrWhiteSpace(workerConnId))
				{
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

		// =========================================================


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
		//-----------------------------------Interoperabilidad------------------------------------//

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

		//------------------------------------------------------------------------------------------------------------//

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
