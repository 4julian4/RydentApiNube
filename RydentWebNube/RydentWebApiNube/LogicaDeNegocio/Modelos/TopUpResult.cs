namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	/// <summary>
	/// Resultado de una recarga
	/// </summary>
	public class TopUpResult
	{
		public bool Success { get; set; }
		public string Message { get; set; }
		public Guid TopUpId { get; set; }
	}
}
