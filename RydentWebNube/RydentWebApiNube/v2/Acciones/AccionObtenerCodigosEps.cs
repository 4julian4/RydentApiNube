using System.Collections.Generic;
using System.Text.Json;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using RydentWebApiNube.v2.Modelos;
using RydentWebApiNube.v2.Modelos.DbRydent;
using RydentWebApiNube.v2.Servicios;

namespace RydentWebApiNube.v2.Acciones
{
    public class AccionObtenerCodigosEps : IAccionWorker
    {
        public string NombreAccion => "OBTENERCODIGOSEPS";

        public LoteInstruccionesSQL GenerarLote(Dictionary<string, object> parametros)
        {
            var lote = new LoteInstruccionesSQL { AccionOriginal = NombreAccion };
            lote.Consultas.Add(new ComandoSQLItem 
            { 
                ClaveResultado = "Eps", 
                ComandoSQL = "SELECT * FROM TCODIGOS_EPS" 
            });
            return lote;
        }

        public string TraducirParaAngular(Dictionary<string, JsonElement> datosWorkerCrudos)
        {
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var listEps = datosWorkerCrudos["Eps"].Deserialize<List<TCODIGOS_EPS>>(jsonOptions);
            return JsonSerializer.Serialize(listEps);
        }
    }
}