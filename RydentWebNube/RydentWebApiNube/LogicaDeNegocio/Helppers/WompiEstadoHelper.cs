using RydentWebApiNube.LogicaDeNegocio.Constantes;

namespace RydentWebApiNube.LogicaDeNegocio.Helppers
{
	public static class WompiEstadoHelper
	{
		public static string MapearEstadoWompi(string estadoWompi)
		{
			return estadoWompi.ToUpperInvariant() switch
			{
				"APPROVED" => EstadoPagoTransaccion.Aprobada,
				"DECLINED" => EstadoPagoTransaccion.Rechazada,
				"VOIDED" => EstadoPagoTransaccion.Expirada,
				"ERROR" => EstadoPagoTransaccion.Error,
				"PENDING" => EstadoPagoTransaccion.Pendiente,
				_ => EstadoPagoTransaccion.Pendiente
			};
		}
	}
}
