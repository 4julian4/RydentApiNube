namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class SedeConectadaView
	{
		public long idSedeConectada { get; set; }
		public long? idCliente { get; set; }
		public string? nombreCliente { get; set; }

		public long? idSede { get; set; }
		public string? idActualSignalR { get; set; }
		public DateTime? fechaUltimoAcceso { get; set; }
		public bool? activo { get; set; }
	}
}