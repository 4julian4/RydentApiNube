using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RydentWebApiNube.LogicaDeNegocio.Entidades; // Tus entidades viejas de Entity Framework
using RydentWebApiNube.Models; // Donde viva RespuestaPinModel y ListadoItemModel
using RydentWebApiNube.v2.Modelos;
using RydentWebApiNube.v2.Modelos.DbRydent;
using RydentWebApiNube.v2.Servicios;

namespace RydentWebApiNube.v2.Acciones
{
    public class AccionObtenerPin : IAccionWorker
    {
        public string NombreAccion => "OBTENERPIN";

        // =========================================================================
        // 1. GENERAR SQL (Lo que antes hacía el Worker, ahora lo decide la Nube)
        // =========================================================================
        public LoteInstruccionesSQL GenerarLote(Dictionary<string, object> parametros)
        {
            if (!parametros.ContainsKey("pin")) throw new ArgumentException("Falta parámetro: pin");
            if (!parametros.ContainsKey("maxIdAnamnesis")) throw new ArgumentException("Falta parámetro: maxIdAnamnesis");

            string pinacceso = parametros["pin"].ToString() ?? "";
            int maxIdAnamnesis = Convert.ToInt32(parametros["maxIdAnamnesis"].ToString());

            var lote = new LoteInstruccionesSQL { AccionOriginal = NombreAccion };

            // A. Primero validamos la clave (Siempre)
            lote.Consultas.Add(new ComandoSQLItem 
            { 
                ClaveResultado = "Clave", 
                ComandoSQL = "SELECT * FROM TCLAVE WHERE CLAVE = @pin",
                Parametros = new Dictionary<string, object> { { "@pin", pinacceso } }
            });

            // B. Pedimos TODOS los catálogos de un solo golpe
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "Doctores", ComandoSQL = "SELECT ID, NOMBRE FROM TDATOSDOCTORES" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "Convenios", ComandoSQL = "SELECT ID, NOMBRE FROM T_CONVENIOS" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "InfoReportes", ComandoSQL = "SELECT ID, NOMBRE FROM TINFORMACIONREPORTES" });
            
            // OJO: Esta consulta la sacabas de un método complejo, aquí la resuelves con un simple WHERE
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "DoctoresConPrestador", ComandoSQL = "SELECT ID, NOMBRE FROM TINFORMACIONREPORTES WHERE CODIGOPRESTADOR IS NOT NULL AND CODIGOPRESTADOR <> ''" });
            
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "FrasesXEvolucion", ComandoSQL = "SELECT * FROM T_FRASE_XEVOLUCION" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "HorariosAgenda", ComandoSQL = "SELECT * FROM THORARIOSAGENDA" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "HorariosAsuntos", ComandoSQL = "SELECT * FROM THORARIOSASUNTOS" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "Festivos", ComandoSQL = "SELECT * FROM TFESTIVOS" });
            lote.Consultas.Add(new ComandoSQLItem { ClaveResultado = "ConfigRydent", ComandoSQL = "SELECT * FROM TCONFIGURACIONES_RYDENT" });

            // C. La consulta de pacientes para el buscador (Solo los que superan el maxId)
            lote.Consultas.Add(new ComandoSQLItem 
            { 
                ClaveResultado = "PacientesBuscador", 
                ComandoSQL = "SELECT IDANAMNESIS, NOMBRE_PACIENTE, IDANAMNESISTEXTO, NUMDOCUMENTO, DOCTOR, PERFIL, NUMAFILIACION, TELEFONO FROM TANAMNESIS WHERE IDANAMNESIS > @maxId",
                Parametros = new Dictionary<string, object> { { "@maxId", maxIdAnamnesis } }
            });

            return lote;
        }

        // =========================================================================
        // 2. TRADUCIR PARA ANGULAR (Engañamos al Frontend armando el DTO viejo)
        // =========================================================================
        public string TraducirParaAngular(Dictionary<string, JsonElement> datosWorkerCrudos)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var respuestaFinal = new RespuestaPinModel { acceso = false };

            // 1. Verificamos si la clave vino correcta (Si hay resultados en la tabla Clave)
            if (datosWorkerCrudos.ContainsKey("Clave") && datosWorkerCrudos["Clave"].GetArrayLength() > 0)
            {
                // Mapeamos el TCLAVE y borramos la contraseña por seguridad web
                respuestaFinal.clave = datosWorkerCrudos["Clave"][0].Deserialize<TCLAVE>(jsonOptions) ?? new TCLAVE();
                respuestaFinal.clave.CLAVE = ""; 
                respuestaFinal.acceso = true;

                // 2. Llenamos los modelos ListadoItemModel (Solo requieren ID y Nombre)
                if (datosWorkerCrudos.ContainsKey("Doctores"))
                    respuestaFinal.lstDoctores = MapearAListadoItemModel(datosWorkerCrudos["Doctores"], jsonOptions);
                
                if (datosWorkerCrudos.ContainsKey("Convenios"))
                    respuestaFinal.lstConvenios = MapearAListadoItemModel(datosWorkerCrudos["Convenios"], jsonOptions);
                
                if (datosWorkerCrudos.ContainsKey("InfoReportes"))
                    respuestaFinal.lstInformacionReporte = MapearAListadoItemModel(datosWorkerCrudos["InfoReportes"], jsonOptions);
                
                if (datosWorkerCrudos.ContainsKey("DoctoresConPrestador"))
                    respuestaFinal.lstDoctoresConPrestador = MapearAListadoItemModel(datosWorkerCrudos["DoctoresConPrestador"], jsonOptions);

                // 3. Llenamos las listas enteras de EF Core (Mapeo directo)
                if (datosWorkerCrudos.ContainsKey("FrasesXEvolucion"))
                    respuestaFinal.lstFrasesXEvolucion = datosWorkerCrudos["FrasesXEvolucion"].Deserialize<List<T_FRASE_XEVOLUCION>>(jsonOptions) ?? new();

                if (datosWorkerCrudos.ContainsKey("HorariosAgenda"))
                    respuestaFinal.lstHorariosAgenda = datosWorkerCrudos["HorariosAgenda"].Deserialize<List<THORARIOSAGENDA>>(jsonOptions) ?? new();

                if (datosWorkerCrudos.ContainsKey("HorariosAsuntos"))
                    respuestaFinal.lstHorariosAsuntos = datosWorkerCrudos["HorariosAsuntos"].Deserialize<List<THORARIOSASUNTOS>>(jsonOptions) ?? new();

                if (datosWorkerCrudos.ContainsKey("Festivos"))
                    respuestaFinal.lstFestivos = datosWorkerCrudos["Festivos"].Deserialize<List<TFESTIVOS>>(jsonOptions) ?? new();

                if (datosWorkerCrudos.ContainsKey("ConfigRydent"))
                    respuestaFinal.lstConfiguracionesRydent = datosWorkerCrudos["ConfigRydent"].Deserialize<List<TCONFIGURACIONES_RYDENT>>(jsonOptions) ?? new();

                if (datosWorkerCrudos.ContainsKey("PacientesBuscador"))
                    respuestaFinal.lstAnamnesisParaAgendayBuscadores = datosWorkerCrudos["PacientesBuscador"].Deserialize<List<TANAMNESIS>>(jsonOptions);
            }

            // Lo convertimos a JSON usando Newtonsoft (para respetar cualquier configuración de formato de tu Angular)
            return Newtonsoft.Json.JsonConvert.SerializeObject(respuestaFinal);
        }

        // --- Helper interno para convertir al ListadoItemModel ---
        private List<ListadoItemModel> MapearAListadoItemModel(JsonElement jsonElement, JsonSerializerOptions options)
        {
            var tempList = new List<ListadoItemModel>();
            if (jsonElement.ValueKind != JsonValueKind.Array) return tempList;

            foreach (var fila in jsonElement.EnumerateArray())
            {
                // Obtenemos ID y NOMBRE como strings genéricos sin importar qué tabla fue
                string id = fila.GetProperty("ID").ToString();
                string nombre = fila.GetProperty("NOMBRE").GetString() ?? "";

                tempList.Add(new ListadoItemModel { id = id, nombre = nombre });
            }
            return tempList;
        }
    }
}