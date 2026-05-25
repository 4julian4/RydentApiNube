namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class ProductoBeneficioDto
	{
		public long IdBeneficio { get; set; }
		public long IdProducto { get; set; }
		public string TipoBeneficio { get; set; } = string.Empty;
		public int? CantidadFacturas { get; set; }
		public int? DiasAcceso { get; set; }
		public bool Activo { get; set; }
	}
}
