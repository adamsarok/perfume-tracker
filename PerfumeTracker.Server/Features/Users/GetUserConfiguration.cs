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
		var userConfig = new UserConfiguration(configuration);
		var response = new GetUserConfigurationResponse(userConfig.InviteOnlyRegistration);
		return await Task.FromResult(response);
	}
}