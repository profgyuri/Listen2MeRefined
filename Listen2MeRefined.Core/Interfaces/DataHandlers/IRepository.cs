﻿using Listen2MeRefined.Core.Models;

namespace Listen2MeRefined.Core.Interfaces.DataHandlers;

/// <summary>
///     Interface to provide CRUD operations.
/// </summary>
/// <typeparam name="T">The the of entity to make changes for in the database.</typeparam>
public interface IRepository<T> : IDataSaver<T>, IDataReader<T>, IDataUpdater<T>, IDataRemover<T>
    where T: Model
{ }