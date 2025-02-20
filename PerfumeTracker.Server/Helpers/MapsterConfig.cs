using Mapster;
using PerfumeTrackerAPI.Dto;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.Helpers {
    public static class MapsterConfig {
        public static void RegisterMapsterConfiguration(this IServiceCollection services) {

            var config = TypeAdapterConfig.GlobalSettings;
        }
    }
}
