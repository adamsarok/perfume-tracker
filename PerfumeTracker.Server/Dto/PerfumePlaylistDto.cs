namespace PerfumeTracker.Server.Dto;
public record PerfumePlaylistDto(string Name, ICollection<PerfumeDto> Perfumes, DateTime CreatedAt, DateTime? UpdatedAt);

