using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RydentWebApiNube.v2.Modelos;

namespace RydentWebApiNube.v2.Servicios
{
    public interface IGestorAccionesWorkerService
    {
        LoteInstruccionesSQL GenerarLote(string accion, Dictionary<string, object> parametros);
        string TraducirParaAngular(string accionOriginal, string jsonCrudoWorker);
    }

    public class GestorAccionesWorkerService : IGestorAccionesWorkerService
    {
        // Guardamos todas las acciones en un diccionario en memoria RAM para que sea ultra rápido
        private readonly Dictionary<string, IAccionWorker> _estrategias;

        // .NET inyecta mágicamente todas las clases que hereden de IAccionWorker aquí
        public GestorAccionesWorkerService(IEnumerable<IAccionWorker> accionesDisponibles)
        {
            _estrategias = accionesDisponibles.ToDictionary(
                a => a.NombreAccion.ToUpper(), 
                a => a
            );
        }

        public LoteInstruccionesSQL GenerarLote(string accion, Dictionary<string, object> parametros)
        {
            string clave = accion.ToUpper();
            if (!_estrategias.ContainsKey(clave))
                throw new Exception($"La acción '{accion}' no tiene una estrategia configurada.");

            return _estrategias[clave].GenerarLote(parametros);
        }

        public string TraducirParaAngular(string accionOriginal, string jsonCrudoWorker)
        {
            string clave = accionOriginal.ToUpper();
            if (!_estrategias.ContainsKey(clave))
                throw new Exception($"La acción '{accionOriginal}' no tiene un traductor configurado.");

            var datosWorkerCrudos = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(jsonCrudoWorker);
            
            return _estrategias[clave].TraducirParaAngular(datosWorkerCrudos);
        }
    }
}