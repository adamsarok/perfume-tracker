
namespace PerfumeTracker.Server.Repo;
public class PerfumeRepo(PerfumeTrackerContext context, SettingsRepo settingsRepo, IMediator mediator) {
	public record class PerfumeAddedEvent(Perfume Perfume) : INotification;
	public record class PerfumeUpdatedEvent(Perfume Perfume) : INotification;
	public record class PerfumeTagsAddedEvent() : INotification;
	public async Task<List<PerfumeWithWornStatsDto>> GetPerfumesWithWorn(string? fulltext = null) {
		var settings = await settingsRepo.GetSettingsOrDefault("DEFAULT"); //TODO implement when multi user is needed
		return await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => string.IsNullOrWhiteSpace(fulltext)
				|| p.FullText.Matches(EF.Functions.PlainToTsQuery($"{fulltext}:*"))
				|| p.PerfumeTags.Any(pt => EF.Functions.ILike(pt.Tag.TagName, fulltext))
				)
			.Select(p => MapToPerfumeWithWornStatsDto(p, settings))
			.AsSplitQuery()
			.AsNoTracking()
			.ToListAsync();
	}
	public async Task<PerfumeWithWornStatsDto?> GetPerfume(int id) {
		var settings = await settingsRepo.GetSettingsOrDefault("DEFAULT"); //TODO implement when multi user is needed
		var p = await context
			.Perfumes
			.Include(x => x.PerfumeEvents)
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.Where(p => p.Id == id)
			.Select(p => MapToPerfumeWithWornStatsDto(p, settings))
			.AsSplitQuery()
			.AsNoTracking()
			.FirstOrDefaultAsync();
		return p ?? throw new NotFoundException();
	}
	private static PerfumeWithWornStatsDto MapToPerfumeWithWornStatsDto(Perfume p, Settings settings) {
		decimal burnRatePerYearMl = 0;
		decimal yearsLeft = 0;
		p.MlLeft = Math.Max(0, p.PerfumeEvents.Sum(e => e.AmountMl));
		var worns = p.PerfumeEvents.Where(x => x.Type == PerfumeWorn.PerfumeEventType.Worn).ToList();
		if (p.MlLeft > 0 && worns.Any()) {
			var firstWorn = worns.Min(x => x.CreatedAt);
			var daysSinceFirstWorn = (DateTime.UtcNow - firstWorn).TotalDays;
			if (daysSinceFirstWorn >= 30 && worns.Count > 1) { //otherwise prediction will be inaccurate
				var spraysPerYear = 365 * (decimal)worns.Count / (decimal)(DateTime.UtcNow - firstWorn).TotalDays;
				var sprayAmountMl = settings.SprayAmountForBottleSize(p.Ml);
				if (sprayAmountMl > 0) {
					burnRatePerYearMl = spraysPerYear * settings.SprayAmountForBottleSize(p.Ml);
					yearsLeft = p.MlLeft / burnRatePerYearMl;
				}
			}
		}
		return new PerfumeWithWornStatsDto(
				 new PerfumeDto(
					p.Id,
					p.House,
					p.PerfumeName,
					p.Rating,
					p.Notes,
					p.Ml,
					p.MlLeft,
					p.ImageObjectKey,
					p.Autumn,
					p.Spring,
					p.Summer,
					p.Winter,
					p.PerfumeTags.Select(tag => new TagDto(tag.Tag.TagName, tag.Tag.Color, tag.Tag.Id)).ToList()
				  ),
				  worns.Any() ? worns.Count : 0,
				  worns.Any() ? worns.Max(x => x.CreatedAt) : null,
				  burnRatePerYearMl,
				  yearsLeft
				  );
	}
	public async Task<PerfumeDto> AddPerfume(PerfumeDto Dto) {
		var perfume = Dto.Adapt<Perfume>();
		if (perfume == null) throw new InvalidOperationException("Perfume mapping failed");
		context.Perfumes.Add(perfume);
		await context.SaveChangesAsync();
		foreach (var tag in Dto.Tags) {
			context.PerfumeTags.Add(new PerfumeTag() {
				PerfumeId = perfume.Id,
				TagId = tag.Id,
			});
		}
		if (Dto.MlLeft > 0) {
			context.PerfumeEvents.Add(new PerfumeWorn() {
				AmountMl = Dto.MlLeft,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				Perfume = perfume,
				Type = PerfumeWorn.PerfumeEventType.Added,
				UpdatedAt = DateTime.UtcNow
			});
		}
		await context.SaveChangesAsync();
		await mediator.Publish(new PerfumeAddedEvent(perfume));
		return perfume.Adapt<PerfumeDto>();
	}
	public async Task DeletePerfume(int id) {
		var perfume = await context.Perfumes.FindAsync(id);
		if (perfume == null) throw new NotFoundException();
		context.Perfumes.Remove(perfume);
		await context.SaveChangesAsync();
	}
	public async Task<PerfumeDto> UpdatePerfume(int id, PerfumeDto Dto) {
		var perfume = Dto.Adapt<Perfume>();
		if (perfume == null || id != perfume.Id) {
			throw new NotFoundException();
		}
		var find = await context
			.Perfumes
			.Include(x => x.PerfumeTags)
			.ThenInclude(x => x.Tag)
			.FirstOrDefaultAsync(x => x.Id == perfume.Id);
		if (find == null) throw new NotFoundException();

		context.Entry(find).CurrentValues.SetValues(perfume);
		var ayo = context.PerfumeEvents.Where(x => x.PerfumeId == perfume.Id).ToList();
		var mlLeftInDb = context.PerfumeEvents.Where(x => x.PerfumeId == perfume.Id).Sum(s => s.AmountMl);
		if (Dto.MlLeft != mlLeftInDb) {
			context.PerfumeEvents.Add(new PerfumeWorn() {
				AmountMl = Dto.MlLeft - mlLeftInDb,
				CreatedAt = DateTime.UtcNow,
				EventDate = DateTime.UtcNow,
				PerfumeId = perfume.Id,
				Type = PerfumeWorn.PerfumeEventType.Adjusted,
				UpdatedAt = DateTime.UtcNow
			});
		}
		await UpdateTags(Dto, find);
		await mediator.Publish(new PerfumeUpdatedEvent(perfume));
		return find.Adapt<PerfumeDto>();
	}

	public async Task<PerfumeDto> UpdatePerfumeImageGuid(ImageGuidDto Dto) {
		var find = await context.Perfumes.FindAsync(Dto.ParentObjectId);
		if (find == null) throw new NotFoundException();
		find.ImageObjectKey = Dto.ImageObjectKey;
		await context.SaveChangesAsync();
		return find.Adapt<PerfumeDto>();
	}

	private async Task UpdateTags(PerfumeDto Dto, Perfume? find) {
		bool tagsAdded = false;
		if (find == null) return;
		var tagsInDB = find.PerfumeTags
			.Select(x => x.Tag)
			.Select(x => x.TagName)
			.ToList();
		foreach (var remove in find.PerfumeTags.Where(x => !Dto.Tags.Select(x => x.TagName).Contains(x.Tag.TagName))) {
			context.PerfumeTags.Remove(remove);
		}
		if (Dto.Tags != null && Dto.Tags.Any()) {
			foreach (var add in Dto.Tags.Where(x => !tagsInDB.Contains(x.TagName))) {
				context.PerfumeTags.Add(new PerfumeTag() {
					PerfumeId = find.Id,
					TagId = add.Id //TODO: this is not good, tag ID is coming back from client side
				});
				tagsAdded = true;
			}
		}
		await context.SaveChangesAsync();
		if (tagsAdded) {
			await mediator.Publish(new PerfumeTagsAddedEvent());
		}
	}
}
