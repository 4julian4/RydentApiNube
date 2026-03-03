using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Keyless]
	[Table("TCODIGOS_EPS")]
	public class CodigosEps
	{
		public string? CODIGO { get; set; }
		public string? NOMBRE { get; set; }
		public string? TELEFONO { get; set; }
		public string? RIPS { get; set; }
	}
}