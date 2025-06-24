using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EnergyDashboardAPI1.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EnergyDashboardAPI1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SpacesController : ControllerBase
    {
        private readonly EnergyDbContext _context;

        public SpacesController(EnergyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetSpaces()
        {
            try
            {
                var spaces = await _context.Spaces
                    .Select(s => new
                    {
                        s.SpaceId,
                        s.SpaceName,
                        s.ParentSpaceId
                    })
                    .ToListAsync();

                return Ok(spaces);
            }
            catch (Exception ex)
            {
                return StatusCode(500, "Error: " + ex.Message);
            }
        }
        [HttpGet("{id}/all-child-space-ids")]
        public async Task<ActionResult<List<int>>> GetAllChildSpaceIds(int id)
        {
            var allSpaces = await _context.Spaces.ToListAsync();

            List<int> result = new List<int> { id };
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(id);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                var children = allSpaces
                    .Where(s => s.ParentSpaceId == current)
                    .Select(s => s.SpaceId)
                    .ToList();

                foreach (var child in children)
                {
                    result.Add(child);
                    queue.Enqueue(child);
                }
            }

            return result;
        }
    }
}