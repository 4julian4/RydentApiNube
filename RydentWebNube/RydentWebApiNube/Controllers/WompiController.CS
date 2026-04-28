using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RydentWebApiNube.LogicaDeNegocio.Modelos;
using RydentWebApiNube.LogicaDeNegocio.Modelos.WompiRequests;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RydentWebApiNube.Controllers
{
    [ApiController]
    [Route("api/wompi")]
    public class WompiController : ControllerBase
    {
        private readonly WompiOptions _wompiOptions;

        public WompiController(IOptions<WompiOptions> wompiOptions)
        {
            _wompiOptions = wompiOptions.Value;
        }

        [HttpGet("config-test")]
        public IActionResult ConfigTest()
        {
            return Ok(new
            {
                publicKey = _wompiOptions.PublicKey,
                currency = _wompiOptions.Currency,
                redirectUrl = _wompiOptions.RedirectUrl,
                integritySecretConfigured = !string.IsNullOrWhiteSpace(_wompiOptions.IntegritySecret),
                eventsSecretConfigured = !string.IsNullOrWhiteSpace(_wompiOptions.EventsSecret)
            });
        }
        
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine("===== WEBHOOK WOMPI RECIBIDO =====");
            Console.WriteLine(body);
            Console.WriteLine("==================================");

            if (string.IsNullOrWhiteSpace(body))
            {
                return BadRequest(new
                {
                    mensaje = "Body vacío"
                });
            }

            try
            {
                using var json = JsonDocument.Parse(body);

                var root = json.RootElement;

                string? evento = null;
                string? transaccionId = null;
                string? referencia = null;
                string? estado = null;
                long? montoCentavos = null;

                if (root.TryGetProperty("event", out var eventElement))
                {
                    evento = eventElement.GetString();
                }

                if (root.TryGetProperty("data", out var dataElement) &&
                    dataElement.TryGetProperty("transaction", out var transactionElement))
                {
                    if (transactionElement.TryGetProperty("id", out var idElement))
                    {
                        transaccionId = idElement.GetString();
                    }

                    if (transactionElement.TryGetProperty("reference", out var referenceElement))
                    {
                        referencia = referenceElement.GetString();
                    }

                    if (transactionElement.TryGetProperty("status", out var statusElement))
                    {
                        estado = statusElement.GetString();
                    }

                    if (transactionElement.TryGetProperty("amount_in_cents", out var amountElement))
                    {
                        montoCentavos = amountElement.GetInt64();
                    }
                }

                Console.WriteLine($"Evento: {evento}");
                Console.WriteLine($"Transacción: {transaccionId}");
                Console.WriteLine($"Referencia: {referencia}");
                Console.WriteLine($"Estado: {estado}");
                Console.WriteLine($"Monto centavos: {montoCentavos}");

                // Demo:
                // Aquí todavía NO guardamos en BD.
                // En producción aquí validarías firma/evento y actualizarías la compra.

                return Ok(new
                {
                    recibido = true,
                    evento,
                    transaccionId,
                    referencia,
                    estado,
                    montoCentavos
                });
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Error parseando JSON de webhook Wompi:");
                Console.WriteLine(ex.Message);

                return BadRequest(new
                {
                    mensaje = "JSON inválido"
                });
            }
        }
        
        [HttpPost("crear-checkout")]
        public IActionResult CrearCheckout([FromBody] CrearCheckoutRequest request)
        {
            var paquete = ObtenerPaquete(request.IdPaquete);

            if (paquete == null)
            {
                return BadRequest(new
                {
                    mensaje = "Paquete no válido"
                });
            }

            var reference = $"PBI003-{paquete.Id}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
            var amountInCents = paquete.Precio * 100;
            var currency = _wompiOptions.Currency;
            var integritySecret = _wompiOptions.IntegritySecret;

            var cadenaFirma = $"{reference}{amountInCents}{currency}{integritySecret}";
            var signature = Sha256(cadenaFirma);

            return Ok(new
            {
                publicKey = _wompiOptions.PublicKey,
                reference,
                amountInCents,
                currency,
                signature,
                redirectUrl = _wompiOptions.RedirectUrl,
                paquete = new
                {
                    paquete.Id,
                    paquete.Nombre,
                    paquete.CantidadFacturas,
                    paquete.Precio
                }
            });
        }

        private static string Sha256(string rawData)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToHexString(bytes).ToLower();
        }

        private static Paquete? ObtenerPaquete(string idPaquete)
        {
            var paquetes = new List<Paquete>
            {
                new Paquete
                {
                    Id = "BASICO",
                    Nombre = "Paquete Básico",
                    CantidadFacturas = 100,
                    Precio = 100000
                },
                new Paquete
                {
                    Id = "PLUS",
                    Nombre = "Paquete Plus",
                    CantidadFacturas = 500,
                    Precio = 220000
                },
                new Paquete
                {
                    Id = "PRO",
                    Nombre = "Paquete Pro",
                    CantidadFacturas = 1000,
                    Precio = 300000
                }
            };

            return paquetes.FirstOrDefault(x =>
                x.Id.Equals(idPaquete, StringComparison.OrdinalIgnoreCase)
            );
        }
    }
}