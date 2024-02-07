using Microsoft.EntityFrameworkCore;
using RydentWebApiNube.LogicaDeNegocio.DbContexts;
using RydentWebApiNube.LogicaDeNegocio.Entidades;

namespace RydentWebApiNube.LogicaDeNegocio.Servicios
{
    public class UsuariosServicios : IUsuariosServicios
    {
        protected readonly AppDbContext _dbcontext;
        public UsuariosServicios()
        {

        }


        public async Task<long> Agregar(Usuarios usuarios)
        {
            using (var _dbcontext = new AppDbContext())
            {
                _dbcontext.TUsuarios.Add(usuarios);
                await _dbcontext.SaveChangesAsync();
                return usuarios.idUsuario;
            }
        }

        public async Task Borrar(long idUsuario)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TUsuarios.FirstOrDefaultAsync(x => x.idUsuario == idUsuario);
                _dbcontext.TUsuarios.Remove(obj);
                await _dbcontext.SaveChangesAsync();
            }
        }

        public async Task<Usuarios> ConsultarPorId(long idUsuario)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TUsuarios.FirstOrDefaultAsync(x => x.idUsuario == idUsuario);
                return obj == null ? new Usuarios() : obj;
            }
        }

        public async Task<Usuarios> ConsultarPorCorreo(string correoUsuario)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TUsuarios.FirstOrDefaultAsync(x => x.correoUsuario == correoUsuario);
                return obj == null ? new Usuarios() : obj;
            }
        }

        public async Task<List<Usuarios>> ConsultarTodos()
        {
            using (var _dbcontext = new AppDbContext())
            {
                return await _dbcontext.TUsuarios.ToListAsync();
            }
        }

        public async Task<int> ConsultarCorreoyFechaActivo(string correoUsuario)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TUsuarios.Where(x => x.correoUsuario == correoUsuario).ToListAsync();
                if (obj.Count == 0)
                {
                    return 2; //No existe el correo
                }
                else
                {
                    var usuario = obj.FirstOrDefault();
                    var cliente = await _dbcontext.TClientes.FirstOrDefaultAsync(x => x.idCliente == usuario.idCliente);
                    if (cliente.activoHasta != null && cliente.activoHasta >= DateTime.Now.Date)
                    {
                        return 1; //El cliente esta activo
                    }
                    else
                    {
                        return 3; //El cliente no esta activo
                    }
                }   
            }
        }

        public async Task<bool> Editar(long idUsuario, Usuarios usuarios)
        {
            using (var _dbcontext = new AppDbContext())
            {
                var obj = await _dbcontext.TUsuarios.FirstOrDefaultAsync(x => x.idUsuario == idUsuario);
                if (obj == null)
                {
                    return false;
                }
                else
                {
                    _dbcontext.Entry(obj).CurrentValues.SetValues(usuarios);
                    await _dbcontext.SaveChangesAsync();
                    return true;
                }
            }
        }
    }

    public interface IUsuariosServicios
    {
        Task<long> Agregar(Usuarios usuarios);
        Task<bool> Editar(long idUsuario, Usuarios usuarios);
        Task<Usuarios> ConsultarPorId(long idUsuario);
        Task<Usuarios> ConsultarPorCorreo(string correoUsuario);
        Task<List<Usuarios>> ConsultarTodos();
        Task<int> ConsultarCorreoyFechaActivo(string correoUsuario);
        Task Borrar(long idUsuario);
    }
}
