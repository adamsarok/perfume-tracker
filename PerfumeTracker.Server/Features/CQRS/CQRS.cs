namespace PerfumeTracker.Server.Features.CQRS;
public interface ICommand : ICommand<Unit> {
}
public interface ICommand<out TResponse> : IRequest<TResponse> {
}
public interface ICommandHandler<in TCommand> : ICommandHandler<TCommand, Unit>
	where TCommand : ICommand<Unit> { }
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
   where TCommand : ICommand<TResponse>
   where TResponse : notnull {

}
public interface IQuery<out TResponse> : IRequest<TResponse>
	where TResponse : notnull {
}
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
	where TQuery : IQuery<TResponse>
	where TResponse : notnull {
}
public interface IUserNotification : INotification {
	Guid UserId { get; }
}