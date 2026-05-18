using Microsoft.EntityFrameworkCore;
using Orbital.API.Data;
using Orbital.API.Models;

namespace Orbital.API.Services
{
    public class GalaxiaService
    {
        private readonly AppDbContext _context;

        public GalaxiaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Galaxia>> ObtenerTodas()
        {
            return await _context.Galaxias.ToListAsync();
        }

        public async Task<Galaxia?> ObtenerPorId(int id)
        {
            return await _context.Galaxias.FindAsync(id);
        }
    }
}