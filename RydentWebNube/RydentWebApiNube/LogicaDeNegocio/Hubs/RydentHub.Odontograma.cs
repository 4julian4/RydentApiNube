using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public partial class RydentHub
    {
        public async Task ObtenerOdontogramaInicial(
            long sedeId,
            int idTratamiento,
            DateTime fecha)
        {
            try
            {
                await EnviarAccionOdontogramaPorSedeAsync(
                    sedeId,
                    "ODONTOGRAMA_OBTENER_INICIAL",
                    idTratamiento,
                    fecha);
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerOdontogramaInicial: {ex.Message}");
            }
        }

        public async Task ObtenerOdontogramaActual(
            long sedeId,
            int idTratamiento,
            DateTime fecha)
        {
            try
            {
                await EnviarAccionOdontogramaPorSedeAsync(
                    sedeId,
                    "ODONTOGRAMA_OBTENER_ACTUAL",
                    idTratamiento,
                    fecha);
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerOdontogramaActual: {ex.Message}");
            }
        }

        private async Task EnviarAccionOdontogramaPorSedeAsync(
            long sedeId,
            string accion,
            int idTratamiento,
            DateTime fecha)
        {
            var returnId = Context.ConnectionId;

            var workerConnId = await ResolveWorkerConnIdBySedeAsync(sedeId);

            if (string.IsNullOrWhiteSpace(workerConnId))
            {
                await Clients.Client(returnId)
                    .SendAsync("ErrorConexion", returnId, "No se encontró conexión activa");

                return;
            }

            var parametros = new Dictionary<string, object>
            {
                { "idTratamiento", idTratamiento },
                { "fecha", fecha }
            };

            var lote = _gestorAccionesWorker.GenerarLote(
                accion,
                parametros);

            lote.IdPeticion = returnId;
            lote.ResponderPorApiHttp = false;

            var loteJson = JsonSerializer.Serialize(lote);

            await Clients.Client(workerConnId).SendAsync(
                "EjecutarLoteSQL",
                returnId,
                loteJson
            );
        }

        public async Task RespuestaAccionWorker(
            string clienteId,
            string accionOriginal,
            string respuestaCrudaWorker)
        {
            try
            {
                if (string.Equals(accionOriginal, "ERROR", StringComparison.OrdinalIgnoreCase))
                {
                    await Clients.Client(clienteId).SendAsync(
                        "ErrorConexion",
                        clienteId,
                        respuestaCrudaWorker
                    );

                    return;
                }

                var respuestaAngular = _gestorAccionesWorker.TraducirParaAngular(
                    accionOriginal,
                    respuestaCrudaWorker);

                await Clients.Client(clienteId).SendAsync(
                    "RespuestaAccionWorker",
                    clienteId,
                    accionOriginal,
                    respuestaAngular
                );
            }
            catch (Exception ex)
            {
                await Clients.Client(clienteId).SendAsync(
                    "ErrorConexion",
                    clienteId,
                    ex.Message
                );

                Console.Error.WriteLine($"Error en RespuestaAccionWorker: {ex.Message}");
            }
        }

        public async Task RegistrarWorkerManual(long idSede, string identificadorLocal)
        {
            try
            {
                _presence.Upsert(
                    idSede,
                    identificadorLocal,
                    Context.ConnectionId
                );

                await Clients.Client(Context.ConnectionId).SendAsync(
                    "WorkerManualRegistrado",
                    idSede,
                    identificadorLocal,
                    Context.ConnectionId
                );
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync(
                    "ErrorConexion",
                    Context.ConnectionId,
                    ex.Message
                );

                Console.Error.WriteLine($"Error al RegistrarWorkerManual: {ex.Message}");
            }
        }

        public async Task ObtenerOdontogramaInicialManual(
            string identificadorLocal,
            int idTratamiento,
            DateTime fecha)
        {
            try
            {
                await EnviarAccionOdontogramaPorIdentificadorAsync(
                    identificadorLocal,
                    "ODONTOGRAMA_OBTENER_INICIAL",
                    idTratamiento,
                    fecha);
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerOdontogramaInicialManual: {ex.Message}");
            }
        }

        public async Task ObtenerOdontogramaActualManual(
            string identificadorLocal,
            int idTratamiento,
            DateTime fecha)
        {
            try
            {
                await EnviarAccionOdontogramaPorIdentificadorAsync(
                    identificadorLocal,
                    "ODONTOGRAMA_OBTENER_ACTUAL",
                    idTratamiento,
                    fecha);
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", Context.ConnectionId, ex.Message);

                Console.Error.WriteLine($"Error al ObtenerOdontogramaActualManual: {ex.Message}");
            }
        }

        private async Task EnviarAccionOdontogramaPorIdentificadorAsync(
            string identificadorLocal,
            string accion,
            int idTratamiento,
            DateTime fecha)
        {
            var returnId = Context.ConnectionId;

            if (!_presence.TryGetActiveConnectionByIdentificadorLocal(
                    identificadorLocal,
                    out var workerConnId))
            {
                await Clients.Client(returnId).SendAsync(
                    "ErrorConexion",
                    returnId,
                    $"No se encontró worker activo con identificadorLocal: {identificadorLocal}"
                );

                return;
            }

            var parametros = new Dictionary<string, object>
    {
        { "idTratamiento", idTratamiento },
        { "fecha", fecha }
    };

            var lote = _gestorAccionesWorker.GenerarLote(
                accion,
                parametros);

            lote.IdPeticion = returnId;
            lote.ResponderPorApiHttp = false;

            var loteJson = JsonSerializer.Serialize(lote);

            await Clients.Client(workerConnId).SendAsync(
                "EjecutarLoteSQL",
                returnId,
                loteJson
            );
        }
    }

}