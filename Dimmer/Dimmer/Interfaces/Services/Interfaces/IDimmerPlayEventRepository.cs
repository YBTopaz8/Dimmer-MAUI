using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services.Interfaces;

// This interface is SPECIFICALLY for DimmerPlayEvent operations.
// It inherits all the common methods from the generic IRepository.
public interface IDimmerPlayEventRepository : IRepository<DimmerPlayEvent>
{
    /// <summary>
    /// Efficiently retrieves all play events within a specified date range.
    /// The filtering is done at the database level for performance.
    /// </summary>
    /// <param name="startDate">The optional start date of the range (inclusive).</param>
    /// <param name="endDate">The optional end date of the range (exclusive).</param>
    /// <returns>A collection of play events that fall within the date range.</returns>
    Task<IReadOnlyCollection<DimmerPlayEvent>> GetEventsInDateRangeAsync(DateTimeOffset? startDate, DateTimeOffset? endDate);

    /// <summary>
    /// Efficiently retrieves all play events for a single song.
    /// </summary>
    Task<IReadOnlyCollection<DimmerPlayEvent>> GetEventsForSongAsync(ObjectId songId);
}