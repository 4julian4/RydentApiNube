using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using System;
using System.Text;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class ClientesServicios : IClientesServicios
    {
        public ClientesServicios()
        {
           
        }

		


		public async Task<long> Agregar(Clientes clientes)
		{
			try
			{
				using (var _dbcontext = new AppDbContext())
				{
					// ✅ si no viene, lo generamos acá
					if (clientes.clienteGuid == Guid.Empty)
						clientes.clienteGuid = Guid.NewGuid();

					_dbcontext.TClientes.Add(clientes);
					await _dbcontext.SaveChangesAsync();
					return clientes.idCliente;
				}
			}
			catch (DbUpdateException ex)
			{
				// Esto casi siempre trae el error real de SQL Server dentro de InnerException
				var msg = BuildFullMessage(ex);
				// Puedes loguearlo aquí también si tienes ILogger
				throw new Exception($"Error guardando cliente: {msg}", ex);
			}
			catch (Exception ex)
			{
				var msg = BuildFullMessage(ex);
				throw new Exception($"Error inesperado guardando cliente: {msg}", ex);
			}
		}

		private static string BuildFullMessage(Exception ex)
		{
			var sb = new StringBuilder();
			var cur = ex;
			while (cur != null)
			{
				sb.AppendLine(cur.Message);
				cur = cur.InnerException;
			}
			return sb.ToString();
		}

		public async Task Borrar(long idCliente)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TClientes.FirstOrDefaultAsync(x => x.idCliente == idCliente);
                _dbcontext.TClientes.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<Clientes> ConsultarPorId(long idCliente)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TClientes.FirstOrDefaultAsync(x => x.idCliente == idCliente);
                return obj == null ? new Clientes() : obj;
            }
        }

        public async Task<List<Clientes>> ConsultarTodos()
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TClientes.ToListAsync() ?? new List<Clientes>();
            }
                
        }

        public async Task<bool> Editar(long idCliente, Clientes clientes)
        {

			using (var _dbcontext = new AppDbContext())
			{
				var obj = await _dbcontext.TClientes.FirstOrDefaultAsync(x => x.idCliente == idCliente);
				if (obj == null) return false;

				// ✅ Editar SOLO lo permitido (NO tocar clienteGuid, idCliente, fechaCreacion)
				obj.nombreCliente = clientes.nombreCliente;
				obj.activoHasta = clientes.activoHasta;
				obj.observacion = clientes.observacion;

				obj.telefono1 = clientes.telefono1;
				obj.telefono2 = clientes.telefono2;
				obj.emailContacto = clientes.emailContacto;

				obj.direccion = clientes.direccion;
				obj.ciudad = clientes.ciudad;
				obj.pais = clientes.pais;

				obj.clienteDesde = clientes.clienteDesde;
				obj.diaPago = clientes.diaPago;
				obj.fechaProximoPago = clientes.fechaProximoPago;
				obj.planNombre = clientes.planNombre;

				obj.usaRydentWeb = clientes.usaRydentWeb;
				obj.usaDataico = clientes.usaDataico;
				obj.usaFacturaTech = clientes.usaFacturaTech;

				// ✅ si Dataico está apagado, el tenant debe quedar null
				obj.billingTenantId = (clientes.usaDataico) ? clientes.billingTenantId : null;

				obj.estado = clientes.estado;

				// ✅ timestamps
				obj.fechaActualizacion = DateTime.Now;

				await _dbcontext.SaveChangesAsync();
				return true;
			}

		}
	}

    public interface IClientesServicios
    {
        Task<long> Agregar(Clientes clientes);
        Task<bool> Editar(long idCliente, Clientes clientes);
        Task<Clientes> ConsultarPorId(long idCliente);
        Task<List<Clientes>> ConsultarTodos();
        Task Borrar(long idCliente);
    }
}