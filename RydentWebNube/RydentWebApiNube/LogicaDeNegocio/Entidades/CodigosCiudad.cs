using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Keyless]
	[Table("TCODIGOS_CIUDAD")]
	public class CodigosCiudad
	{
		public string? NOMBRE { get; set; }
		public string? CODIGO_DEPARTAMENTO { get; set; }
		public string? CODIGO_CIUDAD { get; set; }
	}
}