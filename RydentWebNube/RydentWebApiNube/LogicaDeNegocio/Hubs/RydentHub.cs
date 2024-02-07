using Microsoft.AspNetCore.SignalR;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using System;
using System.Text.Json;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public class RydentHub : Hub
    {
        private readonly ISedesServicios _sedesServicios;
        private readonly ISedesConectadasServicios _sedesconectadasServicios;
        public RydentHub(ISedesServicios sedesServicios, ISedesConectadasServicios sedesconectadasServicios)
        {
            _sedesServicios = sedesServicios;
            _sedesconectadasServicios = sedesconectadasServicios;
        }
        //public async Task SendMessage(string user, string message)
        //{
        //    await Clients.All.SendAsync("ReceiveMessage", user, message);
        //}

        public async Task RegistrarEquipo(string idActualSignalR, string identificadorLocal)
        {
            var sede= await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
            if (sede.idSede > 0)
            {
                var sedesConectadas = await _sedesconectadasServicios.ConsultarPorSedeConEstadoActivo(sede.idSede);
                foreach (var item in sedesConectadas)
                {
                    if (item.idActualSignalR != idActualSignalR)
                    {
                        item.activo = false;
                        await _sedesconectadasServicios.Editar(item.idSedeConectada, item);
                    }
                }
                await _sedesconectadasServicios.Agregar(new SedesConectadas { 
                    idCliente = sede.idCliente,
                    idSede = sede.idSede,
                    idActualSignalR = idActualSignalR,
                    fechaUltimoAcceso = DateTime.Now,
                    activo = true
                });
                return;
            }
            await Clients.All.SendAsync("RegistrarEquipo", idActualSignalR, identificadorLocal);
        }

        public async Task ObtenerPin(string clienteId, string pin)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerPin", Context.ConnectionId, pin);
        }

        public async Task RespuestaObtenerPin(string clienteId, object equipoId)
        {
            string jsonString = JsonSerializer.Serialize(equipoId);
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerPin", clienteId, jsonString);
        }

        public async Task ObtenerDoctor(string clienteId, int idDoctor)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDoctor", Context.ConnectionId, idDoctor);
        }

        public async Task RespuestaObtenerDoctor(string clienteId, object doctor)
        {
            string jsonString = JsonSerializer.Serialize(doctor);
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDoctor", clienteId, jsonString);
        }

        public async Task BuscarPaciente(string clienteId, int tipoBuqueda, string valorDeBusqueda)
        {
            await Clients.Client(clienteId).SendAsync("BuscarPaciente", Context.ConnectionId, tipoBuqueda, valorDeBusqueda);
        }

        public async Task RespuestaBuscarPaciente(string clienteId, List<object> listPacientes)
        {
            string jsonString = JsonSerializer.Serialize(listPacientes);
            await Clients.Client(clienteId).SendAsync("RespuestaBuscarPaciente", clienteId, jsonString);
        }

        public async Task ObtenerDatosPersonalesCompletosPaciente(string clienteId, int idAnanesis)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDatosCompletosPaciente", Context.ConnectionId, idAnanesis);
        }

        public async Task RespuestaObtenerDatosPersonalesCompletosPaciente(string clienteId, object paciente)
        {
            string jsonString = JsonSerializer.Serialize(paciente);
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosCompletosPaciente", clienteId, jsonString);
        }

        public async Task ObtenerDatosEvolucionPaciente(string clienteId, int idAnanesis)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDatosEvolucion", Context.ConnectionId, idAnanesis);
        }

        public async Task RespuestaObtenerDatosEvolucionPaciente(string clienteId, object evolucion)
        {
            string jsonString = JsonSerializer.Serialize(evolucion);
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosEvolucion", clienteId, jsonString);
        }
    }
}
