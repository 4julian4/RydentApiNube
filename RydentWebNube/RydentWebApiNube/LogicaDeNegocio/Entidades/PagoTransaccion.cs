using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Table("pago_transaccion")]
	public class PagoTransaccion
	{
		[Key]
		[Column("id_transaccion")]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public long IdTransaccion { get; set; }

		[Required]
		[StringLength(100)]
		[Column("reference")]
		public string Reference { get; set; } = string.Empty;

		[Required]
		[Column("id_cliente")]
		public long IdCliente { get; set; }

		[StringLength(200)]
		[Column("correo_cliente")]
		public string? CorreoCliente { get; set; }

		[Required]
		[Column("id_producto")]
		public long IdProducto { get; set; }

		[StringLength(50)]
		[Column("codigo_producto")]
		public string? CodigoProducto { get; set; }

		[Required]
		[StringLength(150)]
		[Column("nombre_producto")]
		public string NombreProducto { get; set; } = string.Empty;

		[Column("id_beneficio")]
		public long? IdBeneficio { get; set; }

		[StringLength(30)]
		[Column("tipo_beneficio")]
		public string? TipoBeneficio { get; set; }

		[Column("cantidad_facturas")]
		public int? CantidadFacturas { get; set; }

		[Column("dias_acceso")]
		public int? DiasAcceso { get; set; }

		[Required]
		[Column("precio", TypeName = "decimal(18,2)")]
		public decimal Precio { get; set; }

		[Required]
		[Column("amount_in_cents")]
		public long AmountInCents { get; set; }

		[Required]
		[StringLength(3)]
		[Column("currency")]
		public string Currency { get; set; } = "COP";

		[Required]
		[StringLength(30)]
		[Column("estado")]
		public string Estado { get; set; } = "INICIADA";

		[StringLength(200)]
		[Column("public_key")]
		public string? PublicKey { get; set; }

		[StringLength(500)]
		[Column("redirect_url")]
		public string? RedirectUrl { get; set; }

		[Required]
		[StringLength(200)]
		[Column("signature")]
		public string Signature { get; set; } = string.Empty;

		[StringLength(100)]
		[Column("wompi_transaction_id")]
		public string? WompiTransactionId { get; set; }

		[StringLength(50)]
		[Column("wompi_status")]
		public string? WompiStatus { get; set; }

		[StringLength(500)]
		[Column("wompi_status_message")]
		public string? WompiStatusMessage { get; set; }

		[StringLength(50)]
		[Column("wompi_payment_method_type")]
		public string? WompiPaymentMethodType { get; set; }

		[Required]
		[Column("fecha_creacion")]
		public DateTime FechaCreacion { get; set; } = DateTime.Now;

		[Column("fecha_actualizacion")]
		public DateTime? FechaActualizacion { get; set; }

		[Column("fecha_pago")]
		public DateTime? FechaPago { get; set; }

		[Column("request_json")]
		public string? RequestJson { get; set; }

		[Column("response_json")]
		public string? ResponseJson { get; set; }

		[Column("webhook_json")]
		public string? WebhookJson { get; set; }
	}
}
