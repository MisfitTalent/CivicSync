using System.Collections;
using System.Linq.Expressions;
using CivicSync.Node.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Linq;

namespace CivicSync.Node.Api.Tests.TestSupport;

internal sealed class TestRepository<TEntity> : IRepository<TEntity, Guid>
    where TEntity : class, IEntity<Guid>
{
    private readonly CivicSyncDbContext _dbContext;
    private readonly DbSet<TEntity> _dbSet;

    public TestRepository(CivicSyncDbContext dbContext)
    {
        _dbContext = dbContext;
        _dbSet = dbContext.Set<TEntity>();
        EntityName = typeof(TEntity).Name;
    }

    public string? EntityName { get; set; }
    public string ProviderName => "EntityFrameworkCore.InMemory";
    public bool? IsChangeTrackingEnabled => true;
    public IAsyncQueryableExecuter AsyncExecuter => throw new NotSupportedException("Tests use EF Core async LINQ directly.");

    public IQueryable<TEntity> WithDetails()
    {
        return CreateLocalQuery();
    }

    public IQueryable<TEntity> WithDetails(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return CreateLocalQuery();
    }

    public Task<IQueryable<TEntity>> WithDetailsAsync()
    {
        return Task.FromResult(WithDetails());
    }

    public Task<IQueryable<TEntity>> WithDetailsAsync(params Expression<Func<TEntity, object>>[] propertySelectors)
    {
        return Task.FromResult(WithDetails(propertySelectors));
    }

    public Task<IQueryable<TEntity>> GetQueryableAsync()
    {
        return Task.FromResult(CreateLocalQuery());
    }

    public async Task<TEntity> InsertAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task InsertManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddRangeAsync(entities, cancellationToken);
    }

    public Task<TEntity> UpdateAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public Task UpdateManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.UpdateRange(entities);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entity = await FindAsync(id, includeDetails: false, cancellationToken);
        if (entity is not null)
        {
            await DeleteAsync(entity, autoSave, cancellationToken);
        }
    }

    public async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entities = CreateLocalQuery().Where(predicate).ToList();
        await DeleteManyAsync(entities, autoSave, cancellationToken);
    }

    public Task DeleteDirectAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return DeleteAsync(predicate, autoSave: true, cancellationToken);
    }

    public Task DeleteManyAsync(IEnumerable<TEntity> entities, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        _dbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public async Task DeleteManyAsync(IEnumerable<Guid> ids, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var entities = CreateLocalQuery().Where(item => ids.Contains(item.Id)).ToList();
        await DeleteManyAsync(entities, autoSave, cancellationToken);
    }

    public Task<TEntity?> FindAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateLocalQuery().SingleOrDefault(item => item.Id == id));
    }

    public Task<TEntity?> FindAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateLocalQuery().SingleOrDefault(predicate));
    }

    public async Task<TEntity> GetAsync(Guid id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await FindAsync(id, includeDetails, cancellationToken)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} was not found.");
    }

    public async Task<TEntity> GetAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        return await FindAsync(predicate, includeDetails, cancellationToken)
            ?? throw new InvalidOperationException($"{typeof(TEntity).Name} was not found.");
    }

    public Task<List<TEntity>> GetListAsync(bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateLocalQuery().ToList());
    }

    public Task<List<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> predicate, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateLocalQuery().Where(predicate).ToList());
    }

    public Task<List<TEntity>> GetPagedListAsync(int skipCount, int maxResultCount, string sorting, bool includeDetails = false, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CreateLocalQuery().Skip(skipCount).Take(maxResultCount).ToList());
    }

    public Task<long> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult((long)CreateLocalQuery().Count());
    }

    private IQueryable<TEntity> CreateLocalQuery()
    {
        return new TestAsyncEnumerable<TEntity>(_dbSet.Local.ToList());
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }

    private sealed class TestAsyncQueryProvider<T> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<T>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object? Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var expectedResultType = typeof(TResult).GetGenericArguments()[0];
            var executionResult = typeof(IQueryProvider)
                .GetMethod(nameof(IQueryProvider.Execute), 1, [typeof(Expression)])!
                .MakeGenericMethod(expectedResultType)
                .Invoke(this, [expression]);

            return (TResult)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(expectedResultType)
                .Invoke(null, [executionResult])!;
        }
    }
}
