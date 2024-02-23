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

        public async Task RespuestaObtenerPin(string clienteId, string respuestaPin)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerPin", clienteId, respuestaPin);
        }

        public async Task ObtenerDoctor(string clienteId, string idDoctor)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDoctor", Context.ConnectionId, idDoctor);
        }

        public async Task RespuestaObtenerDoctor(string clienteId, string respuestaObtenerDoctor)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDoctor", clienteId, respuestaObtenerDoctor);
        }

        //----------------ConsultarPorDiaYPorUnidad en Base de Datos Rydent Local----------------
        //ClienteId: Identificador del cliente local que tiene el worked por medio del cual se realizara la consulta en la bd rydent local
        //Este dato de clienteId queda guardado en sedes conectadas

        //Cuando se invoca es que se ejecuta estas funciones del servidor SR
        public async Task ConsultarPorDiaYPorUnidad(string clienteId, int silla, DateTime fecha)
        {
            //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
            await Clients.Client(clienteId).SendAsync("ConsultarPorDiaYPorUnidad", Context.ConnectionId, silla, fecha);
        }


        //En este caso ClienteId es el Identificador del cliente angular que previamente enviamos al invocar ConsultarPorDiaYPorUnidad el Context.ConnectionId 
        public async Task RespuestaConsultarPorDiaYPorUnidad(string clienteId, object respuesta)
        {
            //respuesta es el objeto que se obtiene de la consulta en la bd rydent local, debe devolver una instancia del objeto TCitas y un listado del objeto TDetalleCitas
            string jsonString = JsonSerializer.Serialize(respuesta);
            await Clients.Client(clienteId).SendAsync("RespuestaConsultarPorDiaYPorUnidad", clienteId, jsonString);
        }

        

        public async Task BuscarPaciente(string clienteId, string tipoBuqueda, string valorDeBusqueda)
        {
            await Clients.Client(clienteId).SendAsync("BuscarPaciente", Context.ConnectionId, tipoBuqueda, valorDeBusqueda);
        }

        public async Task RespuestaBuscarPaciente(string clienteId, string listPacientes)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaBuscarPaciente", clienteId, listPacientes);
        }

        public async Task ObtenerDatosPersonalesCompletosPaciente(string clienteId, string idAnanesis)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDatosPersonalesCompletosPaciente", Context.ConnectionId, idAnanesis);
        }

        public async Task RespuestaObtenerDatosPersonalesCompletosPaciente(string clienteId, string paciente)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosPersonalesCompletosPaciente", clienteId, paciente);
        }

        public async Task ObtenerAntecedentesPaciente(string clienteId, string idAnanesis)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerAntecedentesPaciente", Context.ConnectionId, idAnanesis);
        }

        public async Task RespuestaObtenerAntecedentesPaciente(string clienteId, string paciente)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerAntecedentesPaciente", clienteId, paciente);
        }


        public async Task ObtenerDatosEvolucion(string clienteId, string idAnanesis)
        {
            await Clients.Client(clienteId).SendAsync("ObtenerDatosEvolucion", Context.ConnectionId, idAnanesis);
        }

        public async Task RespuestaObtenerDatosEvolucion(string clienteId, string evolucion)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosEvolucion", clienteId, evolucion);
        }
    }
}
