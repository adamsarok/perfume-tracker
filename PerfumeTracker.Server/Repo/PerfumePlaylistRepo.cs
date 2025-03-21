namespace PerfumeTracker.Server.Repo;
public class PerfumePlaylistRepo(PerfumetrackerContext context) {
	public async Task<List<PerfumePlaylistDto>> GetPerfumePlaylists() {
		return await context
			.PerfumePlayLists
			//.Include(x => x.Perfumes)
			.AsSplitQuery()
			.AsNoTracking()
			.ProjectToType<PerfumePlaylistDto>()
			.ToListAsync();
	}
	public async Task<PerfumePlaylistDto?> GetPerfumePlaylist(string name) {
		var p = await context
			.PerfumePlayLists
			.Include(x => x.Perfumes)
			.AsSplitQuery()
			.AsNoTracking()
			//.ProjectToType<PerfumePlaylistDto>() of course ProjectTo does not work...
			.FirstOrDefaultAsync(x => x.Name == name);
		if (p == null) throw new NotFoundException("PerfumePlaylist", name ?? "");
		return p.Adapt<PerfumePlaylistDto>();
	}

	public async Task<PerfumePlaylistDto> AddPerfumePlaylist(PerfumePlaylistDto Dto) {
		var perfumePlaylist = Dto.Adapt<PerfumePlayList>();
		if (perfumePlaylist == null) throw new InvalidOperationException("Perfume playlist mapping failed");
		perfumePlaylist.Created_At = DateTime.UtcNow;
		context.PerfumePlayLists.Add(perfumePlaylist);
		foreach (var p in perfumePlaylist.Perfumes) context.Entry(p).State = EntityState.Unchanged;
		await context.SaveChangesAsync();
		return perfumePlaylist.Adapt<PerfumePlaylistDto>();
	}
	public async Task DeletePerfumePlaylist(string name) {
		var perfumePlayList = await context.PerfumePlayLists.FindAsync(name);
		if (perfumePlayList == null) throw new NotFoundException();
		context.PerfumePlayLists.Remove(perfumePlayList);
		await context.SaveChangesAsync();
	}
	public async Task<PerfumePlaylistDto> UpdatePerfumePlaylist(PerfumePlaylistDto Dto) {
		var perfumePlaylist = Dto.Adapt<PerfumePlayList>();
		if (perfumePlaylist == null) throw new NotFoundException();
		var find = await context
			.PerfumePlayLists
			.FirstOrDefaultAsync(x => x.Name == perfumePlaylist.Name);
		if (find == null) throw new NotFoundException();

		context.Entry(find).CurrentValues.SetValues(perfumePlaylist);
		find.Updated_At = DateTime.UtcNow;
		return find.Adapt<PerfumePlaylistDto>();
	}
}
