﻿namespace Listen2MeRefined.Infrastructure.Mvvm;

using Listen2MeRefined.Infrastructure.Data.EntityFramework;
using Listen2MeRefined.Infrastructure.Data;
using Listen2MeRefined.Infrastructure.Storage;
using Listen2MeRefined.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Listen2MeRefined.Infrastructure.Notifications;

public class StartupManager : IDisposable
{
    private readonly IMediator _mediator;
    private readonly ISettingsManager<AppSettings> _settingsManager;
    private readonly IGlobalHook _globalHook;
    private readonly IFolderScanner _folderScanner;
    private readonly DataContext _dataContext;
    private bool disposedValue;

    public StartupManager(
        ISettingsManager<AppSettings> settingsManager,
        IGlobalHook globalHook,
        IFolderScanner folderScanner,
        DataContext dataContext,
        IMediator mediator)
    {
        _settingsManager = settingsManager;
        _globalHook = globalHook;
        _folderScanner = folderScanner;
        _dataContext = dataContext;
        _mediator = mediator;
    }

    public async Task StartAsync()
    {
        await Task.Run(async () => await _dataContext.Database.MigrateAsync());
        await _mediator.Publish(new FontFamilyChangedNotification(_settingsManager.Settings.FontFamily));

        if (_settingsManager.Settings.ScanOnStartup)
        {
            await Task.Run(async () => await _folderScanner.ScanAllAsync());
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _globalHook.Unregister();
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            disposedValue = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}