using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Table("TSesionesUsuario")]
	public class SesionesUsuario
	{
		[Key]
		public long idSesion { get; set; }

		public long idUsuario { get; set; }
		public long? idCliente { get; set; }
		public string correoUsuario { get; set; } = "";
		public string sessionId { get; set; } = "";
		public bool activa { get; set; }
		public DateTime fechaLogin { get; set; }
		public DateTime fechaUltimaActividad { get; set; }
		public DateTime? fechaCierre { get; set; }
		public string? motivoCierre { get; set; }
		public string? ip { get; set; }
		public string? userAgent { get; set; }
	}
}