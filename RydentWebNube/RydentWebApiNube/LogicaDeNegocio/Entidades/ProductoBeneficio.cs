using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{

	[Table("producto_beneficio")]
	public class ProductoBeneficio
	{
		[Key]
		[Column("id_beneficio")]
		public long IdBeneficio { get; set; }

		[Required]
		[Column("id_producto")]
		public long IdProducto { get; set; }

		[Required]
		[StringLength(30)]
		[Column("tipo_beneficio")]
		public string TipoBeneficio { get; set; } = string.Empty;

		[Column("cantidad_facturas")]
		public int? CantidadFacturas { get; set; }

		[Column("dias_acceso")]
		public int? DiasAcceso { get; set; }

		[Required]
		[Column("activo")]
		public bool Activo { get; set; } = true;

		[ForeignKey(nameof(IdProducto))]
		public virtual Producto Producto { get; set; } = null!;
	}
}
