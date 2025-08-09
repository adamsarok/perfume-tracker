namespace PerfumeTracker.Server.Features.Users;
public record GetUserConfigurationQuery() : IQuery<GetUserConfigurationResponse>;
public class GetUserConfiguration : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/configuration", async (ISender sender) => {
			return await sender.Send(new GetUserConfigurationQuery());
		}).WithTags("Users")
			.WithName("GetConfiguration")
			.AllowAnonymous();
}
}
public record GetUserConfigurationResponse(bool InviteOnlyRegistration);
public class GetUserConfigurationHandler(IConfiguration configuration) : IQueryHandler<GetUserConfigurationQuery, GetUserConfigurationResponse> {
	public async Task<GetUserConfigurationResponse> Handle(GetUserConfigurationQuery request, CancellationToken cancellationToken) {
		await Task.CompletedTask; // to suppress non-async warning
		var userConfig = new UserConfiguration(configuration);
		return new GetUserConfigurationResponse(userConfig.InviteOnlyRegistration);
	}
}