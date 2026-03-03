using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Keyless]
	[Table("TCODIGOS_PROCEDIMIENTOS")]
	public class CodigosProcedimientos
	{
		public int? ID { get; set; }
		public string? NOMBRE { get; set; }
		public string? CODIGO { get; set; }
		public double? COSTO { get; set; }
		public string? CATEGORIA { get; set; }
		public string? TIPO { get; set; }
		public string? TIPO_RIPS { get; set; }
		public string? INSUMO { get; set; }
		public string? INSUMO_REF { get; set; }
		public int? IVA { get; set; }
		public string? CODINTERNO { get; set; }
	}
}