﻿using Microsoft.EntityFrameworkCore;
using SnowflakeItemMaster.Application.Contracts.Persistence;
using System.Linq.Expressions;

namespace SnowflakeItemMaster.Infrastructure.Repositories
{
    public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected RepositoryContext RepositoryContext;

        protected RepositoryBase(RepositoryContext repositoryContext)
        {
            RepositoryContext = repositoryContext;
        }

        public void Create(T entity)
        {
            RepositoryContext.Set<T>().Add(entity);
        }

        public void Delete(T entity)
        {
            RepositoryContext.Set<T>().Remove(entity);
        }

        public IQueryable<T> FindAll(bool trackChanges)
        {
            if (!trackChanges)
            {
                return RepositoryContext.Set<T>().AsNoTracking();
            }

            return RepositoryContext.Set<T>();
        }

        public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges)
        {
            if (!trackChanges)
            {
                return RepositoryContext.Set<T>().Where(expression).AsNoTracking();
            }

            return RepositoryContext.Set<T>().Where(expression);
        }

        public void Update(T entity)
        {
            RepositoryContext.Set<T>().Update(entity);
        }
    }
}