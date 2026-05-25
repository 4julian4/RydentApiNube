namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	public class ProductoDto
	{
		public long IdProducto { get; set; }
		public string Codigo { get; set; } = string.Empty;
		public string Nombre { get; set; } = string.Empty;
		public string? Descripcion { get; set; }
		public decimal Precio { get; set; }
		public string Moneda { get; set; } = "COP";
		public bool Activo { get; set; }
		public DateTime FechaCreacion { get; set; }

		public List<ProductoBeneficioDto> Beneficios { get; set; } = new();
	}
}
