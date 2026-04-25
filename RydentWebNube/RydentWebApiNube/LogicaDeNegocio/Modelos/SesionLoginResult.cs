namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class SesionLoginResult
	{
		public bool PuedeEntrar { get; set; }
		public bool RequiereConfirmacion { get; set; }
		public string Mensaje { get; set; } = "";
		public string SessionId { get; set; } = "";
	}
}
