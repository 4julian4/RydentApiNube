using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RydentWebApiNube.LogicaDeNegocio.Servicios;

namespace RydentWebApiNube.LogicaDeNegocio.Workers
{
	public class WompiTransaccionesWorker : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<WompiTransaccionesWorker> _logger;

		public WompiTransaccionesWorker(
			IServiceProvider serviceProvider,
			ILogger<WompiTransaccionesWorker> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			_logger.LogInformation("Worker de transacciones Wompi iniciado.");

			using var timer = new PeriodicTimer(TimeSpan.FromMinutes(60));

			while (!stoppingToken.IsCancellationRequested &&
				   await timer.WaitForNextTickAsync(stoppingToken))
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();

					var syncService = scope.ServiceProvider
						.GetRequiredService<IWompiSincronizacionService>();

					await syncService.SincronizarTransaccionesAbiertasAsync(stoppingToken);
				}
				catch (OperationCanceledException)
				{
					break;
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "Error sincronizando transacciones abiertas de Wompi.");
				}
			}

			_logger.LogInformation("Worker de transacciones Wompi detenido.");
		}
	}
}
