using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RydentWebApiNube.LogicaDeNegocio.Clients;
using RydentWebApiNube.LogicaDeNegocio.Constantes;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Helppers;
using RydentWebApiNube.LogicaDeNegocio.Modelos;
using RydentWebApiNube.LogicaDeNegocio.Modelos.WompiRequests;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RydentWebApiNube.Controllers
{
    [ApiController]
    [Route("api/wompi")]
    public class WompiController : ControllerBase
    {
		private const string PurposeRenewCheckout = "renew_checkout";

        private readonly WompiOptions _wompiOptions;
		private readonly IProductoService _productoService;
		private readonly ITokenUserService _tokenUserService;
		private readonly IPagoTransaccionService _pagoTransaccionService;
		private readonly IConfiguration _configuration;


		private readonly IFacturacionApiClient _facturacionApiClient;
		private readonly IClientesServicios _clientesServicios;



		public WompiController(IOptions<WompiOptions> wompiOptions, IProductoService productoService, ITokenUserService tokenUserService, IPagoTransaccionService pagoTransaccionService,
			IConfiguration configuration,
			IFacturacionApiClient facturacionApiClient,
			IClientesServicios clientesServicios)
        {
            _wompiOptions = wompiOptions.Value;
			_productoService = productoService;
            _tokenUserService = tokenUserService;
            _pagoTransaccionService = pagoTransaccionService;
			_configuration = configuration;
			_facturacionApiClient = facturacionApiClient;
			_clientesServicios = clientesServicios;
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

				var evento = root.TryGetProperty("event", out var eventElement)
					? eventElement.GetString()
					: null;

				if (evento != "transaction.updated")
				{
					return Ok(new
					{
						recibido = true,
						ignorado = true,
						mensaje = "Evento no manejado",
						evento
					});
				}
				var checksumHeader = Request.Headers.TryGetValue("X-Event-Checksum", out var headerValue) ? headerValue.ToString() : null;

				var checksumValido = WompiWebhookHelper.ValidarChecksum(
					root,
					_wompiOptions.EventsSecret,
					checksumHeader
				);


				if (!checksumValido)
				{
					Console.WriteLine("Webhook Wompi con checksum inválido.");

					return Unauthorized(new
					{
						mensaje = "Checksum inválido"
					});
				}

				if (!root.TryGetProperty("data", out var dataElement) ||
					!dataElement.TryGetProperty("transaction", out var transactionElement))
				{
					return BadRequest(new
					{
						mensaje = "El evento no contiene data.transaction"
					});
				}

				var transaccionId = transactionElement.TryGetProperty("id", out var idElement)
					? idElement.GetString()
					: null;

				var referencia = transactionElement.TryGetProperty("reference", out var referenceElement)
					? referenceElement.GetString()
					: null;

				var estadoWompi = transactionElement.TryGetProperty("status", out var statusElement)
					? statusElement.GetString()
					: null;

				long? montoCentavos = null;

				if (transactionElement.TryGetProperty("amount_in_cents", out var amountElement) &&
					amountElement.ValueKind == JsonValueKind.Number)
				{
					montoCentavos = amountElement.GetInt64();
				}

				var paymentMethodType = transactionElement.TryGetProperty("payment_method_type", out var paymentMethodElement)
					? paymentMethodElement.GetString()
					: null;

				Console.WriteLine($"Evento: {evento}");
				Console.WriteLine($"Transacción: {transaccionId}");
				Console.WriteLine($"Referencia: {referencia}");
				Console.WriteLine($"Estado Wompi: {estadoWompi}");
				Console.WriteLine($"Monto centavos: {montoCentavos}");
				Console.WriteLine($"Método pago: {paymentMethodType}");

				if (string.IsNullOrWhiteSpace(referencia))
				{
					return BadRequest(new
					{
						mensaje = "La referencia de la transacción no llegó en el webhook"
					});
				}

				if (string.IsNullOrWhiteSpace(estadoWompi))
				{
					return BadRequest(new
					{
						mensaje = "El estado de la transacción no llegó en el webhook"
					});
				}

				var transaccionLocal = await _pagoTransaccionService.ObtenerPorReferenceAsync(referencia);

				if (transaccionLocal == null)
				{
					Console.WriteLine($"No existe transacción local para reference: {referencia}");

					return Ok(new
					{
						recibido = true,
						encontrado = false,
						mensaje = "Transacción local no encontrada",
						referencia
					});
				}

				if (montoCentavos.HasValue &&
					transaccionLocal.AmountInCents != montoCentavos.Value)
				{
					Console.WriteLine($"Monto no coincide. Local: {transaccionLocal.AmountInCents}, Wompi: {montoCentavos}");

					await _pagoTransaccionService.ActualizarEstadoAsync(
						referencia,
						EstadoPagoTransaccion.Error,
						wompiTransactionId: transaccionId,
						wompiStatus: estadoWompi,
						wompiStatusMessage: "El monto recibido en webhook no coincide con la transacción local",
						wompiPaymentMethodType: paymentMethodType,
						webhookJson: body
					);

					return Ok(new
					{
						recibido = true,
						procesado = false,
						mensaje = "Monto no coincide"
					});
				}

				var estadoLocalToWompi = WompiEstadoHelper.MapearEstadoWompi(estadoWompi);

				if (transaccionLocal.Estado != EstadoPagoTransaccion.Aprobada && estadoLocalToWompi == EstadoPagoTransaccion.Aprobada)
				{
					await ProcesarPagoAprobadoAsync(transaccionLocal);
				}

				//
				await _pagoTransaccionService.ActualizarEstadoAsync(
					referencia,
					estadoLocalToWompi,
					wompiTransactionId: transaccionId,
					wompiStatus: estadoWompi,
					wompiStatusMessage: null,
					wompiPaymentMethodType: paymentMethodType,
					webhookJson: body
				);

				return Ok(new
				{
					recibido = true,
					procesado = true,
					evento,
					transaccionId,
					referencia,
					estadoWompi,
					estadoLocalToWompi,
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
			catch (Exception ex)
			{
				Console.WriteLine("Error procesando webhook Wompi:");
				Console.WriteLine(ex.Message);

				return StatusCode(500, new
				{
					mensaje = "Error procesando webhook"
				});
			}
		}

		[HttpPost("crear-checkout")]
        public async Task<IActionResult> CrearCheckout([FromBody] CrearCheckoutRequest request)
		{
            var datosToken = _tokenUserService.ObtenerDatosUsuario(HttpContext);

			if (datosToken == null)
			{
				return Unauthorized(new
				{
					mensaje = "Token inválido o sesión no válida"
				});
			}


			return await CrearCheckoutInternoAsync(request, datosToken.IdCliente, datosToken.Correo);
        }

		[HttpPost("crear-checkout-renovacion")]
		public async Task<IActionResult> CrearCheckoutRenovacion([FromBody] CrearCheckoutRequest request)
		{
			var datosRenewToken = ObtenerDatosRenovacionDesdeToken(HttpContext);

			if (datosRenewToken == null)
			{
				return Unauthorized(new
				{
					codigoError = "RENOVACION_TOKEN_INVALIDO",
					mensaje = "Token de renovación inválido o expirado"
				});
			}

			return await CrearCheckoutInternoAsync(request, datosRenewToken.Value.IdCliente, datosRenewToken.Value.Correo);
		}

		[HttpGet("estado-por-referencia/{reference}")]
		public async Task<IActionResult> EstadoPorReferencia(string reference)
		{
			var transaccion = await _pagoTransaccionService.ObtenerPorReferenceAsync(reference);

			if (transaccion == null)
			{
				return NotFound(new
				{
					mensaje = "Transacción no encontrada"
				});
			}

			return Ok(new
			{
				reference = transaccion.Reference,
				estado = transaccion.Estado,
				wompiStatus = transaccion.WompiStatus,
				wompiTransactionId = transaccion.WompiTransactionId,
				amountInCents = transaccion.AmountInCents,
				currency = transaccion.Currency,
				fechaCreacion = transaccion.FechaCreacion,
				fechaActualizacion = transaccion.FechaActualizacion,
				fechaPago = transaccion.FechaPago
			});
		}

		[HttpGet("estado-por-transaccion/{idTransaccion}")]
		public async Task<IActionResult> EstadoPorTransaccion(string idTransaccion)
		{
			var transaccion = await _pagoTransaccionService.ObtenerPorWompiTransactionIdAsync(idTransaccion);

			if (transaccion == null)
			{
				return NotFound(new
				{
					mensaje = "Transacción no encontrada"
				});
			}

			return Ok(new
			{
				reference = transaccion.Reference,
				estado = transaccion.Estado,
				wompiStatus = transaccion.WompiStatus,
				wompiTransactionId = transaccion.WompiTransactionId,
				amountInCents = transaccion.AmountInCents,
				currency = transaccion.Currency,
				fechaCreacion = transaccion.FechaCreacion,
				fechaActualizacion = transaccion.FechaActualizacion,
				fechaPago = transaccion.FechaPago
			});
		}


		private async Task ProcesarPagoAprobadoAsync(PagoTransaccion transaccion, CancellationToken ct = default)
		{
			if (transaccion == null) return;

			var ahora = DateTime.Now;

			var cliente = await _clientesServicios.ConsultarPorId(transaccion.IdCliente);

			if (cliente == null || cliente.billingTenantId == null)
				throw new Exception("La transacción aprobada no tiene TenantCode.");

			var usaDataico = (transaccion.TipoBeneficio == "FACTURAS" || transaccion.TipoBeneficio == "TODAS");

			if (usaDataico)
			{
				if (!transaccion.CantidadFacturas.HasValue || transaccion.CantidadFacturas.Value <= 0)
					throw new Exception("La transacción aprobada no tiene cantidad de facturas válida.");

				var facturacionVencida =
					cliente.facturacionActivoHasta.HasValue &&
					cliente.facturacionActivoHasta.Value.Date < ahora.Date;

				var topUpRequest = new TopUpRequest
				{
					TenantCode = cliente.billingTenantId.ToString(),
					OperationCount = transaccion.CantidadFacturas.Value,
					Amount = transaccion.Precio,
					CreatedBy = $"WOMPI:{transaccion.Reference}",

					Comments = facturacionVencida
						? $"Recarga automática por pago aprobado. Cliente estaba vencido. Se pierde saldo anterior. Producto: {transaccion.NombreProducto}. Ref: {transaccion.Reference}"
						: $"Recarga automática por pago aprobado. Producto: {transaccion.NombreProducto}. Ref: {transaccion.Reference}",

					ResetBalanceBeforeTopUp = facturacionVencida,

					LostBalanceExpiredAt = facturacionVencida
						? cliente.facturacionActivoHasta
						: null
				};

				var resultado = await _facturacionApiClient.RecargarAsync(topUpRequest, ct);

				var diasAcceso = transaccion.DiasAcceso ?? 0;

				var fechaBase =
					cliente.facturacionActivoHasta.HasValue &&
					cliente.facturacionActivoHasta.Value > ahora
						? cliente.facturacionActivoHasta.Value
						: ahora;

				cliente.facturacionActivoHasta = fechaBase.AddDays(diasAcceso);
			}

			if (transaccion.TipoBeneficio == "SERVICIO" || transaccion.TipoBeneficio == "TODAS")
			{
				var diasAcceso = transaccion.DiasAcceso ?? 0;

				var fechaBase =
					cliente.rydentWebActivoHasta.HasValue &&
					cliente.rydentWebActivoHasta.Value > ahora
						? cliente.rydentWebActivoHasta.Value
						: ahora;

				cliente.rydentWebActivoHasta = fechaBase.AddDays(diasAcceso);
			}

			if (usaDataico)
			{
				cliente.usaDataico = true;
			}

			await _clientesServicios.Editar(cliente.idCliente, cliente);
		}

		private async Task<IActionResult> CrearCheckoutInternoAsync(CrearCheckoutRequest request, long idCliente, string correo)
		{
			var productos = await _productoService.ObtenerProductoPorIdAsync(request.idProducto);

			if (productos == null)
			{
				return BadRequest(new
				{
					mensaje = "Paquete no válido"
				});
			}

			if (productos.Beneficios == null || productos.Beneficios.Count == 0)
			{
				return BadRequest(new
				{
					mensaje = "El paquete no tiene beneficios configurados"
				});
			}

			var beneficios = productos.Beneficios[0];

			var reference = $"PBI003-{productos.IdProducto}-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
			var amountInCents = (long)Math.Round(
				productos.Precio * 100,
				0,
				MidpointRounding.AwayFromZero
			);

			if (amountInCents < 0)
			{
				return BadRequest(new
				{
					mensaje = "El producto no tiene un precio válido"
				});
			}

			var currency = _wompiOptions.Currency;
			var integritySecret = _wompiOptions.IntegritySecret;

			var cadenaFirma = $"{reference}{amountInCents}{currency}{integritySecret}";
			var signature = WompiWebhookHelper.Sha256(cadenaFirma);

			var transaccion = new PagoTransaccion
			{
				Reference = reference,
				IdCliente = idCliente,
				CorreoCliente = correo,
				IdProducto = productos.IdProducto,
				CodigoProducto = productos.Codigo,
				NombreProducto = productos.Nombre,
				IdBeneficio = beneficios.IdBeneficio,
				TipoBeneficio = beneficios.TipoBeneficio,
				CantidadFacturas = beneficios.CantidadFacturas,
				DiasAcceso = beneficios.DiasAcceso,
				Precio = productos.Precio,
				AmountInCents = amountInCents,
				Currency = currency,
				Estado = EstadoPagoTransaccion.Iniciada,
				PublicKey = _wompiOptions.PublicKey,
				RedirectUrl = _wompiOptions.RedirectUrl,
				Signature = signature,
				FechaCreacion = DateTime.Now,
				FechaActualizacion = DateTime.Now,
				RequestJson = JsonSerializer.Serialize(request)
			};

			await _pagoTransaccionService.CrearAsync(transaccion);

			return Ok(new
			{
				publicKey = _wompiOptions.PublicKey,
				reference,
				amountInCents,
				currency,
				signature,
				redirectUrl = _wompiOptions.RedirectUrl,
				productos = new
				{
					productos.IdProducto,
					productos.Nombre,
					beneficio = new
					{
						beneficios.IdBeneficio,
						beneficios.IdProducto,
						beneficios.TipoBeneficio,
						beneficios.CantidadFacturas,
						beneficios.DiasAcceso,
						beneficios.Activo
					},
					productos.Precio
				}
			});
		}

		private (long IdUsuario, long IdCliente, string Correo)? ObtenerDatosRenovacionDesdeToken(HttpContext httpContext)
		{
			try
			{
				var auth = httpContext.Request.Headers["Authorization"].ToString();

				if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer "))
					return null;

				var token = auth.Substring("Bearer ".Length).Trim();
				var jwtSecret = _configuration["JWT_SECRET"] ?? "";
				var issuer = _configuration["Jwt:Issuer"] ?? "";

				if (string.IsNullOrWhiteSpace(jwtSecret))
					return null;

				var tokenHandler = new JwtSecurityTokenHandler();
				var key = Encoding.ASCII.GetBytes(jwtSecret);

				var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
					ValidIssuer = issuer,
					ValidateAudience = true,
					ValidAudience = jwtSecret,
					ValidateLifetime = true,
					ClockSkew = TimeSpan.FromSeconds(15)
				}, out _);

				var purpose = principal.Claims.FirstOrDefault(x => x.Type == "purpose")?.Value;
				if (!string.Equals(purpose, PurposeRenewCheckout, StringComparison.Ordinal))
					return null;

				var idStr = principal.Claims.FirstOrDefault(x => x.Type == "id")?.Value;
				var idClienteStr = principal.Claims.FirstOrDefault(x => x.Type == "idCliente")?.Value;
				var correo = principal.Claims.FirstOrDefault(x => x.Type == "correo")?.Value ?? "";

				if (!long.TryParse(idStr, out var idUsuario))
					return null;

				if (!long.TryParse(idClienteStr, out var idCliente))
					return null;

				return (idUsuario, idCliente, correo);
			}
			catch
			{
				return null;
			}
		}

	}
}