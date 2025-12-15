using Microsoft.Extensions.Options;

namespace PerfumeTracker.Server.Features.Users;

public record GetUserConfigurationQuery() : IQuery<GetUserConfigurationResponse>;
public class GetUserConfiguration : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/configuration", async (ISender sender, CancellationToken cancellationToken) => {
			return await sender.Send(new GetUserConfigurationQuery(), cancellationToken);
		}).WithTags("Users")
			.WithName("GetConfiguration")
			.AllowAnonymous();
	}
}
public record GetUserConfigurationResponse(bool InviteOnlyRegistration);
public class GetUserConfigurationHandler(IOptions<UserConfiguration> configuration) : IQueryHandler<GetUserConfigurationQuery, GetUserConfigurationResponse> {
	public async Task<GetUserConfigurationResponse> Handle(GetUserConfigurationQuery request, CancellationToken cancellationToken) {
		await Task.CompletedTask; // to suppress non-async warning
		return new GetUserConfigurationResponse(configuration.Value.InviteOnlyRegistration);
	}
}