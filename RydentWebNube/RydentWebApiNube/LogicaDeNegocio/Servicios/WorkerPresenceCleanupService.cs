// WorkerPresenceCleanupService.cs
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

namespace RydentWebApiNube.LogicaDeNegocio.Services
{
	public class WorkerPresenceCleanupService : BackgroundService
	{
		private readonly WorkerPresenceRegistry _presence;
		private readonly ILogger<WorkerPresenceCleanupService> _logger;
		private readonly IServiceScopeFactory _scopeFactory;

		// ✅ Pasar la escoba cada 3 minutos
		private static readonly TimeSpan Interval = TimeSpan.FromMinutes(3);

		public WorkerPresenceCleanupService(
			WorkerPresenceRegistry presence,
			ILogger<WorkerPresenceCleanupService> logger,
			IServiceScopeFactory scopeFactory)
		{
			_presence = presence;
			_logger = logger;
			_scopeFactory = scopeFactory;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					// 1) Limpia RAM (presencias vencidas por TTL)
					var count = _presence.ExpireStale(out var removed);

					if (count > 0)
					{
						_logger.LogWarning(
							"Presence expired (RAM): {Count}. Ej: {Sample}",
							count,
							string.Join(", ",
								removed.Take(3).Select(x => $"{x.IdentificadorLocal}/{x.ConnectionId}"))
						);

						// 2) Limpia SQL SOLO para esos connId vencidos (seguro)
						await MarkSqlInactiveForExpiredAsync(removed, stoppingToken);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error expiring stale worker presence");
				}

				await Task.Delay(Interval, stoppingToken);
			}
		}

		private async Task MarkSqlInactiveForExpiredAsync(
			System.Collections.Generic.List<WorkerPresence> removed,
			CancellationToken ct)
		{
			// Scoped service
			using var scope = _scopeFactory.CreateScope();
			var sedesConectadas = scope.ServiceProvider.GetRequiredService<ISedesConectadasServicios>();

			foreach (var p in removed)
			{
				if (ct.IsCancellationRequested) break;

				try
				{
					if (string.IsNullOrWhiteSpace(p.ConnectionId))
						continue;

					// Buscar por connId expirado
					var row = await sedesConectadas.ConsultarPorIdSignalR(p.ConnectionId);

					// ✅ Seguridad:
					// Solo desactivamos si:
					// - existe
					// - está activo
					// - su idActualSignalR sigue siendo el mismo connId expirado
					if (row?.idSedeConectada > 0 &&
						(row.activo ?? false) &&
						string.Equals(row.idActualSignalR, p.ConnectionId, StringComparison.Ordinal))
					{
						row.activo = false;
						row.fechaUltimoAcceso = DateTime.Now; // consistente con tu código actual
						await sedesConectadas.Editar(row.idSedeConectada, row);

						_logger.LogWarning(
							"Presence expired (SQL): idSede={IdSede}, ident={Ident}, connId={ConnId}",
							p.IdSede, p.IdentificadorLocal, p.ConnectionId
						);
					}
				}
				catch (Exception ex)
				{
					_logger.LogError(ex,
						"Error marking SQL inactive for connId={ConnId} ident={Ident}",
						p.ConnectionId, p.IdentificadorLocal);
				}
			}
		}
	}
}