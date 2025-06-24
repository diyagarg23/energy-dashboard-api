using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnergyDashboardAPI1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnergyDashboardAPI1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MetersController : ControllerBase
    {
        private readonly EnergyDbContext _context;

        public MetersController(EnergyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Meter>>> GetMeters()
        {
            return await _context.Meters.ToListAsync();
        }
    }
}