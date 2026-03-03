using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RydentWebApiNube.LogicaDeNegocio.Entidades
{
	[Table("TClientes")]
	public class Clientes
	{
		[Key]
		public long idCliente { get; set; }

		public Guid clienteGuid { get; set; }          // NOT NULL en BD (tiene DEFAULT NEWID)

		public string? nombreCliente { get; set; }
		public DateTime? activoHasta { get; set; }
		public string? observacion { get; set; }

		public string? telefono1 { get; set; }
		public string? telefono2 { get; set; }
		public string? emailContacto { get; set; }
		public string? direccion { get; set; }
		public string? ciudad { get; set; }
		public string? pais { get; set; }

		public DateTime? clienteDesde { get; set; }
		public byte? diaPago { get; set; }
		public DateTime? fechaProximoPago { get; set; }
		public string? planNombre { get; set; }

		public bool usaRydentWeb { get; set; }
		public bool usaDataico { get; set; }
		public bool usaFacturaTech { get; set; }

		public Guid? billingTenantId { get; set; }     // Tenant.Id en Billing (GUID)

		public bool estado { get; set; }
		public DateTime fechaCreacion { get; set; }
		public DateTime? fechaActualizacion { get; set; }
	}
}
