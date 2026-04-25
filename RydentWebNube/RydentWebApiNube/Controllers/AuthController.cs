using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using RydentWebApiNube.Models.Google;
using RydentWebApiNube.Models.MSN;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

namespace RydentWebApiNube.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class AuthController : ControllerBase
	{
		private readonly IConfiguration _configuration;
		private readonly IUsuariosServicios _usuarios;
		private readonly IClientesServicios _clientes;
		private readonly ISesionesUsuarioServicios _sesionesUsuarioServicios;

		// ✅ Puedes dejar estático si NO tocas DefaultRequestHeaders
		private static readonly HttpClient httpClient = new HttpClient();

		// MSN / Azure OAuth
		private readonly string AuthCodeEndPoint;
		private readonly string TokenEndPoint;
		private readonly string ClientId;
		private readonly string Secret;
		private readonly string Scope;
		private readonly string RedirectURI;
		private readonly string API_EndPoint;

		// Google OAuth
		private readonly string GoogleTokenEndPoint;
		private readonly string GoogleClientId;
		private readonly string GoogleSecret;
		private readonly string GoogleRedirectURI;
		private readonly string GoogleAPI_EndPoint;

		public AuthController(
			IConfiguration configuration,
			IUsuariosServicios iUsuariosServicios,
			IClientesServicios clientesServicios,
			ISesionesUsuarioServicios sesionesUsuarioServicios)
		{
			_configuration = configuration;
			_usuarios = iUsuariosServicios;
			_clientes = clientesServicios;

			// Azure / MSN
			AuthCodeEndPoint = configuration["OAuth:AuthCodeEndPoint"] ?? "";
			TokenEndPoint = configuration["OAuth:TokenEndPoint"] ?? "";
			ClientId = configuration["OAUTH2_AZURE_CLIENTID"] ?? "";
			Secret = configuration["OAUTH2_AZURE_SECRET"] ?? "";
			Scope = configuration["OAuth:Scope"] ?? "";
			RedirectURI = configuration["OAuth:RedirectURI"] ?? "";
			API_EndPoint = configuration["OAuth:API_EndPoint"] ?? "";

			// Google
			GoogleTokenEndPoint = configuration["OAuthGoogle:TokenEndPoint"] ?? "";
			GoogleClientId = configuration["OAUTH2_GOOGLE_CLIENTID"] ?? "";
			GoogleSecret = configuration["OAUTH2_GOOGLE_SECRET"] ?? "";
			GoogleRedirectURI = configuration["OAuthGoogle:RedirectURI"] ?? "";
			GoogleAPI_EndPoint = configuration["OAuthGoogle:API_EndPoint"] ?? "";
			_sesionesUsuarioServicios = sesionesUsuarioServicios;
		}

		// ✅ DTOs
		public record LoginRequest(string code, string state, bool forzarCerrarAnterior = false);

		public record LoginResponse(
			bool autenticado,
			string respuesta,
			string mensaje,
			bool mostrarRecordatorio,
			DateTime? activoHasta,
			int? diasParaVencer,
			bool requiereConfirmacion = false,
			string? loginConfirmToken = null
		);

		private record ValidacionAccesoResult(
			bool PermitirAcceso,
			string Mensaje,
			bool MostrarRecordatorio,
			DateTime? ActivoHasta,
			int? DiasParaVencer
		);

		// ✅ Redirección a Azure/MSN
		[HttpGet("getcode")]
		public IActionResult GetCode()
		{
			string url =
				$"{AuthCodeEndPoint}?" +
				$"response_type=code&" +
				$"client_id={ClientId}&" +
				$"redirect_uri={Uri.EscapeDataString(RedirectURI)}&" +
				$"scope={Uri.EscapeDataString(Scope)}&" +
				$"state=1234567890";

			return Redirect(url);
		}

		// ✅ POST api/auth  (Azure/MSN)
		[HttpPost("")]
		public async Task<IActionResult> Autenticar([FromBody] LoginRequest modelo)
		{
			var resp = new LoginResponse(false, "", "", false, null, null);

			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.code))
					return Ok(resp);

				var tokenBody = new Dictionary<string, string>
				{
					{ "grant_type", "authorization_code" },
					{ "code", modelo.code },
					{ "redirect_uri", RedirectURI },
					{ "client_id", ClientId },
					{ "client_secret", Secret },
					{ "scope", Scope }
				};

				var tokenHttpResp = await httpClient.PostAsync(
					TokenEndPoint,
					new FormUrlEncodedContent(tokenBody)
				).ConfigureAwait(false);

				if (!tokenHttpResp.IsSuccessStatusCode)
					return Ok(resp);

				var tokenJson = await tokenHttpResp.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
				if (!tokenJson.TryGetProperty("access_token", out var atProp))
					return Ok(resp);

				var accessToken = atProp.GetString();
				if (string.IsNullOrWhiteSpace(accessToken))
					return Ok(resp);

				var meReq = new HttpRequestMessage(HttpMethod.Get, API_EndPoint);
				meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var meHttpResp = await httpClient.SendAsync(meReq).ConfigureAwait(false);
				if (!meHttpResp.IsSuccessStatusCode)
					return Ok(resp);

				var meRaw = await meHttpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
				var me = JsonSerializer.Deserialize<UsuarioMSN>(meRaw);

				if (string.IsNullOrWhiteSpace(me?.id))
					return Ok(resp);

				var usuario = await _usuarios.ConsultarPorCodigoExterno(me.id).ConfigureAwait(false);
				if (usuario == null || usuario.idUsuario <= 0)
				{
					return Ok(new LoginResponse(
						false,
						"",
						"No encontramos un usuario registrado para esta cuenta.",
						false,
						null,
						null
					));
				}

				var validacion = await ValidarAccesoClienteAsync(usuario).ConfigureAwait(false);
				if (!validacion.PermitirAcceso)
				{
					return Ok(new LoginResponse(
						false,
						"",
						validacion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer
					));
				}

				var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
				var userAgent = Request.Headers["User-Agent"].ToString();

				var sesion = await _sesionesUsuarioServicios.PrepararLoginAsync(
					usuario,
					modelo.forzarCerrarAnterior,
					ip,
					userAgent
				);

				if (sesion.RequiereConfirmacion)
				{
					var confirmToken = GenerateLoginConfirmToken(usuario);

					return Ok(new LoginResponse(
						false,
						"",
						sesion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer,
						true,
						confirmToken
					));
				}

				if (!sesion.PuedeEntrar)
				{
					return Ok(new LoginResponse(
						false,
						"",
						sesion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer,
						false
					));
				}

				var jwt = GenerateJwtToken(usuario, sesion.SessionId);

				return Ok(new LoginResponse(
					true,
					jwt,
					validacion.Mensaje,
					validacion.MostrarRecordatorio,
					validacion.ActivoHasta,
					validacion.DiasParaVencer
				));
			}
			catch (Exception ex)
			{
				Console.WriteLine("AUTH MSN/AZURE EXCEPTION: " + ex);
				return Ok(resp);
			}
		}

		[HttpPost("forzar-login")]
		public async Task<IActionResult> ForzarLogin([FromBody] ForzarLoginRequest modelo)
		{
			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.loginConfirmToken))
				{
					return Ok(new LoginResponse(false, "", "Confirmación inválida.", false, null, null));
				}

				var datos = ObtenerDatosDesdeLoginConfirmToken(modelo.loginConfirmToken);

				if (datos == null)
				{
					return Ok(new LoginResponse(false, "", "La confirmación expiró. Intenta iniciar sesión nuevamente.", false, null, null));
				}

				var usuario = new Usuarios
				{
					idUsuario = datos.Value.IdUsuario,
					idCliente = datos.Value.IdCliente,
					correoUsuario = datos.Value.Correo
				};

				var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
				var userAgent = Request.Headers["User-Agent"].ToString();

				var sesion = await _sesionesUsuarioServicios.PrepararLoginAsync(
					usuario,
					true,
					ip,
					userAgent
				);

				if (!sesion.PuedeEntrar)
				{
					return Ok(new LoginResponse(false, "", sesion.Mensaje, false, null, null));
				}

				var jwt = GenerateJwtToken(usuario, sesion.SessionId);

				return Ok(new LoginResponse(true, jwt, "", false, null, null));
			}
			catch (Exception ex)
			{
				Console.WriteLine("FORZAR LOGIN EXCEPTION: " + ex);
				return Ok(new LoginResponse(false, "", "No fue posible continuar la sesión.", false, null, null));
			}
		}

		public record ForzarLoginRequest(string loginConfirmToken);

		// ✅ POST api/auth/authgoogle (Google)
		[HttpPost("authgoogle")]
		public async Task<IActionResult> AutenticarGoogle([FromBody] LoginRequest modelo)
		{
			var resp = new LoginResponse(false, "", "", false, null, null);

			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.code))
					return Ok(resp);

				var tokenBody = new Dictionary<string, string>
				{
					{ "code", modelo.code },
					{ "client_id", GoogleClientId },
					{ "client_secret", GoogleSecret },
					{ "redirect_uri", GoogleRedirectURI },
					{ "grant_type", "authorization_code" }
				};

				var tokenHttpResp = await httpClient.PostAsync(
					GoogleTokenEndPoint,
					new FormUrlEncodedContent(tokenBody)
				).ConfigureAwait(false);

				var tokenRaw = await tokenHttpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!tokenHttpResp.IsSuccessStatusCode)
				{
					Console.WriteLine("GOOGLE TOKEN ERROR: " + tokenRaw);
					return Ok(resp);
				}

				var tokenJson = JsonSerializer.Deserialize<JsonElement>(tokenRaw);
				if (!tokenJson.TryGetProperty("access_token", out var atProp))
					return Ok(resp);

				var accessToken = atProp.GetString();
				if (string.IsNullOrWhiteSpace(accessToken))
					return Ok(resp);

				var meReq = new HttpRequestMessage(HttpMethod.Get, GoogleAPI_EndPoint);
				meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var meHttpResp = await httpClient.SendAsync(meReq).ConfigureAwait(false);
				var meRaw = await meHttpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
				if (!meHttpResp.IsSuccessStatusCode)
				{
					Console.WriteLine("GOOGLE USERINFO ERROR: " + meRaw);
					return Ok(resp);
				}

				var me = JsonSerializer.Deserialize<UsuarioGoogle>(meRaw);
				if (string.IsNullOrWhiteSpace(me?.email))
					return Ok(resp);

				var usuario = await _usuarios.ConsultarPorCorreo(me.email).ConfigureAwait(false);
				if (usuario == null || usuario.idUsuario <= 0)
				{
					return Ok(new LoginResponse(
						false,
						"",
						"No encontramos un usuario registrado con este correo.",
						false,
						null,
						null
					));
				}

				var validacion = await ValidarAccesoClienteAsync(usuario).ConfigureAwait(false);
				if (!validacion.PermitirAcceso)
				{
					return Ok(new LoginResponse(
						false,
						"",
						validacion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer
					));
				}

				var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
				var userAgent = Request.Headers["User-Agent"].ToString();

				var sesion = await _sesionesUsuarioServicios.PrepararLoginAsync(
					usuario,
					modelo.forzarCerrarAnterior,
					ip,
					userAgent
				);

				if (sesion.RequiereConfirmacion)
				{
					var confirmToken = GenerateLoginConfirmToken(usuario);

					return Ok(new LoginResponse(
						false,
						"",
						sesion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer,
						true,
						confirmToken
					));
				}

				if (!sesion.PuedeEntrar)
				{
					return Ok(new LoginResponse(
						false,
						"",
						sesion.Mensaje,
						false,
						validacion.ActivoHasta,
						validacion.DiasParaVencer,
						false
					));
				}

				var jwt = GenerateJwtToken(usuario, sesion.SessionId);

				return Ok(new LoginResponse(
					true,
					jwt,
					validacion.Mensaje,
					validacion.MostrarRecordatorio,
					validacion.ActivoHasta,
					validacion.DiasParaVencer
				));
			}
			catch (Exception ex)
			{
				Console.WriteLine("AUTH GOOGLE EXCEPTION: " + ex);
				return Ok(resp);
			}
		}

		private async Task<ValidacionAccesoResult> ValidarAccesoClienteAsync(Usuarios usuario)
		{
			if (usuario == null || usuario.idUsuario <= 0)
			{
				return new ValidacionAccesoResult(
					false,
					"No fue posible validar el usuario.",
					false,
					null,
					null
				);
			}

			// ✅ Usuario activo
			if (usuario.estado != true)
			{
				return new ValidacionAccesoResult(
					false,
					"Tu usuario se encuentra inactivo. Por favor comunícate con soporte o con el administrador de tu cuenta.",
					false,
					null,
					null
				);
			}

			// ✅ Usuario con cliente asociado
			if (!usuario.idCliente.HasValue || usuario.idCliente.Value <= 0)
			{
				return new ValidacionAccesoResult(
					false,
					"Tu usuario no tiene un cliente asociado. Por favor comunícate con soporte.",
					false,
					null,
					null
				);
			}

			var cliente = await _clientes.ConsultarPorId(usuario.idCliente.Value).ConfigureAwait(false);

			if (cliente == null || cliente.idCliente <= 0)
			{
				return new ValidacionAccesoResult(
					false,
					"No fue posible encontrar la información del cliente asociado a tu cuenta.",
					false,
					null,
					null
				);
			}

			// ✅ Cliente activo
			if (!cliente.estado)
			{
				return new ValidacionAccesoResult(
					false,
					"La cuenta de tu empresa se encuentra inactiva. Por favor comunícate con nuestro equipo para ayudarte.",
					false,
					cliente.activoHasta,
					null
				);
			}

			// ✅ Debe tener habilitado Rydent Web
			if (!cliente.usaRydentWeb)
			{
				return new ValidacionAccesoResult(
					false,
					"Tu cuenta no tiene acceso habilitado a Rydent Web. Si deseas activarlo, con gusto te ayudamos.",
					false,
					cliente.activoHasta,
					null
				);
			}

			// ✅ Si no tiene activoHasta configurado, por ahora se permite acceso
			if (!cliente.activoHasta.HasValue)
			{
				return new ValidacionAccesoResult(
					true,
					"",
					false,
					null,
					null
				);
			}

			var hoy = DateTime.Today;
			var activoHasta = cliente.activoHasta.Value.Date;

			// ✅ Bloquear si ya venció
			if (hoy > activoHasta)
			{
				return new ValidacionAccesoResult(
					false,
					$"Tu acceso se encuentra temporalmente suspendido porque tu servicio estuvo activo hasta el {activoHasta:dd/MM/yyyy}. Por favor comunícate con nosotros y con gusto te ayudamos a reactivarlo.",
					false,
					activoHasta,
					(activoHasta - hoy).Days
				);
			}

			var diasParaVencer = (activoHasta - hoy).Days;

			// ✅ Aviso cordial desde 8 días antes
			if (diasParaVencer <= 8)
			{
				var mensaje = diasParaVencer switch
				{
					0 => $"Hola, tu servicio está activo hasta hoy ({activoHasta:dd/MM/yyyy}). Gracias por tu confianza en Rydent.",
					1 => $"Hola, tu servicio está activo hasta mañana ({activoHasta:dd/MM/yyyy}). Gracias por tu confianza en Rydent.",
					_ => $"Hola, tu servicio está activo hasta el {activoHasta:dd/MM/yyyy}, es decir, por {diasParaVencer} día(s) más. Gracias por tu confianza en Rydent."
				};

				return new ValidacionAccesoResult(
					true,
					mensaje,
					true,
					activoHasta,
					diasParaVencer
				);
			}

			return new ValidacionAccesoResult(
				true,
				"",
				false,
				activoHasta,
				diasParaVencer
			);
		}

		private string GenerateJwtToken(Usuarios user, string sessionId)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? "");

			var claims = new List<Claim>
			{
				new Claim("id", user.idUsuario.ToString()),
				new Claim("idCliente", user.idCliente?.ToString() ?? ""),
				new Claim("correo", user.correoUsuario?.ToString() ?? ""),
				new Claim("sessionId", sessionId)
			};

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddDays(7),
				Issuer = _configuration["Jwt:Issuer"] ?? "",
				Audience = _configuration["JWT_SECRET"] ?? "",
				SigningCredentials = new SigningCredentials(
					new SymmetricSecurityKey(key),
					SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private string GenerateLoginConfirmToken(Usuarios user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? "");

			var claims = new List<Claim>
			{
				new Claim("purpose", "force_login"),
				new Claim("id", user.idUsuario.ToString()),
				new Claim("idCliente", user.idCliente?.ToString() ?? ""),
				new Claim("correo", user.correoUsuario?.ToString() ?? "")
			};

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddMinutes(3),
				Issuer = _configuration["Jwt:Issuer"] ?? "",
				Audience = _configuration["JWT_SECRET"] ?? "",
				SigningCredentials = new SigningCredentials(
					new SymmetricSecurityKey(key),
					SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}

		private (int IdUsuario, int? IdCliente, string Correo)? ObtenerDatosDesdeLoginConfirmToken(string token)
		{
			try
			{
				var jwtSecret = _configuration["JWT_SECRET"] ?? "";
				var issuer = _configuration["Jwt:Issuer"] ?? "";

				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(jwtSecret);

				var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),

					ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
					ValidIssuer = issuer,

					ValidateAudience = true,
					ValidAudience = jwtSecret,

					ValidateLifetime = true,
					ClockSkew = TimeSpan.FromSeconds(15)
				}, out _);

				var purpose = principal.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value;
				if (purpose != "force_login") return null;

				var idStr = principal.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
				var idClienteStr = principal.Claims.FirstOrDefault(x => x.Type == "idCliente")?.Value;
				var correo = principal.Claims.FirstOrDefault(x => x.Type == "correo")?.Value ?? "";

				if (!int.TryParse(idStr, out var idUsuario)) return null;

				int? idCliente = null;
				if (int.TryParse(idClienteStr, out var idClienteParsed))
					idCliente = idClienteParsed;

				return (idUsuario, idCliente, correo);
			}
			catch
			{
				return null;
			}
		}
	}
}