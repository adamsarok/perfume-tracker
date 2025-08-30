using PerfumeTracker.Server.Features.R2;
using System.IO;

namespace PerfumeTracker.Server.Services.R2;

public static class SeedDemoImages {
	public static async Task<List<Guid>> SeedDemoImagesAsync(UploadImageHandler handler) {
		List<Guid> imageGuids = new List<Guid>();
		foreach (var imagePath in Directory.GetFiles("images")) {
			var fileInfo = new FileInfo(imagePath);
			await using var stream = fileInfo.OpenRead();
			var guid = await handler.UploadImage(stream, CancellationToken.None);
			imageGuids.Add(guid);
		}
		return imageGuids;
	}
}
