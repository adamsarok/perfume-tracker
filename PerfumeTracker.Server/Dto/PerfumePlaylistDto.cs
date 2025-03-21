namespace PerfumeTracker.Server.Dto;
public record PerfumePlaylistDto(string Name, ICollection<PerfumeDto> Perfumes, DateTime Created_At, DateTime? Updated_At);

