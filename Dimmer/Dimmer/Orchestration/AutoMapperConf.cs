// In Dimmer.Orchestration/AutoMapperConf.cs

// Assuming other necessary using statements for your ViewModels etc.

using Dimmer.Utilities.TypeConverters;

using static Dimmer.Data.Models.LastFMUser;
using static Dimmer.Data.ModelView.LastFMUserView;

namespace Dimmer.Orchestration;

public static class AutoMapperConf
{
    public static IMapper ConfigureAutoMapper()
    {
        try
        {
            var config = new MapperConfiguration(cfg =>
            {
                // Explicitly add the profile type. 
                // The linker sees 'typeof(DimmerMappingProfile)' and preserves the whole class.
                cfg.AddProfile<DimmerMappingProfile>();
            });

            config.AssertConfigurationIsValid();
            return config.CreateMapper();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);

            throw new InvalidOperationException("AutoMapper configuration failed.", ex);
        }
    }
}


