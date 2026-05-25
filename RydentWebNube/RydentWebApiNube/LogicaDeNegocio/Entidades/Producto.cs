using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Table("Producto")]
	public class Producto
	{
		[Key]
		[Column("id_producto")]
		public long IdProducto { get; set; }

		[Required]
		[StringLength(50)]
		[Column("codigo")]
		public string Codigo { get; set; } = string.Empty;

		[Required]
		[StringLength(150)]
		[Column("nombre")]
		public string Nombre { get; set; } = string.Empty;

		[StringLength(500)]
		[Column("descripcion")]
		public string? Descripcion { get; set; }

		[Required]
		[Column("precio", TypeName = "decimal(18,2)")]
		public decimal Precio { get; set; }

		[Required]
		[StringLength(3)]
		[Column("moneda")]
		public string Moneda { get; set; } = "COP";

		[Required]
		[Column("activo")]
		public bool Activo { get; set; } = true;

		[Required]
		[Column("fecha_creacion")]
		public DateTime FechaCreacion { get; set; }

		public virtual ICollection<ProductoBeneficio> Beneficios { get; set; } = new List<ProductoBeneficio>();

	}
}
