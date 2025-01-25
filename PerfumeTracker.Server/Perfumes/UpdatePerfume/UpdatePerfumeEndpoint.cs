using Carter;
using Mapster;
using MediatR;
using PerfumeTracker.Server.DTO;

namespace PerfumeTracker.Server.Perfumes.UpdatePerfume {
	public record UpdatePerfumeRequest(PerfumeDTO Perfume);
	public record UpdatePerfumeResponse(bool IsSuccess);
	public class UpdatePerfumeEndpoint : ICarterModule {
		public void AddRoutes(IEndpointRouteBuilder app) {
			app.MapPut("/api/perfumes/", async (UpdatePerfumeRequest request, ISender sender) => {
				var command = request.Adapt<UpdatePerfumeCommand>();
				var result = await sender.Send(command);
				var response = result.Adapt<UpdatePerfumeResponse>();
				return Results.Ok(response);
			}).WithTags("Perfumes")
				.WithName("UpdatePerfume");
		}
	}
}
