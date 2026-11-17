using MediatR;
using Tessera.SharedKernel.Results;

namespace Tessera.Application.Messaging;

/// <summary>A request that changes state. Commands return a <see cref="Result{TValue}"/>, never throw for business failures.</summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>A side-effect free request that reads state.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;

/// <summary>Queries implementing this are served from the distributed cache by <c>QueryCachingBehavior</c>.</summary>
public interface ICachedQuery
{
    string CacheKey { get; }

    TimeSpan? Expiration { get; }
}
