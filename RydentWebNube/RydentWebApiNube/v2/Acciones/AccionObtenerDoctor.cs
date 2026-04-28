using System;
using System.Collections.Generic;
using System.Text.Json;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.Models;
using RydentWebApiNube.v2.Modelos;
using RydentWebApiNube.v2.Modelos.DbRydent;
using RydentWebApiNube.v2.Servicios;

namespace RydentWebApiNube.v2.Acciones
{
    public class AccionObtenerDoctor : IAccionWorker
    {
        public string NombreAccion => "OBTENERDOCTOR";

        public LoteInstruccionesSQL GenerarLote(Dictionary<string, object> parametros)
        {
            if (!parametros.ContainsKey("idDoctor")) throw new ArgumentException("Falta idDoctor");
            int idDoc = Convert.ToInt32(parametros["idDoctor"].ToString());

            var lote = new LoteInstruccionesSQL { AccionOriginal = NombreAccion };
            
            lote.Consultas.Add(new ComandoSQLItem { 
                ClaveResultado = "doctor", 
                ComandoSQL = "SELECT * FROM TDATOSDOCTORES WHERE ID = @id",
                Parametros = new Dictionary<string, object> { { "@id", idDoc } }
            });
            
            lote.Consultas.Add(new ComandoSQLItem { 
                ClaveResultado = "pacientes", 
                ComandoSQL = "SELECT COUNT(*) AS TOTAL FROM TANAMNESIS WHERE DOCTOR = @id",
                Parametros = new Dictionary<string, object> { { "@id", idDoc } }
            });

            return lote;
        }

        public string TraducirParaAngular(Dictionary<string, JsonElement> datosWorkerCrudos)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var respuestaDoctor = new RespuestaObtenerDoctorModel();
            
            if (datosWorkerCrudos.ContainsKey("doctor") && datosWorkerCrudos["doctor"].GetArrayLength() > 0)
                respuestaDoctor.doctor = datosWorkerCrudos["doctor"][0].Deserialize<TDATOSDOCTORES>(jsonOptions);
                
            if (datosWorkerCrudos.ContainsKey("pacientes") && datosWorkerCrudos["pacientes"].GetArrayLength() > 0)
                respuestaDoctor.totalPacientes = datosWorkerCrudos["pacientes"][0].GetProperty("TOTAL").GetInt32();

            respuestaDoctor.facturaElectronica = false; 
            return JsonSerializer.Serialize(respuestaDoctor);
        }
    }
}