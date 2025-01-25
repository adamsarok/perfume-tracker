using MediatR;
using System.Diagnostics;

namespace PerfumeTracker.Server.Behaviors {
	public class LoggingBehavior<TRequest, TResponse>(
		ILogger<LoggingBehavior<TRequest, TResponse>> logger)
		: IPipelineBehavior<TRequest, TResponse>
		where TRequest : notnull, IRequest<TResponse>
		where TResponse : notnull {
		public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
			var tReq = typeof(TRequest).Name;
			var tResp = typeof(TResponse).Name;
			logger.LogInformation("[START] Handle request={Request} response={Response} requestData={RequestData}",
				tReq, tResp, request);
			var sw = Stopwatch.StartNew();
			var response = await next();
			sw.Stop();
			if (sw.ElapsedMilliseconds > 3000) {
				logger.LogWarning("[PERFORMANCE] Request {Request} took {Elapsed} ms",
					tReq, sw.ElapsedMilliseconds);
			}
			logger.LogInformation("[END] handled {Request} with {Response}", tReq, tResp);
			return response;
		}
	}
}
