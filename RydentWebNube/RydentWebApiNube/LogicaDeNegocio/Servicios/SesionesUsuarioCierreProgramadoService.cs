using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;

namespace RydentWebApiNube.LogicaDeNegocio.Services
{
	public class SesionesUsuarioCierreProgramadoService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly IConfiguration _configuration;
		private DateTime? _ultimoDiaEjecutado;

		public SesionesUsuarioCierreProgramadoService(
			IServiceProvider serviceProvider,
			IConfiguration configuration)
		{
			_serviceProvider = serviceProvider;
			_configuration = configuration;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					await RevisarYCerrarSesionesAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					Console.WriteLine("[SesionesUsuarioCierreProgramadoService] Error: " + ex.Message);
				}

				var intervaloMinutos =
					_configuration.GetValue<int?>("SesionesUsuario:IntervaloRevisionMinutos") ?? 90;

				if (intervaloMinutos <= 0)
					intervaloMinutos = 90;

				await Task.Delay(TimeSpan.FromMinutes(intervaloMinutos), stoppingToken);
			}
		}

		private async Task RevisarYCerrarSesionesAsync(CancellationToken ct)
		{
			var habilitado =
				_configuration.GetValue<bool>("SesionesUsuario:CerrarSesionesProgramado");

			if (!habilitado)
				return;

			var ahora = DateTime.Now;
			var hoy = ahora.Date;

			if (_ultimoDiaEjecutado.HasValue && _ultimoDiaEjecutado.Value.Date == hoy)
				return;

			var horaInicioTexto =
				_configuration["SesionesUsuario:HoraCierreInicio"] ?? "02:00";

			var horaFinTexto =
				_configuration["SesionesUsuario:HoraCierreFin"] ?? "04:00";

			if (!TimeSpan.TryParse(horaInicioTexto, out var horaInicio))
				horaInicio = new TimeSpan(2, 0, 0);

			if (!TimeSpan.TryParse(horaFinTexto, out var horaFin))
				horaFin = new TimeSpan(4, 0, 0);

			var horaActual = ahora.TimeOfDay;

			if (horaActual < horaInicio || horaActual > horaFin)
				return;

			using var scope = _serviceProvider.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			var sesionesActivas = await db.TSesionesUsuario
				.Where(x => x.activa)
				.ToListAsync(ct);

			if (sesionesActivas.Count == 0)
			{
				_ultimoDiaEjecutado = hoy;
				return;
			}

			foreach (var sesion in sesionesActivas)
			{
				sesion.activa = false;
				sesion.fechaCierre = ahora;
				sesion.motivoCierre = "CIERRE_PROGRAMADO_2AM";
			}

			await db.SaveChangesAsync(ct);

			_ultimoDiaEjecutado = hoy;

			Console.WriteLine($"[SesionesUsuarioCierreProgramadoService] Sesiones cerradas: {sesionesActivas.Count}");
		}
	}
}