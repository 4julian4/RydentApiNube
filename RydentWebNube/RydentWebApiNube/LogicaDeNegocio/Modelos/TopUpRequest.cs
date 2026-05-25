namespace RydentWebApiNube.LogicaDeNegocio.Modelos
{
	/// <summary>DTO para recarga</summary>
	public class TopUpRequest
	{
		/// <summary>Código del tenant</summary>
		public string TenantCode { get; set; }
		/// <summary>Cantidad de operaciones a recargar (&gt; 0)</summary>
		public int OperationCount { get; set; }
		/// <summary>Monto de la recarga (≥ 0)</summary>
		public decimal Amount { get; set; }
		/// <summary>Usuario que realiza la recarga</summary>
		public string CreatedBy { get; set; }
		/// <summary>Comentarios de la recarga</summary>
		public string Comments { get; set; }
		public bool ResetBalanceBeforeTopUp { get; set; }
		public DateTime? LostBalanceExpiredAt { get; set; }
	}
}
