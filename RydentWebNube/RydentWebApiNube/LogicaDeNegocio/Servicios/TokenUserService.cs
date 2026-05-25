using Microsoft.IdentityModel.Tokens;
using RydentWebApiNube.LogicaDeNegocio.Modelos;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
	public class TokenUserService : ITokenUserService
	{
		private readonly IConfiguration _configuration;

		public TokenUserService(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		public TokenUserData? ObtenerDatosUsuario(HttpContext httpContext)
		{
			try
			{
				var auth = httpContext.Request.Headers["Authorization"].ToString();

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

				var idUsuarioStr = principal.Claims
					.FirstOrDefault(x => x.Type == "id")?.Value;

				var idClienteStr = principal.Claims
					.FirstOrDefault(x => x.Type == "idCliente")?.Value;

				var sessionId = principal.Claims
					.FirstOrDefault(x => x.Type == "sessionId")?.Value;

				var correo = principal.Claims
					.FirstOrDefault(x => x.Type == "correo")?.Value;

				var puedeUsarRydentWebStr = principal.Claims
					.FirstOrDefault(x => x.Type == "puedeUsarRydentWeb")?.Value;

				var puedeUsarFacturacionStr = principal.Claims
					.FirstOrDefault(x => x.Type == "puedeUsarFacturacion")?.Value;

				if (!long.TryParse(idUsuarioStr, out var idUsuario))
					return null;

				if (!long.TryParse(idClienteStr, out var idCliente))
					return null;

				if (string.IsNullOrWhiteSpace(sessionId))
					return null;

				return new TokenUserData
				{
					IdUsuario = idUsuario,
					IdCliente = idCliente,
					SessionId = sessionId,
					Correo = correo ?? "",
					PuedeUsarRydentWeb = string.Equals(puedeUsarRydentWebStr, "true", StringComparison.OrdinalIgnoreCase),
					PuedeUsarFacturacion = string.Equals(puedeUsarFacturacionStr, "true", StringComparison.OrdinalIgnoreCase)
				};
			}
			catch
			{
				return null;
			}
		}
	}

	public interface ITokenUserService
	{
		TokenUserData? ObtenerDatosUsuario(HttpContext httpContext);
	}
}