using RydentWebApiNube.LogicaDeNegocio.Modelos.WompiRequests;
using Microsoft.Extensions.Options;
using RydentWebApiNube.LogicaDeNegocio.Modelos;
using System.Net.Http.Headers;
using System.Text.Json;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{

	public class WompiService : IWompiService
	{
		private readonly HttpClient _httpClient;
		private readonly WompiOptions _wompiOptions;

		public WompiService(
			HttpClient httpClient,
			IOptions<WompiOptions> wompiOptions)
		{
			_httpClient = httpClient;
			_wompiOptions = wompiOptions.Value;
		}

		public async Task<WompiTransaccionConsultaDto?> ConsultarTransaccionPorIdAsync(
			string wompiTransactionId,
			CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(wompiTransactionId))
				return null;

			var request = new HttpRequestMessage(
				HttpMethod.Get,
				$"/v1/transactions/{wompiTransactionId}"
			);

			request.Headers.Authorization = new AuthenticationHeaderValue(
				"Bearer",
				_wompiOptions.PrivateKey
			);

			var response = await _httpClient.SendAsync(request, ct);

			var rawJson = await response.Content.ReadAsStringAsync(ct);

			if (!response.IsSuccessStatusCode)
			{
				Console.WriteLine($"Error consultando Wompi: {response.StatusCode}");
				Console.WriteLine(rawJson);
				return null;
			}

			using var json = JsonDocument.Parse(rawJson);

			if (!json.RootElement.TryGetProperty("data", out var data))
				return null;

			var id = data.TryGetProperty("id", out var idElement)
				? idElement.GetString() ?? string.Empty
				: string.Empty;

			var reference = data.TryGetProperty("reference", out var referenceElement)
				? referenceElement.GetString() ?? string.Empty
				: string.Empty;

			var status = data.TryGetProperty("status", out var statusElement)
				? statusElement.GetString() ?? string.Empty
				: string.Empty;

			long amountInCents = 0;

			if (data.TryGetProperty("amount_in_cents", out var amountElement) &&
				amountElement.ValueKind == JsonValueKind.Number)
			{
				amountInCents = amountElement.GetInt64();
			}

			var paymentMethodType = data.TryGetProperty("payment_method_type", out var paymentMethodElement)
				? paymentMethodElement.GetString()
				: null;

			var statusMessage = data.TryGetProperty("status_message", out var statusMessageElement)
				? statusMessageElement.GetString()
				: null;

			return new WompiTransaccionConsultaDto
			{
				Id = id,
				Reference = reference,
				Status = status,
				AmountInCents = amountInCents,
				PaymentMethodType = paymentMethodType,
				StatusMessage = statusMessage,
				RawJson = rawJson
			};
		}
	}
	public interface IWompiService
	{
		Task<WompiTransaccionConsultaDto?> ConsultarTransaccionPorIdAsync(
			string wompiTransactionId,
			CancellationToken ct = default
		);
	}
}
