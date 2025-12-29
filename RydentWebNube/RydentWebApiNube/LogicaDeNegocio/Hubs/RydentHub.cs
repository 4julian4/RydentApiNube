using Microsoft.AspNetCore.SignalR;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.LogicaDeNegocio.Servicios;
using RydentWebApiNube.Models.Google;
using System;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using RydentWebApiNube.Models.MSN;
using Microsoft.AspNetCore.Mvc;

namespace RydentWebApiNube.LogicaDeNegocio.Hubs
{
    public class RydentHub : Hub
    {
        private readonly ISedesServicios _sedesServicios;
        //private readonly IConfiguration configuration;
        private readonly ISedesConectadasServicios _sedesconectadasServicios;
        private static readonly HttpClient httpClient = new HttpClient();
        //private readonly HttpClient httpClient;
        private readonly IUsuariosServicios iUsuariosServicios;
        private readonly IConfiguration configuration;
        private readonly string GoogleRedirectURI;
        private readonly string GoogleClientId;
        private readonly string GoogleSecret;
        private readonly string GoogleTokenEndPoint;
        private readonly string GoogleAPI_EndPoint;
        private readonly string TokenEndPoint;
        private readonly string JWT_SECRET;
        private readonly string JWT;
        private readonly string RedirectURI;
        private readonly string ClientId;
        private readonly string Secret;
        private readonly string Scope;
        private readonly string API_EndPoint;



        public RydentHub(
            ISedesServicios sedesServicios,
            ISedesConectadasServicios sedesconectadasServicios,
            IConfiguration configuration,
            IUsuariosServicios iUsuariosServicios
            )
        {
            _sedesServicios = sedesServicios;
            _sedesconectadasServicios = sedesconectadasServicios;
            this.iUsuariosServicios = iUsuariosServicios;
            this.GoogleTokenEndPoint = configuration["OAuthGoogle:TokenEndPoint"] ?? "";
            this.GoogleRedirectURI = configuration["OAuthGoogle:RedirectURI"] ?? "";
            this.GoogleClientId = configuration["OAUTH2_GOOGLE_CLIENTID"] ?? "";
            this.GoogleSecret = configuration["OAUTH2_GOOGLE_SECRET"] ?? "";
            this.GoogleAPI_EndPoint = configuration["OAuthGoogle:API_EndPoint"] ?? "";
            this.TokenEndPoint = configuration["OAuth:TokenEndPoint"] ?? "";
            this.JWT_SECRET = configuration["JWT_SECRET"] ?? "";
            this.JWT = configuration["Jwt:Issuer"] ?? "";
            this.RedirectURI = configuration["OAuth:RedirectURI"] ?? "";
            this.ClientId = configuration["OAUTH2_AZURE_CLIENTID"] ?? "";
            this.Secret = configuration["OAUTH2_AZURE_SECRET"] ?? "";
            this.Scope = configuration["OAuth:Scope"] ?? "";
            this.API_EndPoint = configuration["OAuth:API_EndPoint"] ?? "";
        }



        //autenticar google
        public async Task PostLoginCallbackGoogle(string clienteId, string code, string state)
        {
            // Diccionario con parámetros de autenticación
            string grant_type = "authorization_code";
            string stringURI = new Uri(this.GoogleRedirectURI).ToString();
            var BodyData = new Dictionary<string, string>
            {
                { "code", code },
                { "client_id", this.GoogleClientId },
                { "client_secret", this.GoogleSecret },
                { "redirect_uri", stringURI },
                { "grant_type", grant_type }
            };


            var body = new FormUrlEncodedContent(BodyData);
            var response = await httpClient.PostAsync(this.GoogleTokenEndPoint, body).ConfigureAwait(false);
            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);

            var result = new ExpandoObject() as IDictionary<string, Object>;
            result["respuesta"] = "";
            result["autenticado"] = false;

            if (response.IsSuccessStatusCode)
            {
                var accessToken = jsonContent.GetProperty("access_token").GetString();
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                string googleApiUrl = this.GoogleAPI_EndPoint + accessToken;

                var googleResponse = await httpClient.GetAsync(googleApiUrl).ConfigureAwait(false);
                if (googleResponse.IsSuccessStatusCode)
                {
                    var googleData = await googleResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var googleUser = JsonSerializer.Deserialize<UsuarioGoogle>(googleData);

                    if (!string.IsNullOrEmpty(googleUser?.email))
                    {
                        var usuario = await iUsuariosServicios.ConsultarPorCorreo(googleUser.email).ConfigureAwait(false);
                        var jwtToken = generateJwtToken(usuario);
                        result["respuesta"] = jwtToken;
                        result["autenticado"] = true;
                    }
                }
            }

            // Emitir respuesta de autenticación a cliente específico
            string jsonResult = JsonSerializer.Serialize(result); // Convertir a string
            await Clients.Caller.SendAsync("RespuestaPostLoginCallbackGoogle", clienteId, jsonResult);
        }

        public async Task PostLoginCallback(string clienteId, string code, string state)
        {
            // Diccionario con parámetros de autenticación
            string grant_type = "authorization_code";

            // Datos del cuerpo de la solicitud para obtener el token
            var BodyData = new Dictionary<string, string>
            {
                { "grant_type", grant_type },
                { "code", code },
                { "Redirect_uri", this.RedirectURI },
                { "client_id", this.ClientId },
                { "client_secret", this.Secret },
                { "scope", this.Scope }
            };

            // Enviar solicitud para obtener el token
            var body = new FormUrlEncodedContent(BodyData);
            var response = await httpClient.PostAsync(this.TokenEndPoint, body).ConfigureAwait(false);
            var status = $"{(int)response.StatusCode} {response.ReasonPhrase}";

            // Deserializar respuesta en JSON
            var jsonContent = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
            var prettyJson = JsonSerializer.Serialize(jsonContent, new JsonSerializerOptions { WriteIndented = true });

            // Extraer el token de acceso
            var accessToken = jsonContent.GetProperty("access_token").GetString();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Realizar la solicitud autenticada para obtener información del usuario
            var response1 = await httpClient.GetAsync(API_EndPoint).ConfigureAwait(false);

            // Inicializar el objeto de resultado para el cliente
            var result = new ExpandoObject() as IDictionary<string, Object>;
            result["respuesta"] = "";
            result["autenticado"] = false;

            // Verificar si la solicitud fue exitosa
            if (response1.IsSuccessStatusCode)
            {
                var usrMSNAzure = await response1.Content.ReadAsStringAsync().ConfigureAwait(false);
                var jsUsuarioMSN = JsonSerializer.Deserialize<UsuarioMSN>(usrMSNAzure);

                status = $"{(int)response1.StatusCode} {response1.ReasonPhrase}";

                // Verificar si el usuario fue encontrado
                if (!string.IsNullOrEmpty(jsUsuarioMSN?.mail))
                {
                    var usuario = await iUsuariosServicios.ConsultarPorCorreo(jsUsuarioMSN.mail).ConfigureAwait(false);
                    var respuesta = generateJwtToken(usuario);
                    result["respuesta"] = respuesta;
                    result["autenticado"] = true;
                }
            }

            // Serializar el resultado a JSON para enviarlo al cliente
            string jsonResult = JsonSerializer.Serialize(result);

            // Emitir respuesta de autenticación a cliente específico
            await Clients.Caller.SendAsync("RespuestaPostLoginCallback", clienteId, jsonResult);
        }



        private string generateJwtToken(Usuarios user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(this.JWT_SECRET);
            var lstClaims = new List<Claim>
            {
                new Claim("id", user.idUsuario.ToString()),
                new Claim("idCliente", user.idCliente.ToString()),
                new Claim("correo", user.correoUsuario.ToString())
            };
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(lstClaims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = this.JWT ?? "",
                Audience = this.JWT_SECRET ?? "",
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(Context.ConnectionId);
                if (objSedesConectada?.idSedeConectada > 0 && (objSedesConectada.activo ?? false))
                {
                    objSedesConectada.activo = false;
                    await _sedesconectadasServicios.Editar(objSedesConectada.idSedeConectada, objSedesConectada);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al manejar la desconexión: {ex.Message}");
            }
            await base.OnDisconnectedAsync(exception);
        }
        private async Task<string> ValidarIdActualSignalR(string idActualSignalR)
        {
            try
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
            }
            catch (Exception ex)
            {
                // Manejo de errores en la validación de ID
                Console.Error.WriteLine($"Error al validar ID actual de SignalR: {ex.Message}");
            }
            return "";
        }

        public async Task ErrorConexion(string clienteId, string errorConexion)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("ErrorConexion", clienteId, errorConexion);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la comunicación con el cliente
                Console.Error.WriteLine($"Error al enviar ErrorConexion: {ex.Message}");
            }
        }


        // Método que verifica si el dispositivo ya está registrado
        public async Task<bool> IsDeviceRegistered(string idActualSignalR)
        {
            try
            {
                // Consultar si ya existe un dispositivo con este SignalR ID activo
                var objSedesConectada = await _sedesconectadasServicios.ConsultarPorIdSignalR(idActualSignalR);
                return objSedesConectada != null && (objSedesConectada.activo ?? false);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la consulta
                Console.Error.WriteLine($"Error al verificar si el dispositivo está registrado: {ex.Message}");
                return false; // Si ocurre un error, considera que no está registrado
            }
        }

        // Método para registrar un dispositivo
        public async Task RegistrarEquipo(string idActualSignalR, string identificadorLocal)
        {
            try
            {
                var sede = await _sedesServicios.ConsultarSedePorIdentificadorLocal(identificadorLocal);
                if (sede?.idSede > 0)
                {
                    // Desactivar conexiones previas
                    await DesactivarConexionesPrevias(sede.idSede);

                    // Registrar nuevo dispositivo
                    await _sedesconectadasServicios.Agregar(new SedesConectadas
                    {
                        idCliente = sede.idCliente,
                        idSede = sede.idSede,
                        idActualSignalR = idActualSignalR,
                        fechaUltimoAcceso = DateTime.Now,
                        activo = true
                    });

                    Console.WriteLine($"Dispositivo registrado correctamente: {idActualSignalR}");
                }
                else
                {
                    Console.WriteLine("Identificador local no válido o no se encontró la sede.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al registrar equipo: {ex.Message}");
            }
        }

        private async Task DesactivarConexionesPrevias(long idSede)
        {
            var conexionesActivas = await _sedesconectadasServicios.ConsultarPorSedeConEstadoActivo(idSede);

            foreach (var conexion in conexionesActivas)
            {
                conexion.activo = false;
                await _sedesconectadasServicios.Editar(conexion.idSedeConectada, conexion);
            }
        }

        public async Task ObtenerPin(string clienteId, string pin, int maxIdAnamnesis)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (!string.IsNullOrEmpty(idActualSignalR))
                {
                    await Clients.Client(idActualSignalR).SendAsync("ObtenerPin", Context.ConnectionId, pin, maxIdAnamnesis);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del pin
                Console.Error.WriteLine($"Error al obtener pin: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerPin(string clienteId, string respuestaPin)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerPin", clienteId, respuestaPin);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del pin
                Console.Error.WriteLine($"Error al enviar respuesta de obtener pin: {ex.Message}");
            }
        }



        public async Task ObtenerDoctor(string clienteId, string idDoctor)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (!string.IsNullOrEmpty(idActualSignalR))
                {
                    await Clients.Client(idActualSignalR).SendAsync("ObtenerDoctor", Context.ConnectionId, idDoctor);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerDoctor(string clienteId, string respuestaObtenerDoctor)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerDoctor", clienteId, respuestaObtenerDoctor);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task<List<SedesConectadas>> ObtenerActualizarSedesActivasPorCliente(long idCliente)
        {
            try
            {
                return await _sedesconectadasServicios.ConsultarSedesConectadasActivasPorCliente(idCliente);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la obtención de sedes activas
                Console.Error.WriteLine($"Error al obtener sedes activas: {ex.Message}");
                // Retorna una lista vacía en caso de error
                return new List<SedesConectadas>();
            }
        }




        public async Task ObtenerDoctorSiLoCambian(string clienteId, string idDoctor)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (!string.IsNullOrEmpty(idActualSignalR))
                {
                    await Clients.Client(idActualSignalR).SendAsync("ObtenerDoctorSiLoCambian", Context.ConnectionId, idDoctor);
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, "No se encontró conexión activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerDoctorSiLoCambian(string clienteId, string respuestaObtenerDoctor)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerDoctorSiLoCambian", clienteId, respuestaObtenerDoctor);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }

        //----------------ConsultarPorDiaYPorUnidad en Base de Datos Rydent Local----------------
        //ClienteId: Identificador del cliente local que tiene el worked por medio del cual se realizara la consulta en la bd rydent local
        //Este dato de clienteId queda guardado en sedes conectadas

        //Cuando se invoca es que se ejecuta estas funciones del servidor SR

        public async Task AgendarCita(string clienteId, string modelocrearcita)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaAgendarCita(string clienteId, string modelocrearcita)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                await Clients.Client(clienteId).SendAsync("RespuestaAgendarCita", clienteId, modelocrearcita);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }




        public async Task BuscarPaciente(string clienteId, string tipoBuqueda, string valorDeBusqueda)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }


        }

        public async Task RespuestaBuscarPaciente(string clienteId, string listPacientes)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                await Clients.Client(clienteId).SendAsync("RespuestaBuscarPaciente", clienteId, listPacientes);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }

        public async Task BuscarCitasPacienteAgenda(string clienteId, string valorBuscarAgenda)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaBuscarCitasPacienteAgenda(string clienteId, string listPacientes)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                await Clients.Client(clienteId).SendAsync("RespuestaBuscarCitasPacienteAgenda", clienteId, listPacientes);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }



        public async Task ObtenerDatosPersonalesCompletosPaciente(string clienteId, string idAnanesis)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerDatosPersonalesCompletosPaciente(string clienteId, string paciente)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosPersonalesCompletosPaciente", clienteId, paciente);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task ObtenerAntecedentesPaciente(string clienteId, string idAnanesis)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerAntecedentesPaciente(string clienteId, string paciente)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerAntecedentesPaciente", clienteId, paciente);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }


        // Editar antecedentes

        public async Task EditarAntecedentes(string clienteId, string antecedentesPaciente)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("EditarAntecedentes", Context.ConnectionId, antecedentesPaciente);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaEditarAntecedentes(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaEditarAntecedentes" +
                    "", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }


        public async Task ObtenerDatosEvolucion(string clienteId, string idAnanesis)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }





        public async Task RespuestaObtenerDatosEvolucion(string clienteId, string evolucion)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosEvolucion", clienteId, evolucion);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task GuardarDatosEvolucion(string clienteId, string evolucion)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaGuardarDatosEvolucion(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaGuardarDatosEvolucion", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }



        public async Task GuardarDatosPersonales(string clienteId, string datosPersonales)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("GuardarDatosPersonales", Context.ConnectionId, datosPersonales);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaGuardarDatosPersonales(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaGuardarDatosPersonales" +
                    "", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }


        public async Task EditarDatosPersonales(string clienteId, string datosPersonales)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("EditarDatosPersonales", Context.ConnectionId, datosPersonales);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaEditarDatosPersonales(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaEditarDatosPersonales" +
                    "", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        // listar facturas entre fechas por id

        public async Task ObtenerFacturasPorIdEntreFechas(string clienteId, string modeloDatosParaConsultarFacturasEntreFechas)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("ObtenerFacturasPorIdEntreFechas", Context.ConnectionId, modeloDatosParaConsultarFacturasEntreFechas);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerFacturasPorIdEntreFechas(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerFacturasPorIdEntreFechas", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }



        public async Task GuardarDatosRips(string clienteId, string datosRips)
        {
            try
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaGuardarDatosRips(string clienteId, bool respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaGuardarDatosRips", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task PresentarRips(string clienteId, int identificador, string objPresentarRips)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("PresentarRips", Context.ConnectionId, identificador, objPresentarRips);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaPresentarRips(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaPresentarRips", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task GenerarRips(string clienteId, int identificador, string objGenerarRips)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("GenerarRips", Context.ConnectionId, identificador, objGenerarRips);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaGenerarRips(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaGenerarRips", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task ObtenerFacturasPendientes(string clienteId)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("ObtenerFacturasPendientes", Context.ConnectionId);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerFacturasPendientes(string clienteId, string facturasPendientes)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerFacturasPendientes", clienteId, facturasPendientes);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }


        public async Task ObtenerFacturasCreadas(string clienteId, string Factura)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(idActualSignalR).SendAsync("ObtenerFacturasCreadas", Context.ConnectionId, Factura ?? "");
                    }
                    catch (Exception e)
                    {
                        await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", idActualSignalR, e.Message);
                    }
                }
                else
                {
                    await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", idActualSignalR, "no se encontro conexion activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerFacturasCreadas(string clienteId, string facturasCreadas)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerFacturasCreadas", clienteId, facturasCreadas);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }


        public async Task PresentarFacturasEnDian(string clienteId, string payloadJson)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (!string.IsNullOrEmpty(idActualSignalR))
                {
                    try
                    {
                        // Enviar al cliente local (worker) usando el ConnectionId ACTUAL validado
                        await Clients.Client(idActualSignalR).SendAsync(
                            "PresentarFacturasEnDian",
                            Context.ConnectionId, // para que el worker sepa a quién devolver
                            payloadJson
                        );
                    }
                    catch (Exception e)
                    {
                        await Clients.Client(Context.ConnectionId)
                            .SendAsync("ErrorConexion", clienteId, e.Message);
                    }
                }
                else
                {
                    await Clients.Client(Context.ConnectionId)
                        .SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
                }
            }
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId)
                    .SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error en PresentarFacturasEnDian: {ex.Message}");
            }
        }

        // 2) CLIENTE LOCAL (WORKER) -> CLOUD -> FRONT (PROGRESO OPCIONAL POR FACTURA)
        public async Task RespuestaProgresoPresentacion(
            string clienteIdDestino, // este es el Context.ConnectionId que enviaste arriba
            string progresoJson      // ej: { "documentRef":"33112", "status":"ENVIADA", "mensaje":"..." }
        )
        {
            try
            {
                await Clients.Client(clienteIdDestino)
                    .SendAsync("ProgresoPresentacionFactura", clienteIdDestino, progresoJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al reenviar progreso: {ex.Message}");
            }
        }

        // 3) CLIENTE LOCAL (WORKER) -> CLOUD -> FRONT (RESUMEN FINAL)
        public async Task RespuestaPresentarFacturasEnDian(
            string clienteIdDestino, // el Context.ConnectionId del Angular cloud
            string resumenJson
        )
        {
            try
            {
                await Clients.Client(clienteIdDestino)
                    .SendAsync("RespuestaPresentarFacturasEnDian", clienteIdDestino, resumenJson);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar resumen de presentación: {ex.Message}");
            }
        }



        public async Task ObtenerDatosAdministrativos(string clienteId, int idDoctor, DateTime fechaInicio, DateTime fechaFin)
        {
            try
            {
                string idActualSignalR = await ValidarIdActualSignalR(clienteId);
                if (idActualSignalR != "")
                {
                    try
                    {
                        await Clients.Client(clienteId).SendAsync("ObtenerDatosAdministrativos", Context.ConnectionId, idDoctor, fechaInicio, fechaFin);
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }
        }

        public async Task RespuestaObtenerDatosAdministrativos(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerDatosAdministrativos", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }
        public async Task ObtenerCodigosEps(string clienteId)
        {
            try
            {
                //-----Context.ConnectionId es el identificador del equipo que realiza la consulta es decir del cliente angular del que esta en la nube
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }

        }

        public async Task RespuestaObtenerCodigosEps(string clienteId, string listadoeps)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerCodigosEps", clienteId, listadoeps);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }


        public async Task ObtenerConsultaPorDiaYPorUnidad(string clienteId, string silla, DateTime fecha)
        {
            try
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }
        }
        //En este caso ClienteId es el Identificador del cliente angular que previamente enviamos al invocar ConsultarPorDiaYPorUnidad el Context.ConnectionId 
        public async Task RespuestaObtenerConsultaPorDiaYPorUnidad(string clienteId, string respuesta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaObtenerConsultaPorDiaYPorUnidad", clienteId, respuesta);
            }
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }

        }

        public async Task RealizarAccionesEnCitaAgendada(string clienteId, string modelorealizaraccionesenlacitaagendada)
        {
            try
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                // Manejo de errores en la obtención del doctor
                Console.Error.WriteLine($"Error al obtener doctor: {ex.Message}");
            }
        }

        public async Task RespuestaRealizarAccionesEnCitaAgendada(string clienteId, string modelorealizaraccionesenlacitaagendada)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync("RespuestaRealizarAccionesEnCitaAgendada", clienteId, modelorealizaraccionesenlacitaagendada);
            }//respuesta es el objeto que se obtiene de la consulta en la bd rydent local, debe devolver una instancia del objeto TCitas y un listado del objeto TDetalleCitas
            catch (Exception ex)
            {
                // Manejo de errores en la respuesta del doctor
                Console.Error.WriteLine($"Error al enviar respuesta de obtener doctor: {ex.Message}");
            }
        }

        public async Task ConsultarEstadoCuenta(string clienteId, string modeloDatosParaConsultarEstadoCuenta)
        {
            try
            {
                // clienteId = idActualSignalR de la sede destino (worker)
                var workerId = await ValidarIdActualSignalR(clienteId);

                if (!string.IsNullOrWhiteSpace(workerId))
                {
                    try
                    {
                        // ✅ Reenviamos AL WORKER (no al clienteId original)
                        // Context.ConnectionId = conexión del FRONT que pide
                        await Clients.Client(workerId).SendAsync(
                            "ConsultarEstadoCuenta",
                            Context.ConnectionId,
                            modeloDatosParaConsultarEstadoCuenta
                        );
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error al consultar estado cuenta: {ex.Message}");
            }
        }

        public async Task RespuestaConsultarEstadoCuenta(string clienteId, string respuestaConsultarEstadoCuenta)
        {
            try
            {
                // ✅ clienteId aquí SIEMPRE es el ConnectionId del FRONT
                await Clients.Client(clienteId).SendAsync(
                    "RespuestaConsultarEstadoCuenta",
                    clienteId,
                    respuestaConsultarEstadoCuenta
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar respuesta consultar estado cuenta: {ex.Message}");
            }
        }

        public async Task PrepararEstadoCuenta(string clienteId, string modeloPrepararEstadoCuenta)
        {
            try
            {
                var workerId = await ValidarIdActualSignalR(clienteId);

                if (!string.IsNullOrWhiteSpace(workerId))
                {
                    try
                    {
                        await Clients.Client(workerId).SendAsync(
                            "PrepararEstadoCuenta",
                            Context.ConnectionId,
                            modeloPrepararEstadoCuenta
                        );
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error al preparar estado cuenta: {ex.Message}");
            }
        }

        public async Task RespuestaPrepararEstadoCuenta(string clienteId, string respuestaPrepararEstadoCuenta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync(
                    "RespuestaPrepararEstadoCuenta",
                    clienteId,
                    respuestaPrepararEstadoCuenta
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar respuesta preparar estado cuenta: {ex.Message}");
            }
        }

        public async Task CrearEstadoCuenta(string clienteId, string modeloCrearEstadoCuenta)
        {
            try
            {
                var workerId = await ValidarIdActualSignalR(clienteId);

                if (!string.IsNullOrWhiteSpace(workerId))
                {
                    try
                    {
                        await Clients.Client(workerId).SendAsync(
                            "CrearEstadoCuenta",
                            Context.ConnectionId,
                            modeloCrearEstadoCuenta
                        );
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error al crear estado cuenta: {ex.Message}");
            }
        }

        public async Task RespuestaCrearEstadoCuenta(string clienteId, string respuestaCrearEstadoCuenta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync(
                    "RespuestaCrearEstadoCuenta",
                    clienteId,
                    respuestaCrearEstadoCuenta
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar respuesta crear estado cuenta: {ex.Message}");
            }
        }

		public async Task PrepararEditarEstadoCuenta(string clienteId, string modeloPrepararEditarEstadoCuenta)
		{
			try
			{
				// Valida que el destino (worker/sede) siga conectado
				string idActualSignalR = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(idActualSignalR))
				{
					try
					{
						// Reenvía al Worker:
						// clienteId (o idActualSignalR) = conexión del worker/sede destino
						// Context.ConnectionId = conexión del FRONT que pidió
						await Clients.Client(idActualSignalR).SendAsync(
							"PrepararEditarEstadoCuenta",
							Context.ConnectionId,
							modeloPrepararEditarEstadoCuenta
						);
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
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
				Console.Error.WriteLine($"Error al preparar editar estado cuenta: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararEditarEstadoCuenta(string clienteId, string respuestaPrepararEditarEstadoCuenta)
		{
			try
			{
				// clienteId aquí es el ConnectionId del FRONT (el que el hub le pasó al worker)
				await Clients.Client(clienteId).SendAsync(
					"RespuestaPrepararEditarEstadoCuenta",
					clienteId,
					respuestaPrepararEditarEstadoCuenta
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta preparar editar estado cuenta: {ex.Message}");
			}
		}


		public async Task EditarEstadoCuenta(string clienteId, string modeloEditarEstadoCuenta)
        {
            try
            {
                var workerId = await ValidarIdActualSignalR(clienteId);

                if (!string.IsNullOrWhiteSpace(workerId))
                {
                    try
                    {
                        await Clients.Client(workerId).SendAsync(
                            "EditarEstadoCuenta",
                            Context.ConnectionId,
                            modeloEditarEstadoCuenta
                        );
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error al editar estado cuenta: {ex.Message}");
            }
        }

        public async Task RespuestaEditarEstadoCuenta(string clienteId, string respuestaEditarEstadoCuenta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync(
                    "RespuestaEditarEstadoCuenta",
                    clienteId,
                    respuestaEditarEstadoCuenta
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar respuesta editar estado cuenta: {ex.Message}");
            }
        }

        public async Task BorrarEstadoCuenta(string clienteId, string modeloBorrarEstadoCuenta)
        {
            try
            {
                var workerId = await ValidarIdActualSignalR(clienteId);

                if (!string.IsNullOrWhiteSpace(workerId))
                {
                    try
                    {
                        await Clients.Client(workerId).SendAsync(
                            "BorrarEstadoCuenta",
                            Context.ConnectionId,
                            modeloBorrarEstadoCuenta
                        );
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
            catch (Exception ex)
            {
                await Clients.Client(Context.ConnectionId).SendAsync("ErrorConexion", clienteId, ex.Message);
                Console.Error.WriteLine($"Error al borrar estado cuenta: {ex.Message}");
            }
        }

        public async Task RespuestaBorrarEstadoCuenta(string clienteId, string respuestaBorrarEstadoCuenta)
        {
            try
            {
                await Clients.Client(clienteId).SendAsync(
                    "RespuestaBorrarEstadoCuenta",
                    clienteId,
                    respuestaBorrarEstadoCuenta
                );
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al enviar respuesta borrar estado cuenta: {ex.Message}");
            }
        }

		// ======================================================
		// PREPARAR INSERTAR ABONO (Front -> Hub -> Worker)
		// ======================================================
		public async Task PrepararInsertarAbono(string clienteId, string modeloPrepararInsertarAbono)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"PrepararInsertarAbono",
							Context.ConnectionId,              // <- el worker responde a este id
							modeloPrepararInsertarAbono
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al preparar insertar abono: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararInsertarAbono(string clienteId, string respuestaPrepararInsertarAbono)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaPrepararInsertarAbono",
					clienteId,
					respuestaPrepararInsertarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta preparar insertar abono: {ex.Message}");
			}
		}

		// ======================================================
		// INSERTAR ABONO (Front -> Hub -> Worker)
		// ======================================================
		public async Task InsertarAbono(string clienteId, string modeloInsertarAbono)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"InsertarAbono",
							Context.ConnectionId,              // <- el worker responde a este id
							modeloInsertarAbono
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al insertar abono: {ex.Message}");
			}
		}

		public async Task RespuestaInsertarAbono(string clienteId, string respuestaInsertarAbono)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaInsertarAbono",
					clienteId,
					respuestaInsertarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta insertar abono: {ex.Message}");
			}
		}

		// ======================================================
		// PREPARAR INSERTAR ADICIONAL (Front -> Hub -> Worker)
		// ======================================================
		public async Task PrepararInsertarAdicional(string clienteId, string modeloPrepararInsertarAdicional)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"PrepararInsertarAdicional",
							Context.ConnectionId,              // <- el worker responde a este id
							modeloPrepararInsertarAdicional
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al preparar insertar adicional: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararInsertarAdicional(string clienteId, string respuestaPrepararInsertarAdicional)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaPrepararInsertarAdicional",
					clienteId,
					respuestaPrepararInsertarAdicional
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta preparar insertar adicional: {ex.Message}");
			}
		}

		// ======================================================
		// INSERTAR ADICIONAL (Front -> Hub -> Worker)
		// ======================================================
		public async Task InsertarAdicional(string clienteId, string modeloInsertarAdicional)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"InsertarAdicional",
							Context.ConnectionId,              // <- el worker responde a este id
							modeloInsertarAdicional
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al insertar adicional: {ex.Message}");
			}
		}

		public async Task RespuestaInsertarAdicional(string clienteId, string respuestaInsertarAdicional)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaInsertarAdicional",
					clienteId,
					respuestaInsertarAdicional
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta insertar adicional: {ex.Message}");
			}
		}


		// ======================================================
		// PREPARAR BORRAR ABONO (Front -> Hub -> Worker)
		// ======================================================
		public async Task PrepararBorrarAbono(string clienteId, string modeloPrepararBorrarAbono)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"PrepararBorrarAbono",
							Context.ConnectionId,                 // <- el worker responde a este id
							modeloPrepararBorrarAbono
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al preparar borrar abono: {ex.Message}");
			}
		}

		public async Task RespuestaPrepararBorrarAbono(string clienteId, string respuestaPrepararBorrarAbono)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaPrepararBorrarAbono",
					clienteId,
					respuestaPrepararBorrarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta preparar borrar abono: {ex.Message}");
			}
		}


		// ======================================================
		// BORRAR ABONO (Front -> Hub -> Worker)
		// ======================================================
		public async Task BorrarAbono(string clienteId, string modeloBorrarAbono)
		{
			try
			{
				var workerId = await ValidarIdActualSignalR(clienteId);

				if (!string.IsNullOrWhiteSpace(workerId))
				{
					try
					{
						await Clients.Client(workerId).SendAsync(
							"BorrarAbono",
							Context.ConnectionId,                 // <- el worker responde a este id
							modeloBorrarAbono
						);
					}
					catch (Exception e)
					{
						await Clients.Client(Context.ConnectionId)
							.SendAsync("ErrorConexion", clienteId, e.Message);
					}
				}
				else
				{
					await Clients.Client(Context.ConnectionId)
						.SendAsync("ErrorConexion", clienteId, "no se encontro conexion activa");
				}
			}
			catch (Exception ex)
			{
				await Clients.Client(Context.ConnectionId)
					.SendAsync("ErrorConexion", clienteId, ex.Message);

				Console.Error.WriteLine($"Error al borrar abono: {ex.Message}");
			}
		}

		public async Task RespuestaBorrarAbono(string clienteId, string respuestaBorrarAbono)
		{
			try
			{
				await Clients.Client(clienteId).SendAsync(
					"RespuestaBorrarAbono",
					clienteId,
					respuestaBorrarAbono
				);
			}
			catch (Exception ex)
			{
				Console.Error.WriteLine($"Error al enviar respuesta borrar abono: {ex.Message}");
			}
		}

	}
}
