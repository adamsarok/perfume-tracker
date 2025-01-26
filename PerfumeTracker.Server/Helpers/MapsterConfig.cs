using Mapster;
using PerfumeTrackerAPI.DTO;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Helpers {
    public static class MapsterConfig {
        public static void RegisterMapsterConfiguration(this IServiceCollection services) {

            var config = TypeAdapterConfig.GlobalSettings;

            //config.NewConfig< Perfume, PerfumeDTO>()
            //    .Map(d => d.Perfume, s => s.Perfume1);
        }
    }
}
