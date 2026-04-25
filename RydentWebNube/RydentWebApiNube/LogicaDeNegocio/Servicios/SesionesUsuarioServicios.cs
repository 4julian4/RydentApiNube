using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Modelos;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
	public interface ISesionesUsuarioServicios
	{
		Task<SesionLoginResult> PrepararLoginAsync(
			Usuarios usuario,
			bool forzarCerrarAnterior,
			string? ip,
			string? userAgent);

		Task<bool> ValidarSesionActivaAsync(long idUsuario, string sessionId);

		Task ActualizarActividadAsync(long idUsuario, string sessionId);

		Task CerrarSesionAsync(long idUsuario, string sessionId, string motivo);
	}

	public class SesionesUsuarioServicios : ISesionesUsuarioServicios
	{
		private readonly IConfiguration _configuration;

		public SesionesUsuarioServicios(IConfiguration configuration)
		{
			_configuration = configuration;
		}

		private bool ExpirarPorInactividad =>
			_configuration.GetValue<bool>("SesionesUsuario:ExpirarPorInactividad");

		private int MinutosExpiracion =>
			_configuration.GetValue<int?>("SesionesUsuario:MinutosExpiracion") ?? 30;

		public async Task<SesionLoginResult> PrepararLoginAsync(
			Usuarios usuario,
			bool forzarCerrarAnterior,
			string? ip,
			string? userAgent)
		{
			using var db = new AppDbContext();

			var ahora = DateTime.Now;
			var idUsuario = Convert.ToInt64(usuario.idUsuario);

			long? idCliente = usuario.idCliente.HasValue
				? Convert.ToInt64(usuario.idCliente.Value)
				: null;

			var sesionActiva = await db.TSesionesUsuario
				.Where(x => x.idUsuario == idUsuario && x.activa)
				.OrderByDescending(x => x.fechaUltimaActividad)
				.FirstOrDefaultAsync();

			if (sesionActiva != null)
			{
				var vencida = ExpirarPorInactividad &&
					sesionActiva.fechaUltimaActividad.AddMinutes(MinutosExpiracion) < ahora;

				if (!vencida && !forzarCerrarAnterior)
				{
					return new SesionLoginResult
					{
						PuedeEntrar = false,
						RequiereConfirmacion = true,
						Mensaje = "Ya tienes una sesión activa. ¿Deseas cerrarla y continuar aquí?"
					};
				}

				sesionActiva.activa = false;
				sesionActiva.fechaCierre = ahora;
				sesionActiva.motivoCierre = vencida
					? "EXPIRADA_POR_INACTIVIDAD"
					: "REEMPLAZADA_POR_NUEVO_LOGIN";
			}

			var sessionId = Guid.NewGuid().ToString("N");

			db.TSesionesUsuario.Add(new SesionesUsuario
			{
				idUsuario = idUsuario,
				idCliente = idCliente,
				correoUsuario = usuario.correoUsuario ?? "",
				sessionId = sessionId,
				activa = true,
				fechaLogin = ahora,
				fechaUltimaActividad = ahora,
				ip = ip,
				userAgent = userAgent
			});

			await db.SaveChangesAsync();

			return new SesionLoginResult
			{
				PuedeEntrar = true,
				RequiereConfirmacion = false,
				SessionId = sessionId
			};
		}

		public async Task<bool> ValidarSesionActivaAsync(long idUsuario, string sessionId)
		{
			if (idUsuario <= 0 || string.IsNullOrWhiteSpace(sessionId))
				return false;

			using var db = new AppDbContext();

			var ahora = DateTime.Now;

			var sesion = await db.TSesionesUsuario
				.FirstOrDefaultAsync(x =>
					x.idUsuario == idUsuario &&
					x.sessionId == sessionId &&
					x.activa);

			if (sesion == null)
				return false;

			var vencida = ExpirarPorInactividad &&
				sesion.fechaUltimaActividad.AddMinutes(MinutosExpiracion) < ahora;

			if (vencida)
			{
				sesion.activa = false;
				sesion.fechaCierre = ahora;
				sesion.motivoCierre = "EXPIRADA_POR_INACTIVIDAD";

				await db.SaveChangesAsync();
				return false;
			}

			return true;
		}

		public async Task ActualizarActividadAsync(long idUsuario, string sessionId)
		{
			if (idUsuario <= 0 || string.IsNullOrWhiteSpace(sessionId))
				return;

			using var db = new AppDbContext();

			var sesion = await db.TSesionesUsuario
				.FirstOrDefaultAsync(x =>
					x.idUsuario == idUsuario &&
					x.sessionId == sessionId &&
					x.activa);

			if (sesion == null)
				return;

			sesion.fechaUltimaActividad = DateTime.Now;

			await db.SaveChangesAsync();
		}

		public async Task CerrarSesionAsync(long idUsuario, string sessionId, string motivo)
		{
			if (idUsuario <= 0 || string.IsNullOrWhiteSpace(sessionId))
				return;

			using var db = new AppDbContext();

			var sesion = await db.TSesionesUsuario
				.FirstOrDefaultAsync(x =>
					x.idUsuario == idUsuario &&
					x.sessionId == sessionId &&
					x.activa);

			if (sesion == null)
				return;

			sesion.activa = false;
			sesion.fechaCierre = DateTime.Now;
			sesion.motivoCierre = string.IsNullOrWhiteSpace(motivo)
				? "CIERRE_MANUAL"
				: motivo;

			await db.SaveChangesAsync();
		}
	}
}