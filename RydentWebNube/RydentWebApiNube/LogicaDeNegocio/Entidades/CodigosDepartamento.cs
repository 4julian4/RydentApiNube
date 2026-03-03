using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Keyless]
	[Table("TCODIGOS_DEPARTAMENTO")]
	public class CodigosDepartamento
	{
		[Key]
		public string CODIGO_DEPARTAMENTO { get; set; } = string.Empty;
		public string? NOMBRE { get; set; }
	}
}