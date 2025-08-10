using Dimmer.Interfaces.Services.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.Interfaces.Services;
internal class DimmerPlayEventRepository : RealmCoreRepo<DimmerPlayEvent>, IDimmerPlayEventRepository
{
    private readonly IRealmFactory _realmFactory;

    public DimmerPlayEventRepository(IRealmFactory factory) : base(factory)
    {
        _realmFactory= factory;
    }

    public async Task<IReadOnlyCollection<DimmerPlayEvent>> GetEventsInDateRangeAsync(DateTimeOffset? startDate, DateTimeOffset? endDate)
    {

        using var realm = _realmFactory.GetRealmInstance();


        var query = realm.All<DimmerPlayEvent>();

        if (startDate.HasValue)
        {
            query = query.Where(e => e.EventDate >= startDate.Value);
        }
        if (endDate.HasValue)
        {
            query = query.Where(e => e.EventDate < endDate.Value);
        }


        return await Task.FromResult(query.ToList().AsReadOnly());
    }

    public async Task<IReadOnlyCollection<DimmerPlayEvent>> GetEventsForSongAsync(ObjectId songId)
    {
        using var realm = _realmFactory.GetRealmInstance();
        var query = realm.All<DimmerPlayEvent>().Where(e => e.SongId == songId);
        return await Task.FromResult(query.ToList().AsReadOnly());
    }
}
