using RydentWebApiNube.LogicaDeNegocio.Constantes;
using RydentWebApiNube.LogicaDeNegocio.Helppers;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
	public class WompiSincronizacionService : IWompiSincronizacionService
	{
		private readonly IPagoTransaccionService _pagoTransaccionService;
		private readonly IWompiService _wompiService;

		public WompiSincronizacionService(
			IPagoTransaccionService pagoTransaccionService,
			IWompiService wompiService)
		{
			_pagoTransaccionService = pagoTransaccionService;
			_wompiService = wompiService;
		}

		public async Task SincronizarTransaccionesAbiertasAsync(CancellationToken ct = default)
		{
			var abiertas = await _pagoTransaccionService.ListarAbiertasParaSincronizarAsync(ct);

			foreach (var transaccion in abiertas)
			{
				if (ct.IsCancellationRequested)
					break;

				try
				{
					if (string.IsNullOrWhiteSpace(transaccion.WompiTransactionId))
					{
						await _pagoTransaccionService.MarcarIntentoConsultaAsync(
							transaccion.Reference,
							ct
						);

						continue;
					}

					var resultadoWompi = await _wompiService.ConsultarTransaccionPorIdAsync(
						transaccion.WompiTransactionId,
						ct
					);

					if (resultadoWompi == null)
					{
						await _pagoTransaccionService.MarcarIntentoConsultaAsync(
							transaccion.Reference,
							ct
						);

						continue;
					}

					if (resultadoWompi.AmountInCents != transaccion.AmountInCents)
					{
						await _pagoTransaccionService.ActualizarEstadoAsync(
							transaccion.Reference,
							EstadoPagoTransaccion.Error,
							wompiTransactionId: resultadoWompi.Id,
							wompiStatus: resultadoWompi.Status,
							wompiStatusMessage: "Monto consultado en Wompi no coincide con la transacción local",
							wompiPaymentMethodType: resultadoWompi.PaymentMethodType,
							webhookJson: resultadoWompi.RawJson
						);

						continue;
					}

					var estadoLocal = WompiEstadoHelper.MapearEstadoWompi(resultadoWompi.Status);

					await _pagoTransaccionService.ActualizarEstadoAsync(
						transaccion.Reference,
						estadoLocal,
						wompiTransactionId: resultadoWompi.Id,
						wompiStatus: resultadoWompi.Status,
						wompiStatusMessage: resultadoWompi.StatusMessage,
						wompiPaymentMethodType: resultadoWompi.PaymentMethodType,
						webhookJson: resultadoWompi.RawJson
					);
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error sincronizando transacción {transaccion.Reference}: {ex.Message}");

					await _pagoTransaccionService.MarcarIntentoConsultaAsync(
						transaccion.Reference,
						ct
					);
				}
			}
		}
	}
	public interface IWompiSincronizacionService
	{
		Task SincronizarTransaccionesAbiertasAsync(CancellationToken ct = default);
	}
}
