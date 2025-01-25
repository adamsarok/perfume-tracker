using Mapster;
using PerfumeTracker.Server.DTO;
using PerfumeTracker.Server.Models;

namespace PerfumeTracker.Server.Helpers {
    public static class MapsterConfig {
        public static void RegisterMapsterConfiguration(this IServiceCollection services) {

            var config = TypeAdapterConfig.GlobalSettings;

            //config.NewConfig< Perfume, PerfumeDTO>()
            //    .Map(d => d.Perfume, s => s.Perfume1);
        }
    }
}
