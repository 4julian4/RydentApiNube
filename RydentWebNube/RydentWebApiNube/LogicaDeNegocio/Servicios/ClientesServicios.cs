using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;
using System;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class ClientesServicios : IClientesServicios
    {
        public ClientesServicios()
        {
           
        }


        public async Task<long> Agregar(Clientes clientes)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.TClientes.Add(clientes);
                await _dbcontext.SaveChangesAsync();
                return clientes.idCliente;
            }
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
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(clientes);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
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