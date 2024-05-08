using Microsoft.AspNetCore.SignalR;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using System;
using System.ComponentModel.DataAnnotations;
using System.Net.NetworkInformation;
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
        public override async Task OnDisconnectedAsync(Exception exception)
        {
            // Lógica para manejar la desconexión del cliente
            // Puedes acceder a la información del cliente usando Context.ConnectionId o Context.User
            var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(Context.ConnectionId);
            if (objSedesConectada.idSedeConectada > 0 && (objSedesConectada.activo?? false))
            {
                objSedesConectada.activo = false;
                await _sedesconectadasServicios.Editar(objSedesConectada.idSedeConectada, objSedesConectada);
            }
            // Puedes realizar otras acciones, como notificar a otros clientes sobre la desconexión, actualizar el estado del servidor, etc.

            await base.OnDisconnectedAsync(exception);
        }
        private async Task<string> ValidarIdActualSignalR(string idActualSignalR)
        { 
            var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(idActualSignalR);
            if (objSedesConectada.idCliente > 0)
            {
                if (objSedesConectada.activo ?? false)
                {
                    return objSedesConectada.idActualSignalR ?? "";
                }
                else
                {
                    var objSedeConectadaActiva = await _sedesconectadasServicios.ConsultarSedesConectadasActivasPorCliente(objSedesConectada.idCliente ?? 0);
                    return objSedeConectadaActiva.Count > 0 ? (objSedeConectadaActiva[0].idActualSignalR ?? "") : "";
                }
            }
            return "";
        }

        public async Task ErrorConexion(string clienteId, string errorConexion)
        {
            await Clients.Client(clienteId).SendAsync("ErrorConexion", clienteId, errorConexion);
        }

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
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if(idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(idActualSignalR).SendAsync("ObtenerPin", Context.ConnectionId, pin);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
        }

        public async Task RespuestaObtenerPin(string clienteId, string respuestaPin)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerPin", clienteId, respuestaPin);
        }



        public async Task ObtenerDoctor(string clienteId, string idDoctor)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerDoctor", Context.ConnectionId, idDoctor);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
        }

        public async Task RespuestaObtenerDoctor(string clienteId, string respuestaObtenerDoctor)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDoctor", clienteId, respuestaObtenerDoctor);
        }

        //----------------ConsultarPorDiaYPorUnidad en Base de Datos Rydent Local----------------
        //ClienteId: Identificador del cliente local que tiene el worked por medio del cual se realizara la consulta en la bd rydent local
        //Este dato de clienteId queda guardado en sedes conectadas

        //Cuando se invoca es que se ejecuta estas funciones del servidor SR

        public async Task AgendarCita(string clienteId, string modelocrearcita)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("AgendarCita", Context.ConnectionId, modelocrearcita);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaAgendarCita(string clienteId, string modelocrearcita)
        {
            //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
            await Clients.Client(clienteId).SendAsync("RespuestaAgendarCita", Context.ConnectionId, modelocrearcita);
        }


        
        
        public async Task BuscarPaciente(string clienteId, string tipoBuqueda, string valorDeBusqueda)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("BuscarPaciente", Context.ConnectionId, tipoBuqueda, valorDeBusqueda);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaBuscarPaciente(string clienteId, string listPacientes)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaBuscarPaciente", clienteId, listPacientes);
        }
        
        public async Task BuscarCitasPacienteAgenda(string clienteId, string valorBuscarAgenda)
        {            
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("BuscarCitasPacienteAgenda", Context.ConnectionId, valorBuscarAgenda);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }
        
        public async Task RespuestaBuscarCitasPacienteAgenda(string clienteId, string listPacientes)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaBuscarCitasPacienteAgenda", clienteId, listPacientes);
        }



        public async Task ObtenerDatosPersonalesCompletosPaciente(string clienteId, string idAnanesis)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerDatosPersonalesCompletosPaciente", Context.ConnectionId, idAnanesis);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaObtenerDatosPersonalesCompletosPaciente(string clienteId, string paciente)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosPersonalesCompletosPaciente", clienteId, paciente);
        }

        public async Task ObtenerAntecedentesPaciente(string clienteId, string idAnanesis)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerAntecedentesPaciente", Context.ConnectionId, idAnanesis);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaObtenerAntecedentesPaciente(string clienteId, string paciente)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerAntecedentesPaciente", clienteId, paciente);
        }


        public async Task ObtenerDatosEvolucion(string clienteId, string idAnanesis)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerDatosEvolucion", Context.ConnectionId, idAnanesis);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaObtenerDatosEvolucion(string clienteId, string evolucion)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosEvolucion", clienteId, evolucion);
        }

        public async Task GuardarDatosEvolucion(string clienteId, string evolucion)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("GuardarDatosEvolucion", Context.ConnectionId, evolucion);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaGuardarDatosEvolucion(string clienteId, string respuesta)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaGuardarDatosEvolucion", clienteId, respuesta);
        }

        public async Task GuardarDatosRips(string clienteId, string datosRips)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("GuardarDatosRips", Context.ConnectionId, datosRips);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }

        }

        public async Task RespuestaGuardarDatosRips(string clienteId, bool respuesta)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaGuardarDatosRips", clienteId, respuesta);
        }

        public async Task ObtenerDatosAdministrativos(string clienteId, DateTime fechaInicio, DateTime fechaFin)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerDatosAdministrativos", Context.ConnectionId, fechaInicio, fechaFin);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }

        }

        public async Task RespuestaObtenerDatosAdministrativos(string clienteId, string respuesta)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosAdministrativos", clienteId, respuesta);
        }
        public async Task ObtenerCodigosEps(string clienteId)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerCodigosEps", Context.ConnectionId);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
            
        }

        public async Task RespuestaObtenerCodigosEps(string clienteId, string listadoeps)
        {
            await Clients.Client(clienteId).SendAsync("RespuestaObtenerCodigosEps", clienteId, listadoeps);
        }


        public async Task ObtenerConsultaPorDiaYPorUnidad(string clienteId, string silla, DateTime fecha)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ObtenerConsultaPorDiaYPorUnidad", Context.ConnectionId, silla, fecha);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
        }
        //En este caso ClienteId es el Identificador del cliente angular que previamente enviamos al invocar ConsultarPorDiaYPorUnidad el Context.ConnectionId 
        public async Task RespuestaObtenerConsultaPorDiaYPorUnidad(string clienteId, string respuesta)
        {
            //respuesta es el objeto que se obtiene de la consulta en la bd rydent local, debe devolver una instancia del objeto TCitas y un listado del objeto TDetalleCitas

            await Clients.Client(clienteId).SendAsync("RespuestaObtenerConsultaPorDiaYPorUnidad", clienteId, respuesta);
        }

        public async Task RealizarAccionesEnCitaAgendada(string clienteId, string modelorealizaraccionesenlacitaagendada)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("RealizarAccionesEnCitaAgendada", Context.ConnectionId, modelorealizaraccionesenlacitaagendada);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
        }

        public async Task RespuestaRealizarAccionesEnCitaAgendada(string clienteId, string modelorealizaraccionesenlacitaagendada)
        {
            //respuesta es el objeto que se obtiene de la consulta en la bd rydent local, debe devolver una instancia del objeto TCitas y un listado del objeto TDetalleCitas

            await Clients.Client(clienteId).SendAsync("RespuestaRealizarAccionesEnCitaAgendada", clienteId, modelorealizaraccionesenlacitaagendada);
        }

        public async Task ConsultarEstadoCuenta(string clienteId, string modeloDatosParaConsultarEstadoCuenta)
        {
            string idActualSignalR = await ValidarIdActualSignalR(clienteId);
            if (idActualSignalR != "")
            {
                try
                {
                    await Clients.Client(clienteId).SendAsync("ConsultarEstadoCuenta", Context.ConnectionId, modeloDatosParaConsultarEstadoCuenta);
                }
                catch (Exception e)
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, e.Message);
                }
            }
            else
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
            }
        }

        public async Task RespuestaConsultarEstadoCuenta(string clienteId, string respuestaConsultarEstadoCuenta)
        {
            
            await Clients.Client(clienteId).SendAsync("RespuestaConsultarEstadoCuenta", clienteId, respuestaConsultarEstadoCuenta);
        }


    }
}
