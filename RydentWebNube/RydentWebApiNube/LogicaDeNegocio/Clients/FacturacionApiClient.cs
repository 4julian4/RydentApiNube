using RydentWebApiNube.LogicaDeNegocio.Modelos;

namespace RydentWebApiNube.LogicaDeNegocio.Clients
{
	public class FacturacionApiClient : IFacturacionApiClient
	{
		private readonly HttpClient _httpClient;

		public FacturacionApiClient(HttpClient httpClient)
		{
			_httpClient = httpClient;
		}

		public async Task<TopUpResult?> RecargarAsync(TopUpRequest request, CancellationToken ct = default)
		{
			var response = await _httpClient.PostAsJsonAsync(
				"api/dataico-control/recarga_id",
				request,
				ct
			);

			if (!response.IsSuccessStatusCode)
			{
				var error = await response.Content.ReadAsStringAsync(ct);
				throw new Exception($"Error recargando facturación: {response.StatusCode} - {error}");
			}

			return await response.Content.ReadFromJsonAsync<TopUpResult>(cancellationToken: ct);
		}
	}

	public interface IFacturacionApiClient
	{
		Task<TopUpResult?> RecargarAsync(TopUpRequest request, CancellationToken ct = default);
	}

}
