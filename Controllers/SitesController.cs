using EnergyDashboardAPI1.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace EnergyDashboardAPI1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin,EnergyManager")]
    public class SitesController : ControllerBase
    {
        private readonly EnergyDbContext _context;

        public SitesController(EnergyDbContext context)
        {
            _context = context;
        }


        [HttpPost]
        public async Task<IActionResult> PostSite([FromBody] Site site)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSingleSite), new { id = site.SiteId }, site);
        }


        [HttpGet("Single")]
        public async Task<ActionResult<Site>> GetSingleSite()
        {
            var site = await _context.Sites.FirstOrDefaultAsync();

            if (site == null)
                return NotFound("No site data found.");

            return Ok(site);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSite(int id, [FromBody] Site updatedSite)
        {
            if (id != updatedSite.SiteId)
                return BadRequest("ID mismatch.");

            var existingSite = await _context.Sites.FindAsync(id);
            if (existingSite == null)
                return NotFound("Site not found.");


            existingSite.SiteName = updatedSite.SiteName;
            existingSite.Address = updatedSite.Address;
            existingSite.ContactPerson = updatedSite.ContactPerson;
            existingSite.ContactEmail = updatedSite.ContactEmail;
            existingSite.ContactPhone = updatedSite.ContactPhone;

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}