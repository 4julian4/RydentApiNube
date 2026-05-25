	using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.Constantes;
	using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{

	public class PagoTransaccionService : IPagoTransaccionService
	{
		public async Task<PagoTransaccion> CrearAsync(PagoTransaccion transaccion)
		{
			using var db = new AppDbContext();

			transaccion.FechaCreacion = DateTime.Now;
			transaccion.FechaActualizacion = DateTime.Now;

			db.PagoTransaccion.Add(transaccion);
			await db.SaveChangesAsync();

			return transaccion;
		}

		public async Task<PagoTransaccion?> ObtenerPorReferenceAsync(string reference)
		{
			using var db = new AppDbContext();

			return await db.PagoTransaccion
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.Reference == reference);
		}
		public async Task<PagoTransaccion?> ObtenerPorWompiTransactionIdAsync(string wompiTransactionId)
		{
			using var db = new AppDbContext();

			return await db.PagoTransaccion
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.WompiTransactionId == wompiTransactionId);
		}

		public async Task<PagoTransaccion?> ObtenerPorIdAsync(long idTransaccion)
		{
			using var db = new AppDbContext();

			return await db.PagoTransaccion
				.AsNoTracking()
				.FirstOrDefaultAsync(x => x.IdTransaccion == idTransaccion);
		}

		public async Task<List<PagoTransaccion>> ListarPendientesAsync()
		{
			using var db = new AppDbContext();

			return await db.PagoTransaccion
				.AsNoTracking()
				.Where(x =>
					x.Estado == "INICIADA" ||
					x.Estado == "PENDIENTE"
				)
				.OrderBy(x => x.FechaCreacion)
				.ToListAsync();
		}

		public async Task<bool> ActualizarEstadoAsync(
			string reference,
			string estado,
			string? wompiTransactionId = null,
			string? wompiStatus = null,
			string? wompiStatusMessage = null,
			string? wompiPaymentMethodType = null,
			string? webhookJson = null
		)
		{
			using var db = new AppDbContext();

			var transaccion = await db.PagoTransaccion
				.FirstOrDefaultAsync(x => x.Reference == reference);

			if (transaccion == null)
				return false;

			transaccion.Estado = estado;
			transaccion.FechaActualizacion = DateTime.Now;

			if (!string.IsNullOrWhiteSpace(wompiTransactionId))
				transaccion.WompiTransactionId = wompiTransactionId;

			if (!string.IsNullOrWhiteSpace(wompiStatus))
				transaccion.WompiStatus = wompiStatus;

			if (!string.IsNullOrWhiteSpace(wompiStatusMessage))
				transaccion.WompiStatusMessage = wompiStatusMessage;

			if (!string.IsNullOrWhiteSpace(wompiPaymentMethodType))
				transaccion.WompiPaymentMethodType = wompiPaymentMethodType;

			if (!string.IsNullOrWhiteSpace(webhookJson))
				transaccion.WebhookJson = webhookJson;

			if (estado == EstadoPagoTransaccion.Aprobada)
			{
				transaccion.FechaPago = DateTime.Now;

			}

			await db.SaveChangesAsync();

			return true;
		}
		public async Task<List<PagoTransaccion>> ListarAbiertasParaSincronizarAsync(CancellationToken ct = default)
		{
			using var db = new AppDbContext();

			var fechaMinima = DateTime.Now.AddMinutes(-5);
			var fechaMaxima = DateTime.Now.AddDays(-2);

			return await db.PagoTransaccion
				.AsNoTracking()
				.Where(x =>
					(x.Estado == EstadoPagoTransaccion.Iniciada ||
					 x.Estado == EstadoPagoTransaccion.Pendiente) &&
					x.FechaCreacion <= fechaMinima &&
					x.FechaCreacion >= fechaMaxima
				)
				.OrderBy(x => x.FechaCreacion)
				.Take(50)
				.ToListAsync(ct);
		}
		public async Task MarcarIntentoConsultaAsync(string reference, CancellationToken ct = default)
		{
			using var db = new AppDbContext();

			var transaccion = await db.PagoTransaccion
				.FirstOrDefaultAsync(x => x.Reference == reference, ct);

			if (transaccion == null)
				return;

			transaccion.FechaActualizacion = DateTime.Now;

			await db.SaveChangesAsync(ct);
		}
	}
	public interface IPagoTransaccionService
	{
		Task<PagoTransaccion> CrearAsync(PagoTransaccion transaccion);
		Task<PagoTransaccion?> ObtenerPorReferenceAsync(string reference);
		Task<PagoTransaccion?> ObtenerPorIdAsync(long idTransaccion);
		Task<List<PagoTransaccion>> ListarPendientesAsync();
		Task<PagoTransaccion?> ObtenerPorWompiTransactionIdAsync(string wompiTransactionId);
		Task<List<PagoTransaccion>> ListarAbiertasParaSincronizarAsync(CancellationToken ct = default);

		Task MarcarIntentoConsultaAsync(string reference, CancellationToken ct = default);
		Task<bool> ActualizarEstadoAsync(
			string reference,
			string estado,
			string? wompiTransactionId = null,
			string? wompiStatus = null,
			string? wompiStatusMessage = null,
			string? wompiPaymentMethodType = null,
			string? webhookJson = null
		);
	}
}
