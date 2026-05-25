namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class TokenUserData
	{
		public long IdUsuario { get; set; }
		public long IdCliente { get; set; }
		public string SessionId { get; set; } = string.Empty;
		public string Correo { get; set; } = string.Empty;
		public bool PuedeUsarRydentWeb { get; set; }
		public bool PuedeUsarFacturacion { get; set; }
	}
}
