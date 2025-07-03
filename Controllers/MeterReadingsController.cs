using EnergyDashboardAPI1.DTOs;
using EnergyDashboardAPI1.DTOs.EnergyDashboardAPI1.DTOs;
using EnergyDashboardAPI1.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;


namespace EnergyDashboardAPI1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Admin,AccountManager")]
    public class MeterReadingsController : ControllerBase
    {
        private readonly EnergyDbContext _context;

        public MeterReadingsController(EnergyDbContext context)
        {
            _context = context;
        }

        [HttpGet("meterreadings")]
        public async Task<IActionResult> GetMeterReadings(
    [FromQuery] int? spaceId = null,
    [FromQuery] DateTime? startTime = null,
    [FromQuery] DateTime? endTime = null)
        {
            try
            {
                IQueryable<MeterReading> query = _context.MeterReadings
                    .Include(mr => mr.Meter)
                    .ThenInclude(m => m.Space);

                if (spaceId.HasValue)
                {
                    query = query.Where(mr => mr.Meter.SpaceId == spaceId.Value);
                }
                if (startTime.HasValue)
                {
                    query = query.Where(mr => mr.ReadingTimestamp >= startTime.Value);
                }
                if (endTime.HasValue)
                {
                    query = query.Where(mr => mr.ReadingTimestamp <= endTime.Value);
                }

                var result = await query.ToListAsync();

                return Ok(result);
            }
            catch (Exception ex)
            {
                
                return StatusCode(500, new { message = "Internal server error", detail = ex.Message });
            }
        }

        [HttpGet("energy-consumption/{meterId}")]
        public async Task<IActionResult> GetEnergyConsumption(int meterId)
        {
            var readings = await _context.MeterReadings
                .Where(r => r.MeterID == meterId)
                .OrderBy(r => r.ReadingTimestamp)
                .ToListAsync();

            if (readings.Count < 2)
                return Ok("Not enough data to calculate consumption.");

            decimal totalConsumption = 0;

            for (int i = 1; i < readings.Count; i++)
            {
                var prev = readings[i - 1].Value;
                var curr = readings[i].Value;

                if (curr >= prev) 
                    totalConsumption += (curr - prev);
            }

            return Ok(new
            {
                MeterID = meterId,
                TotalEnergyConsumed = totalConsumption
            });
        }
        [HttpGet("energy-by-space/{spaceId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetEnergyBySpace(int spaceId)
        {
            
            var allSpaces = await _context.Spaces.ToListAsync();

            
            List<int> childSpaceIds = new List<int> { spaceId };
            Queue<int> queue = new Queue<int>();
            queue.Enqueue(spaceId);

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                var children = allSpaces
                    .Where(s => s.ParentSpaceId == current)
                    .Select(s => s.SpaceId)
                    .ToList();

                foreach (var child in children)
                {
                    childSpaceIds.Add(child);
                    queue.Enqueue(child);
                }
            }

            
            var readings = await _context.MeterReadings
                .Include(r => r.Meter)
                .Where(r => childSpaceIds.Contains(r.Meter.SpaceId))
                .OrderBy(r => r.MeterID)
                .ThenBy(r => r.ReadingTimestamp)
                .ToListAsync();

            
            var consumption = readings
                .GroupBy(r => r.MeterID)
                .Select(g => new
                {
                    MeterId = g.Key,
                    TotalEnergyConsumed = g
                        .OrderBy(r => r.ReadingTimestamp)
                        .Select((r, index) => index == 0 ? 0 : r.Value - g.ElementAt(index - 1).Value)
                        .Sum()
                });

            return Ok(consumption);
        }

        [HttpGet("energy-consumption-by-time")]
        public async Task<IActionResult> GetEnergyConsumptionByTime(DateTime startTime, DateTime endTime)
        {
            Console.WriteLine($"[DEBUG] StartTime: {startTime}, EndTime: {endTime}");

            var readings = await _context.MeterReadings
                .Where(r => r.ReadingTimestamp >= startTime && r.ReadingTimestamp <= endTime)
                .OrderBy(r => r.MeterID)
                .ThenBy(r => r.ReadingTimestamp)
                .ToListAsync();

            Console.WriteLine($"[DEBUG] Found {readings.Count} readings");

            return Ok(readings);
        }



        [HttpGet("energy-summary")]
        public async Task<IActionResult> GetEnergySummary(
            [FromQuery] List<int>? spaceIds,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime)
        {
            if (startTime >= endTime)
            {
                return BadRequest(new ApiError
                {
                    StatusCode = 400,
                    Message = "Start time must be earlier than end time."
                });
            }

            if (spaceIds != null && spaceIds.Any(id => id <= 0))
            {
                return BadRequest(new ApiError
                {
                    StatusCode = 400,
                    Message = "All space IDs must be positive integers."
                });
            }

            var meterQuery = _context.Meters.AsQueryable();

            if (spaceIds != null && spaceIds.Any())
            {
                meterQuery = meterQuery.Where(m => spaceIds.Contains(m.SpaceId));
            }

            var meterIds = await meterQuery.Select(m => m.MeterID).ToListAsync();

            if (!meterIds.Any())
            {
                return NotFound(new ApiError
                {
                    StatusCode = 404,
                    Message = "No meters found for the given space IDs."
                });
            }

            var readings = await _context.MeterReadings
                .Where(r => meterIds.Contains(r.MeterID)
                         && r.ReadingTimestamp >= startTime
                         && r.ReadingTimestamp <= endTime)
                .OrderBy(r => r.ReadingTimestamp)
                .ToListAsync();

            if (readings.Count < 2)
            {
                return Ok(new
                {
                    totalEnergyConsumption = 0,
                    hourlyAggregated = new List<object>()
                });
            }

            var hourlyAggregated = readings
                .GroupBy(r => new
                {
                    r.MeterID,
                    Hour = r.ReadingTimestamp
                            .AddMinutes(-r.ReadingTimestamp.Minute)
                            .AddSeconds(-r.ReadingTimestamp.Second)
                            .AddMilliseconds(-r.ReadingTimestamp.Millisecond)
                })
                .Select(g => new
                {
                    MeterId = g.Key.MeterID,
                    Timestamp = g.Key.Hour,
                    Reading = g.OrderBy(r => r.ReadingTimestamp).First().Value
                })
                .OrderBy(r => r.Timestamp)
                .ToList();

            var groupedList = hourlyAggregated
                .GroupBy(r => r.Timestamp)
                .OrderBy(g => g.Key)
                .ToList();

            var hourlyGrouped = groupedList
                .Select((group, index) =>
                {
                    var previous = index > 0 ? groupedList[index - 1] : null;
                    decimal totalDiff = 0;

                    if (previous != null)
                    {
                        foreach (var curr in group)
                        {
                            var prev = previous.FirstOrDefault(p => p.MeterId == curr.MeterId);
                            if (prev != null)
                            {
                                totalDiff += Math.Max(0, curr.Reading - prev.Reading);
                            }
                        }
                    }

                    return new
                    {
                        timestamp = group.Key,
                        energyConsumed = Math.Round(totalDiff, 2)
                    };
                })
                .Skip(1) 
                .ToList();

            var totalEnergy = Math.Round(hourlyGrouped.Sum(h => h.energyConsumed), 2);

            return Ok(new
            {
                totalEnergyConsumption = totalEnergy,
                hourlyAggregated = hourlyGrouped
            });
        }


        [HttpGet("space-wise-energy")]
        public async Task<IActionResult> GetSpaceWiseEnergy(
        [FromQuery] List<int>? spaceIds,
        [FromQuery] DateTime? startTime,
        [FromQuery] DateTime? endTime)
        {

            if (!startTime.HasValue || !endTime.HasValue)
            {
                return BadRequest(new ApiError
                {
                    StatusCode = 400,
                    Message = "Start time and end time are required."
                });
            }

            if (startTime >= endTime)
            {
                return BadRequest(new ApiError
                {
                    StatusCode = 400,
                    Message = "Start time must be earlier than end time."
                });
            }

            if (spaceIds != null && spaceIds.Any(id => id <= 0))
            {
                return BadRequest(new ApiError
                {
                    StatusCode = 400,
                    Message = "All space IDs must be positive integers."
                });
            }
            var meterQuery = _context.Meters.AsQueryable();

            if (spaceIds != null && spaceIds.Any())
            {
                meterQuery = meterQuery.Where(m => spaceIds.Contains(m.SpaceId));
            }

            var meterIds = await meterQuery.Select(m => m.MeterID).ToListAsync();

            if (!meterIds.Any())
            {
                return NotFound(new ApiError
                {
                    StatusCode = 404,
                    Message = "No meters found for the given space IDs."
                });
            }

            var readings = await _context.MeterReadings
                .Where(r => meterIds.Contains(r.MeterID) && r.ReadingTimestamp >= startTime && r.ReadingTimestamp <= endTime)
                .OrderBy(r => r.ReadingTimestamp)
                .ToListAsync();
            if (!readings.Any())
            {
                return NotFound(new ApiError
                {
                    StatusCode = 404,
                    Message = "No readings found for the given time range."
                });
            }


            var groupedReadings = readings
                .GroupBy(r => r.MeterID)
                .ToList();

            var spaceMeterMap = await _context.Meters
                .Where(m => meterIds.Contains(m.MeterID))
                .ToDictionaryAsync(m => m.MeterID, m => m.SpaceId);

            var totalBySpace = new Dictionary<int, decimal>();
            var hourlyBySpace = new Dictionary<int, Dictionary<DateTime, decimal>>();

            foreach (var meterGroup in groupedReadings)
            {
                var readingsList = meterGroup.OrderBy(r => r.ReadingTimestamp).ToList();
                for (int i = 1; i < readingsList.Count; i++)
                {
                    var prev = readingsList[i - 1];
                    var curr = readingsList[i];

                    if (curr.ReadingTimestamp.Subtract(prev.ReadingTimestamp).TotalHours != 1)
                        continue;

                    var diff = Math.Max(0, curr.Value - prev.Value);
                    var hour = curr.ReadingTimestamp.AddMinutes(-curr.ReadingTimestamp.Minute).AddSeconds(-curr.ReadingTimestamp.Second).AddMilliseconds(-curr.ReadingTimestamp.Millisecond);
                    if (!spaceMeterMap.TryGetValue(curr.MeterID, out var spaceId))
                        continue;

                    if (!totalBySpace.ContainsKey(spaceId))
                        totalBySpace[spaceId] = 0;

                    totalBySpace[spaceId] += diff;

                    if (!hourlyBySpace.ContainsKey(spaceId))
                        hourlyBySpace[spaceId] = new Dictionary<DateTime, decimal>();

                    if (!hourlyBySpace[spaceId].ContainsKey(hour))
                        hourlyBySpace[spaceId][hour] = 0;

                    hourlyBySpace[spaceId][hour] += diff;
                }
            }

            var result = new
            {
                totalBySpace = totalBySpace.Select(kvp => new
                {
                    spaceId = kvp.Key,
                    energyConsumed = kvp.Value
                }),
                hourlyBySpace = hourlyBySpace.Select(kvp => new
                {
                    spaceId = kvp.Key,
                    hourly = kvp.Value.Select(h => new
                    {
                        timestamp = h.Key,
                        energyConsumed = h.Value
                    })
                })
            };

            return Ok(result);
        }


        [HttpGet("aggregated-summary")]
        public async Task<IActionResult> GetAggregatedSummary(
        [FromQuery] List<int> spaceIds,
        [FromQuery] DateTime startTime,
        [FromQuery] DateTime endTime,
        [FromQuery] string groupBy)
        {
            if (spaceIds == null || !spaceIds.Any())
                return BadRequest("Space IDs are required.");
            if (endTime <= startTime)
                return BadRequest("End time must be after start time.");

            var spaceMeterIds = await _context.Meters
                .Where(m => spaceIds.Contains(m.SpaceId))
                .Select(m => m.MeterID)
                .ToListAsync();

            var readings = await _context.MeterReadings
                .Where(r => spaceMeterIds.Contains(r.MeterID) &&
                            r.ReadingTimestamp >= startTime &&
                            r.ReadingTimestamp <= endTime)
                .OrderBy(r => r.ReadingTimestamp)
                .ToListAsync();

            var grouped = groupBy.ToLower() == "monthly"
            ? readings
           .GroupBy(r => new { r.ReadingTimestamp.Year, r.ReadingTimestamp.Month })
           .Select(g => new {
            Period = $"{g.Key.Year}-{g.Key.Month:D2}", 
            TotalEnergy = g.Max(x => x.Value) - g.Min(x => x.Value)
          })
    :       readings
           .GroupBy(r => r.ReadingTimestamp.Date)
           .Select(g => new {
            Period = g.Key.ToString("yyyy-MM-dd"), 
            TotalEnergy = g.Max(x => x.Value) - g.Min(x => x.Value)
        });


            return Ok(grouped);
        }


        [HttpGet("energy-aggregate")]
        public async Task<IActionResult> GetEnergyAggregate(
       [FromQuery] List<int> spaceIds,
       [FromQuery] DateTime startTime,
       [FromQuery] DateTime endTime)
        {
            if (spaceIds == null || !spaceIds.Any())
                return BadRequest("Space IDs are required.");
            if (endTime <= startTime)
                return BadRequest("End time must be after start time.");

            var meterIds = await _context.Meters
                .Where(m => spaceIds.Contains(m.SpaceId))
                .Select(m => m.MeterID)
                .ToListAsync();

            if (!meterIds.Any())
                return NotFound("No meters found for the given spaces.");

            var readings = await _context.MeterReadings
                .Where(r => meterIds.Contains(r.MeterID)
                            && r.ReadingTimestamp >= startTime
                            && r.ReadingTimestamp <= endTime)
                .OrderBy(r => r.ReadingTimestamp)
                .ToListAsync();

            if (!readings.Any())
                return NotFound("No readings found for the given criteria.");

            var totalEnergy = readings
                .GroupBy(r => r.MeterID)
                .Sum(g => g.Max(x => x.Value) - g.Min(x => x.Value));

            string aggregationLevel;
            var duration = (endTime - startTime).TotalDays;

            if (duration <= 2)
                aggregationLevel = "hourly";
            else if (duration <= 60)
                aggregationLevel = "daily";
            else
                aggregationLevel = "monthly";

            var aggregatedList = aggregationLevel switch
            {
                "hourly" => readings
                    .GroupBy(r => new { r.ReadingTimestamp.Date, r.ReadingTimestamp.Hour })
                    .Select(g => new {
                        Period = $"{g.Key.Date:yyyy-MM-dd} {g.Key.Hour:D2}:00",
                        TotalEnergy = g.Max(x => x.Value) - g.Min(x => x.Value)
                    }),

                "monthly" => readings
                    .GroupBy(r => new { r.ReadingTimestamp.Year, r.ReadingTimestamp.Month })
                    .Select(g => new {
                        Period = $"{g.Key.Year}-{g.Key.Month:D2}",
                        TotalEnergy = g.Max(x => x.Value) - g.Min(x => x.Value)
                    }),

                _ => readings
                    .GroupBy(r => r.ReadingTimestamp.Date)
                    .Select(g => new {
                        Period = g.Key.ToString("yyyy-MM-dd"),
                        TotalEnergy = g.Max(x => x.Value) - g.Min(x => x.Value)
                    })
            };

            return Ok(new
            {
                totalEnergy,
                aggregationLevel,
                aggregatedList
            });
        }

    }
}

