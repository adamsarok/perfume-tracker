﻿using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PerfumeTracker.Server.Features.Auth;
namespace PerfumeTracker.xTests;
public class TestScope : IDisposable {
	private bool _disposed;
	public PerfumeTrackerContext PerfumeTrackerContext { get; init; }
	public IServiceScope ServiceScope { get; init; }
	public TestScope(WebApplicationFactory<Program> factory, ITenantProvider tenantProvider) {
		ServiceScope = factory.Services.CreateScope();
		PerfumeTrackerContext = ServiceScope.ServiceProvider.GetRequiredService<PerfumeTrackerContext>();
		if (!PerfumeTrackerContext.Database.GetDbConnection().Database.ToLower().Contains("test")) {
			throw new Exception("Live database connected!");
		}
		PerfumeTrackerContext.TenantProvider = tenantProvider;
	}
	public void Dispose() {
		Dispose(true);
		GC.SuppressFinalize(this);
	}
	protected virtual void Dispose(bool disposing) {
		if (!_disposed) {
			if (disposing) {
				PerfumeTrackerContext?.Dispose();
				ServiceScope?.Dispose();
			}
			_disposed = true;
		}
	}
	~TestScope() {
		Dispose(false);
	}
}