using System.Collections.Generic;
using System.Text.Json;
using RydentWebApiNube.v2.Modelos;

namespace RydentWebApiNube.v2.Servicios
{
    public interface IAccionWorker
    {
        // 1. Dice a qué "Acción de Angular" responde (Ej: "OBTENERDOCTOR")
        string NombreAccion { get; }

        // 2. Arma el SQL para el Worker
        LoteInstruccionesSQL GenerarLote(Dictionary<string, object> parametros);

        // 3. Traduce el JSON crudo del Worker al objeto de Angular
        string TraducirParaAngular(Dictionary<string, JsonElement> datosWorkerCrudos);
    }
}