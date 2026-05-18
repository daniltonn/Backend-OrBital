using Microsoft.EntityFrameworkCore;
using Orbital.API.Data;
using Orbital.API.Models;

namespace Orbital.API.Services
{
    public class AtmosferaService
    {
        private readonly AppDbContext _context;

        public AtmosferaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<TipoAtmosfera>> ObtenerTodas()
        {
            return await _context.TiposAtmosfera.ToListAsync();
        }

        public async Task<TipoAtmosfera?> ObtenerPorId(int id)
        {
            return await _context.TiposAtmosfera.FindAsync(id);
        }
    }
}