using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using RydentWebApiNube.Models.Google;
using RydentWebApiNube.Models.MSN;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
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

		// ✅ Puedes dejar estático si NO tocas DefaultRequestHeaders (como lo hacemos aquí)
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

		public AuthController(IConfiguration configuration, IUsuariosServicios iUsuariosServicios)
		{
			_configuration = configuration;
			_usuarios = iUsuariosServicios;

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
		}

		// ✅ DTOs claros (mejor que Expando)
		public record LoginRequest(string code, string state);
		public record LoginResponse(bool autenticado, string respuesta);

		// ✅ Redirección a Azure/MSN (si la sigues usando)
		[HttpGet("getcode")]
		public IActionResult GetCode()
		{
			// OAuth estándar: redirect_uri en minúscula
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
			var resp = new LoginResponse(false, "");

			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.code))
					return Ok(resp);

				// 1) Cambiar code por access_token
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

				// 2) Pedir datos del usuario (SIN tocar DefaultRequestHeaders)
				var meReq = new HttpRequestMessage(HttpMethod.Get, API_EndPoint);
				meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var meHttpResp = await httpClient.SendAsync(meReq).ConfigureAwait(false);
				if (!meHttpResp.IsSuccessStatusCode)
					return Ok(resp);

				var meRaw = await meHttpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
				var me = JsonSerializer.Deserialize<UsuarioMSN>(meRaw);

				// Mantengo tu lógica: buscar por id externo
				if (string.IsNullOrWhiteSpace(me?.id))
					return Ok(resp);

				var usuario = await _usuarios.ConsultarPorCodigoExterno(me.id).ConfigureAwait(false);
				if (usuario == null || usuario.idUsuario <= 0)
					return Ok(resp);

				var jwt = GenerateJwtToken(usuario);
				return Ok(new LoginResponse(true, jwt));
			}
			catch
			{
				return Ok(resp);
			}
		}

		// ✅ POST api/auth/authgoogle (Google)
		/*[HttpPost("authgoogle")]
		public async Task<IActionResult> AutenticarGoogle([FromBody] LoginRequest modelo)
		{
			var resp = new LoginResponse(false, "");

			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.code))
					return Ok(resp);

				string redirectUri = new Uri(GoogleRedirectURI).ToString();

				var tokenBody = new Dictionary<string, string>
				{
					{ "code", modelo.code },
					{ "client_id", GoogleClientId },
					{ "client_secret", GoogleSecret },
					{ "redirect_uri", redirectUri },
					{ "grant_type", "authorization_code" }
				};

				var tokenHttpResp = await httpClient.PostAsync(
					GoogleTokenEndPoint,
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

				// Google userinfo: algunos endpoints son tipo "...?access_token="
				// Si tu GoogleAPI_EndPoint ya trae "?access_token=", esto funciona.
				var userInfoUrl = GoogleAPI_EndPoint + accessToken;

				var meReq = new HttpRequestMessage(HttpMethod.Get, userInfoUrl);
				meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

				var meHttpResp = await httpClient.SendAsync(meReq).ConfigureAwait(false);
				if (!meHttpResp.IsSuccessStatusCode)
					return Ok(resp);

				var meRaw = await meHttpResp.Content.ReadAsStringAsync().ConfigureAwait(false);
				var me = JsonSerializer.Deserialize<UsuarioGoogle>(meRaw);

				if (string.IsNullOrWhiteSpace(me?.email))
					return Ok(resp);

				var usuario = await _usuarios.ConsultarPorCorreo(me.email).ConfigureAwait(false);
				if (usuario == null || usuario.idUsuario <= 0)
					return Ok(resp);

				var jwt = GenerateJwtToken(usuario);
				return Ok(new LoginResponse(true, jwt));
			}
			catch
			{
				return Ok(resp);
			}
		}*/

		[HttpPost("authgoogle")]
		public async Task<IActionResult> AutenticarGoogle([FromBody] LoginRequest modelo)
		{
			var resp = new LoginResponse(false, "");

			try
			{
				if (modelo == null || string.IsNullOrWhiteSpace(modelo.code))
					return Ok(resp);

				var tokenBody = new Dictionary<string, string>
				{
				  { "code", modelo.code },
				  { "client_id", GoogleClientId },
				  { "client_secret", GoogleSecret },
				  { "redirect_uri", GoogleRedirectURI }, // ✅ TAL CUAL
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

				// ✅ Userinfo sin concatenar access_token
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
					return Ok(resp);

				var jwt = GenerateJwtToken(usuario);
				return Ok(new LoginResponse(true, jwt));
			}
			catch (Exception ex)
			{
				Console.WriteLine("AUTH GOOGLE EXCEPTION: " + ex);
				return Ok(resp);
			}
		}

		private string GenerateJwtToken(Usuarios user)
		{
			var tokenHandler = new JwtSecurityTokenHandler();
			var key = Encoding.ASCII.GetBytes(_configuration["JWT_SECRET"] ?? "");

			var claims = new List<Claim>
			{
				new Claim("id", user.idUsuario.ToString()),
				new Claim("idCliente", user.idCliente.ToString()),
				new Claim("correo", user.correoUsuario?.ToString() ?? "")
			};

			var tokenDescriptor = new SecurityTokenDescriptor
			{
				Subject = new ClaimsIdentity(claims),
				Expires = DateTime.UtcNow.AddDays(7),
				Issuer = _configuration["Jwt:Issuer"] ?? "",
				Audience = _configuration["JWT_SECRET"] ?? "",
				SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
			};

			var token = tokenHandler.CreateToken(tokenDescriptor);
			return tokenHandler.WriteToken(token);
		}
	}
}
