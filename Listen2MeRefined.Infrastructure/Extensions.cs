﻿using Ardalis.GuardClauses;
using Listen2MeRefined.Infrastructure.Data.EntityFramework;

namespace Listen2MeRefined.Infrastructure;

internal static class Extensions
{
    private static readonly object _contextLock = new();

    internal static void NotExistingFile(
        this IGuardClause clause,
        string path,
        string parameterName)
    {
        if (!File.Exists(path))
        {
            throw new ArgumentException($"There is no file under this path: {path}", parameterName);
        }
    }

    internal static void AddRange<T>(
        this ICollection<T> collection,
        IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    internal static void AddIfDoesNotExist<T>(
        this DataContext context,
        T item)
        where T : class
    {
        if (!context.Set<T>().Any(x => x.Equals(item)))
        {
            context.Set<T>().Add(item);
        }
    }

    internal static void AddIfDoesNotExist<T>(
        this DataContext context,
        IEnumerable<T> items)
        where T : class
    {
        foreach (var item in items)
        {
            context.AddIfDoesNotExist(item);
        }
    }

    internal static async Task AddIfDoesNotExistAsync<T>(
        this DataContext context,
        IEnumerable<T> items)
        where T : class
    {
        await Task.Run(() =>
        {
            foreach (var item in items)
            {
                Monitor.Enter(_contextLock);
                try
                {
                    context.AddIfDoesNotExist(item);
                }
                finally
                {
                    Monitor.Exit(_contextLock);
                }
            }
        }).ConfigureAwait(false);
    }
}