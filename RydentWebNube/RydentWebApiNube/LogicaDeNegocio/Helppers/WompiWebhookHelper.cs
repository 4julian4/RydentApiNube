
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RydentWebApiNube.LogicaDeNegocio.Helppers
{
	public static class WompiWebhookHelper
	{
		public static bool ValidarChecksum(
			JsonElement root,
			string eventsSecret,
			string? checksumHeader = null)
		{
			if (string.IsNullOrWhiteSpace(eventsSecret))
				return false;

			string? checksumRecibido = checksumHeader;

			if (string.IsNullOrWhiteSpace(checksumRecibido) &&
				root.TryGetProperty("signature", out var signatureElement) &&
				signatureElement.TryGetProperty("checksum", out var checksumElement))
			{
				checksumRecibido = checksumElement.GetString();
			}

			if (string.IsNullOrWhiteSpace(checksumRecibido))
				return false;

			if (!root.TryGetProperty("signature", out var signature) ||
				!signature.TryGetProperty("properties", out var properties) ||
				properties.ValueKind != JsonValueKind.Array)
			{
				return false;
			}

			if (!root.TryGetProperty("timestamp", out var timestampElement))
				return false;

			if (!root.TryGetProperty("data", out var dataElement))
				return false;

			var cadena = new StringBuilder();

			foreach (var property in properties.EnumerateArray())
			{
				var propertyPath = property.GetString();

				if (string.IsNullOrWhiteSpace(propertyPath))
					return false;

				if (!TryGetNestedProperty(dataElement, propertyPath, out var value))
					return false;

				cadena.Append(JsonElementToChecksumValue(value));
			}

			cadena.Append(timestampElement.GetRawText());
			cadena.Append(eventsSecret);

			var checksumCalculado = Sha256(cadena.ToString());

			return string.Equals(
				checksumCalculado,
				checksumRecibido,
				StringComparison.OrdinalIgnoreCase
			);
		}

		public static bool TryGetNestedProperty(JsonElement element, string path, out JsonElement value)
		{
			value = default;

			var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries);
			var current = element;

			foreach (var part in parts)
			{
				if (current.ValueKind != JsonValueKind.Object ||
					!current.TryGetProperty(part, out current))
				{
					return false;
				}
			}

			value = current;
			return true;
		}

		private static string JsonElementToChecksumValue(JsonElement element)
		{
			return element.ValueKind switch
			{
				JsonValueKind.String => element.GetString() ?? string.Empty,
				JsonValueKind.Number => element.GetRawText(),
				JsonValueKind.True => "true",
				JsonValueKind.False => "false",
				JsonValueKind.Null => string.Empty,
				_ => element.GetRawText()
			};
		}

		public static string Sha256(string rawData)
		{
			var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
			return Convert.ToHexString(bytes).ToLowerInvariant();
		}
	}
}
