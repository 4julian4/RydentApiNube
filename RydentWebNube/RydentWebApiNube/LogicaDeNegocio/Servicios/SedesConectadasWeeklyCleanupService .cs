using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;

namespace RydentWebApiNube.LogicaDeNegocio.Services
{
	public class SedesConectadasWeeklyCleanupService : BackgroundService
	{
		private readonly IServiceScopeFactory _scopeFactory;
		private readonly ILogger<SedesConectadasWeeklyCleanupService> _logger;

		private const int DaysToKeep = 8;
		private const int BatchSize = 5000;

		// Ventana domingo Colombia 00:00 - 04:00
		private static readonly TimeSpan WindowStart = TimeSpan.FromHours(0);
		private static readonly TimeSpan WindowEnd = TimeSpan.FromHours(4);

		// ✅ Colombia TZ (compat: Windows + Linux)
		private static TimeZoneInfo GetColombiaTz()
		{
			try { return TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"); }          // Linux
			catch { return TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time"); } // Windows
		}

		private static readonly TimeZoneInfo ColombiaTz = GetColombiaTz();

		public SedesConectadasWeeklyCleanupService(
			IServiceScopeFactory scopeFactory,
			ILogger<SedesConectadasWeeklyCleanupService> logger)
		{
			_scopeFactory = scopeFactory;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			// delay pequeño al arranque
			await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					var delay = GetDelayUntilNextSundayColombiaRandomWindow(WindowStart, WindowEnd);
					_logger.LogWarning("Cleanup TSedesConectadas: programado en {Delay}", delay);

					await Task.Delay(delay, stoppingToken);

					await RunCleanupBatched(stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error en cleanup semanal TSedesConectadas");
					await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
				}
			}
		}

		private async Task RunCleanupBatched(CancellationToken ct)
		{
			using var scope = _scopeFactory.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

			// ⚠️ Como tu BD guarda DateTime.Now (hora del servidor),
			// el cutoff se calcula con DateTime.Now para ser consistente.
			// Si algún día migras la BD a UTC, aquí cambiamos a UTC también.
			var cutoff = DateTime.Now.AddDays(-DaysToKeep);

			int totalDeleted = 0;

			while (!ct.IsCancellationRequested)
			{
				var deleted = await db.Database.ExecuteSqlInterpolatedAsync($@"
DELETE TOP ({BatchSize}) FROM TSedesConectadas
WHERE (activo = 0 OR activo IS NULL)
  AND fechaUltimoAcceso IS NOT NULL
  AND fechaUltimoAcceso < {cutoff};
", ct);

				totalDeleted += deleted;

				if (deleted == 0) break;

				await Task.Delay(TimeSpan.FromMilliseconds(200), ct);
			}

			var nowCo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ColombiaTz);
			_logger.LogWarning(
				"Cleanup TSedesConectadas OK (domingo CO={NowCo}): borradas {Total} filas inactivas > {Days} días",
				nowCo, totalDeleted, DaysToKeep);
		}

		private static TimeSpan GetDelayUntilNextSundayColombiaRandomWindow(TimeSpan start, TimeSpan end)
		{
			if (end <= start) throw new ArgumentException("WindowEnd debe ser mayor que WindowStart");

			var nowUtc = DateTime.UtcNow;
			var nowCo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, ColombiaTz);

			// Próximo domingo en Colombia (puede ser hoy)
			int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)nowCo.DayOfWeek + 7) % 7;
			var sundayCoDate = nowCo.Date.AddDays(daysUntilSunday);

			// Hora aleatoria dentro de la ventana (en Colombia)
			var windowSeconds = (int)(end - start).TotalSeconds;
			var randomSeconds = Random.Shared.Next(0, windowSeconds);
			var scheduledCo = sundayCoDate.Add(start).AddSeconds(randomSeconds);

			// Si hoy es domingo pero ya pasó la hora, programar siguiente domingo
			if (scheduledCo <= nowCo)
				scheduledCo = scheduledCo.AddDays(7);

			// Convertir scheduledCo (hora Colombia) a UTC para obtener el delay real
			var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(scheduledCo, ColombiaTz);

			return scheduledUtc - nowUtc;
		}
	}
}