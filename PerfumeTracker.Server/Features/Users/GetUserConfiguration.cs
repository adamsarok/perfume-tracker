using PerfumeTracker.Server.Features.Users;
using System.Security.Claims;

namespace PrfumeTracker.Server.Features.Users;

public record GetUserConfigurationQuery() : IQuery<GetUserConfigurationResponse>;
public class GetUserConfiguration : ICarterModule {
	public void AddRoutes(IEndpointRouteBuilder app) {
		app.MapGet("/api/identity/user-configuration", async (ISender sender) => {
			return await sender.Send(new GetUserConfigurationQuery());
		});
	}
}
public record GetUserConfigurationResponse(bool InviteOnlyRegistration);
public class GetUserConfigurationHandler(IConfiguration configuration) : IQueryHandler<GetUserConfigurationQuery, GetUserConfigurationResponse> {
	public async Task<GetUserConfigurationResponse> Handle(GetUserConfigurationQuery request, CancellationToken cancellationToken) {
		var userConfig = new UserConfiguration(configuration);
		return new GetUserConfigurationResponse(userConfig.InviteOnlyRegistration);
	}
}