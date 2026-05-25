namespace RydentWebApiNube.LogicaDeNegocio.Modelos.WompiRequests
{
	public class WompiTransaccionConsultaDto
	{
		public string Id { get; set; } = string.Empty;
		public string Reference { get; set; } = string.Empty;
		public string Status { get; set; } = string.Empty;
		public long AmountInCents { get; set; }
		public string? PaymentMethodType { get; set; }
		public string? StatusMessage { get; set; }
		public string RawJson { get; set; } = string.Empty;
	}
}
