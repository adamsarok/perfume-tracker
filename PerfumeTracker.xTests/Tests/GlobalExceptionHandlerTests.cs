using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using PerfumeTracker.Server.Features.Common;
using System.Text.Json;

namespace PerfumeTracker.xTests.Tests;

public class GlobalExceptionHandlerTests {

	private readonly GlobalExceptionHandler _handler;
	private readonly DefaultHttpContext _httpContext;

	public GlobalExceptionHandlerTests() {
		var logger = Mock.Of<ILogger<GlobalExceptionHandler>>();
		_handler = new GlobalExceptionHandler(logger);
		_httpContext = new DefaultHttpContext();
		_httpContext.Response.Body = new MemoryStream();
	}

	[Fact]
	public async Task TryHandleAsync_NotFoundException_Returns404() {
		var result = await _handler.TryHandleAsync(_httpContext, new NotFoundException("Not found"), CancellationToken.None);
		Assert.True(result);
		Assert.Equal(404, _httpContext.Response.StatusCode);
		var problem = await ReadProblemDetails();
		Assert.Equal(404, problem.Status);
	}

	[Fact]
	public async Task TryHandleAsync_BadRequestException_Returns400() {
		var result = await _handler.TryHandleAsync(_httpContext, new BadRequestException("Bad request"), CancellationToken.None);
		Assert.True(result);
		Assert.Equal(400, _httpContext.Response.StatusCode);
		var problem = await ReadProblemDetails();
		Assert.Equal(400, problem.Status);
	}

	[Fact]
	public async Task TryHandleAsync_UnauthorizedException_Returns401() {
		var result = await _handler.TryHandleAsync(_httpContext, new UnauthorizedException("Unauthorized"), CancellationToken.None);
		Assert.True(result);
		Assert.Equal(401, _httpContext.Response.StatusCode);
	}

	[Fact]
	public async Task TryHandleAsync_GenericException_Returns500() {
		var result = await _handler.TryHandleAsync(_httpContext, new Exception("Something broke"), CancellationToken.None);
		Assert.True(result);
		Assert.Equal(500, _httpContext.Response.StatusCode);
		var problem = await ReadProblemDetails();
		Assert.Equal(500, problem.Status);
		Assert.NotNull(problem.Extensions["traceId"]);
	}

	private async Task<ProblemDetails> ReadProblemDetails() {
		_httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
		var json = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
		return JsonSerializer.Deserialize<ProblemDetails>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
	}
}
