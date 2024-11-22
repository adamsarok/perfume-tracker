using AutoMapper;
using PerfumeTrackerAPI.Models;

namespace PerfumeTrackerAPI.DTO {
    public class AutomapperProfile : Profile {
        public AutomapperProfile() {
            CreateMap<Perfume, PerfumeDTO>().ForMember(d => d.Perfume, o => o.MapFrom(s => s.Perfume1));
            CreateMap<Tag, TagDTO>().ForMember(d => d.Tag, o => o.MapFrom(s => s.Tag1));
        }
    }
}
