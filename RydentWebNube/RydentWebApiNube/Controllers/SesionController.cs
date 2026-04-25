using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RydentWebApiNube.Controllers
{
	[ApiController]
	[Route("api/sesion")]
	public class SesionController : ControllerBase
	{
		private readonly ISesionesUsuarioServicios _sesionesUsuarioServicios;
		private readonly IConfiguration _configuration;

		public SesionController(
			ISesionesUsuarioServicios sesionesUsuarioServicios,
			IConfiguration configuration)
		{
			_sesionesUsuarioServicios = sesionesUsuarioServicios;
			_configuration = configuration;
		}

		[HttpPost("actividad")]
		public async Task<IActionResult> Actividad()
		{
			var datos = ObtenerDatosSesionDesdeToken();

			if (datos == null)
			{
				return Ok(new
				{
					activa = false,
					mensaje = "Sesión inválida."
				});
			}

			var activa = await _sesionesUsuarioServicios.ValidarSesionActivaAsync(
				datos.Value.IdUsuario,
				datos.Value.SessionId
			);

			if (!activa)
			{
				return Ok(new
				{
					activa = false,
					mensaje = "Tu sesión fue cerrada porque se inició con tu usuario en otro equipo."
				});
			}

			await _sesionesUsuarioServicios.ActualizarActividadAsync(
				datos.Value.IdUsuario,
				datos.Value.SessionId
			);

			return Ok(new
			{
				activa = true,
				mensaje = ""
			});
		}

		[HttpPost("cerrar")]
		public async Task<IActionResult> Cerrar()
		{
			var datos = ObtenerDatosSesionDesdeToken();

			if (datos != null)
			{
				await _sesionesUsuarioServicios.CerrarSesionAsync(
					datos.Value.IdUsuario,
					datos.Value.SessionId,
					"CIERRE_MANUAL"
				);
			}

			return Ok(new
			{
				ok = true
			});
		}

		private (long IdUsuario, string SessionId)? ObtenerDatosSesionDesdeToken()
		{
			try
			{
				var auth = Request.Headers["Authorization"].ToString();

				if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer "))
					return null;

				var token = auth.Substring("Bearer ".Length).Trim();

				var jwtSecret = _configuration["JWT_SECRET"] ?? "";
				var issuer = _configuration["Jwt:Issuer"] ?? "";

				if (string.IsNullOrWhiteSpace(jwtSecret))
					return null;

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
					ClockSkew = TimeSpan.FromMinutes(1)
				}, out _);

				var idUsuarioStr =
					principal.Claims.FirstOrDefault(x => x.Type == "id")?.Value;

				var sessionId =
					principal.Claims.FirstOrDefault(x => x.Type == "sessionId")?.Value;

				if (!long.TryParse(idUsuarioStr, out var idUsuario))
					return null;

				if (string.IsNullOrWhiteSpace(sessionId))
					return null;

				return (idUsuario, sessionId);
			}
			catch
			{
				return null;
			}
		}
	}
}