namespace PerfumeTracker.Server.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : ICommand<TResponse> { //pretty great to not do this for queries only CRUD

	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken) {
		var context = new ValidationContext<TRequest>(request);
		var validationResults = await Task.WhenAll(validators.Select(x => x.ValidateAsync(context, cancellationToken)));
		var errors = validationResults.Where(x => x.Errors.Count > 0).SelectMany(x => x.Errors).ToList();
		if (errors.Count > 0) throw new ValidationException(errors);
		return await next();
	}
}

