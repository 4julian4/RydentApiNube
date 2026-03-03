using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Keyless]
	[Table("TCODIGOS_CONSLUTAS")]
	public class CodigosConsultas
	{
		public string? CODIGO { get; set; }
		public string? NOMBRE { get; set; }
		public int? COSTO { get; set; }
	}
}