using System.Collections.Concurrent;

namespace RydentWebApiNube.LogicaDeNegocio.Services
{
	public record WorkerPresence(
		long IdSede,
		string IdentificadorLocal,
		string ConnectionId,
		DateTime LastSeenUtc
	);

	/// <summary>
	/// "Pizarra" en memoria:
	/// - qué sede tiene qué worker activo
	/// - cuándo fue el último latido
	///
	/// ✅ Índices:
	///   _bySede: sede -> presence actual
	///   _sedeByIdentificadorLocal: identificadorLocal -> sede
	///   _sedeByConnectionId: connectionId (viejo o actual) -> sede
	/// </summary>
	public class WorkerPresenceRegistry
	{
		private readonly ConcurrentDictionary<long, WorkerPresence> _bySede = new();
		private readonly ConcurrentDictionary<string, long> _sedeByIdentificadorLocal = new();
		private readonly ConcurrentDictionary<string, long> _sedeByConnectionId = new();

		// Ajusta: si no late en 90s, lo consideramos muerto
		public TimeSpan Ttl { get; } = TimeSpan.FromSeconds(180);


		public void Upsert(long idSede, string identificadorLocal, string connectionId)
		{
			var now = DateTime.UtcNow;

			if (!string.IsNullOrWhiteSpace(identificadorLocal))
				_sedeByIdentificadorLocal[identificadorLocal] = idSede;

			if (!string.IsNullOrWhiteSpace(connectionId))
				_sedeByConnectionId[connectionId] = idSede;

			_bySede[idSede] = new WorkerPresence(
				IdSede: idSede,
				IdentificadorLocal: identificadorLocal ?? "",
				ConnectionId: connectionId ?? "",
				LastSeenUtc: now
			);
		}

		/// <summary>
		/// Marca "vivo" el worker de una sede ya conocida (por ejemplo después de resolver sede).
		/// </summary>
		public void MarkSeen(long idSede, string connectionId)
		{
			var now = DateTime.UtcNow;

			if (!string.IsNullOrWhiteSpace(connectionId))
				_sedeByConnectionId[connectionId] = idSede;

			_bySede.AddOrUpdate(
				idSede,
				_ => new WorkerPresence(idSede, "", connectionId ?? "", now),
				(_, old) => old with
				{
					ConnectionId = string.IsNullOrWhiteSpace(connectionId) ? old.ConnectionId : connectionId,
					LastSeenUtc = now
				}
			);
		}

		public bool TryGetActiveConnectionBySede(long idSede, out string connectionId)
		{
			connectionId = "";
			if (!_bySede.TryGetValue(idSede, out var p)) return false;
			if (DateTime.UtcNow - p.LastSeenUtc > Ttl) return false;

			connectionId = p.ConnectionId;
			return !string.IsNullOrWhiteSpace(connectionId);
		}

		public bool TryGetActiveConnectionByIdentificadorLocal(string identificadorLocal, out string connectionId)
		{
			connectionId = "";
			if (string.IsNullOrWhiteSpace(identificadorLocal)) return false;

			if (!_sedeByIdentificadorLocal.TryGetValue(identificadorLocal, out var idSede))
				return false;

			return TryGetActiveConnectionBySede(idSede, out connectionId);
		}

		/// <summary>
		/// ✅ Si te llega un connectionId viejo o actual:
		/// - encuentra la sede a la que pertenecía
		/// - devuelve el connectionId activo de ESA sede (si está vivo por TTL)
		/// </summary>
		public bool TryResolveActiveByAnyConnectionId(string anyConnectionId, out string activeConnectionId)
		{
			activeConnectionId = "";
			if (string.IsNullOrWhiteSpace(anyConnectionId)) return false;

			if (!_sedeByConnectionId.TryGetValue(anyConnectionId, out var idSede))
				return false;

			return TryGetActiveConnectionBySede(idSede, out activeConnectionId);
		}

		public bool TryGetSedeByConnectionId(string connectionId, out long idSede)
		{
			idSede = 0;
			if (string.IsNullOrWhiteSpace(connectionId)) return false;
			return _sedeByConnectionId.TryGetValue(connectionId, out idSede) && idSede > 0;
		}

		/// <summary>
		/// 🔥 CLAVE:
		/// Si se desconecta un connectionId viejo, NO debemos borrar el presence actual
		/// si esa sede ya tiene OTRO connectionId más nuevo.
		/// </summary>
		public void RemoveByConnection(string connectionId)
		{
			if (string.IsNullOrWhiteSpace(connectionId)) return;

			// Quitamos el índice connectionId -> sede (si existe)
			if (_sedeByConnectionId.TryRemove(connectionId, out var idSede))
			{
				// ✅ Solo borramos _bySede[idSede] si el presence actual usa ESTE MISMO connectionId
				if (_bySede.TryGetValue(idSede, out var current))
				{
					if (string.Equals(current.ConnectionId, connectionId, StringComparison.Ordinal))
					{
						_bySede.TryRemove(idSede, out var removed);

						// Limpia identificadorLocal -> sede solo si apunta a esa misma sede
						if (removed != null && !string.IsNullOrWhiteSpace(removed.IdentificadorLocal))
						{
							if (_sedeByIdentificadorLocal.TryGetValue(removed.IdentificadorLocal, out var sedeFromIdLocal) &&
								sedeFromIdLocal == idSede)
							{
								_sedeByIdentificadorLocal.TryRemove(removed.IdentificadorLocal, out _);
							}
						}
					}
					// Si no coincide, significa que ya hay un connectionId nuevo activo:
					// NO TOCAMOS _bySede ni _sedeByIdentificadorLocal.
				}

				return;
			}

			// Fallback lento (si no estaba indexado)
			foreach (var kv in _bySede)
			{
				if (kv.Value.ConnectionId == connectionId)
				{
					_bySede.TryRemove(kv.Key, out var removed);

					if (removed != null)
					{
						if (!string.IsNullOrWhiteSpace(removed.IdentificadorLocal))
						{
							if (_sedeByIdentificadorLocal.TryGetValue(removed.IdentificadorLocal, out var sedeFromIdLocal) &&
								sedeFromIdLocal == kv.Key)
							{
								_sedeByIdentificadorLocal.TryRemove(removed.IdentificadorLocal, out _);
							}
						}
					}

					_sedeByConnectionId.TryRemove(connectionId, out _);
					break;
				}
			}
		}

		// ======================================================
		// ✅ NUEVO: Limpieza automática de presencias vencidas
		// ======================================================
		/// <summary>
		/// Recorre las sedes y elimina del registry las que llevan más de TTL sin latir.
		/// ✅ Seguro: solo borra si el registro no cambió mientras lo revisamos.
		/// Retorna cuántos removió y entrega la lista removida para logs.
		/// </summary>
		public int ExpireStale(out List<WorkerPresence> removed)
		{
			removed = new List<WorkerPresence>();
			var now = DateTime.UtcNow;

			foreach (var kv in _bySede)
			{
				var idSede = kv.Key;
				var p = kv.Value;

				// ¿ya venció?
				if (now - p.LastSeenUtc <= Ttl) continue;

				// ✅ Anti-borrado de activos:
				// solo borramos si sigue siendo EXACTAMENTE el mismo presence que vimos
				if (_bySede.TryGetValue(idSede, out var current) &&
					current.LastSeenUtc == p.LastSeenUtc &&
					string.Equals(current.ConnectionId, p.ConnectionId, StringComparison.Ordinal))
				{
					// Borra coherentemente (índices + bySede)
					RemoveByConnection(p.ConnectionId);
					removed.Add(p);
				}
			}

			return removed.Count;
		}
	}
}