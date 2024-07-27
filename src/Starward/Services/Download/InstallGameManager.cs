using Starward.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Starward.Services.Download;

internal class InstallGameManager
{


    private readonly ConcurrentDictionary<GameBiz, InstallGameStateModel> _services = new();


    public InstallGameManager()
    {
        _services = new();
    }


    private static InstallGameManager _instance;
    public static InstallGameManager Instance => _instance ??= new();




    public double Speed { get; set; }



    public double SpeedLimit { get; set; }



    public event EventHandler<InstallGameStateModel> InstallTaskAdded;



    public event EventHandler<InstallGameStateModel> InstallTaskRemoved;






    public bool TryGetInstallService(GameBiz gameBiz, [NotNullWhen(true)] out InstallGameService? service)
    {
        if (_services.TryGetValue(gameBiz, out var model))
        {
            service = model.Service;
            return true;
        }
        else
        {
            service = null;
            return false;
        }
    }




    public void AddInstallService(InstallGameService service)
    {
        var model = new InstallGameStateModel(service);
        _services[service.CurrentGameBiz] = model;
        model.InstallFinished -= Model_InstallFinished;
        model.InstallFinished += Model_InstallFinished;
        model.InstallFailed -= Model_InstallFailed;
        model.InstallFailed += Model_InstallFailed;
        model.InstallCanceled -= Model_InstallCanceled;
        model.InstallCanceled += Model_InstallCanceled;
        InstallTaskAdded?.Invoke(this, model);
    }



    private void Model_InstallFinished(object? sender, EventArgs e)
    {
        if (sender is InstallGameStateModel model)
        {
            model.Service.ClearState();
            _services.TryRemove(model.GameBiz, out _);
            model.InstallFinished -= Model_InstallFinished;
            model.InstallFailed -= Model_InstallFailed;
            model.InstallCanceled -= Model_InstallCanceled;
            InstallTaskRemoved?.Invoke(this, model);
        }
    }



    private void Model_InstallFailed(object? sender, EventArgs e)
    {
        if (sender is InstallGameStateModel model)
        {

        }
    }


    private void Model_InstallCanceled(object? sender, EventArgs e)
    {
        if (sender is InstallGameStateModel model)
        {
            model.Service.Pause();
            model.Service.ClearState();
            _services.TryRemove(model.GameBiz, out _);
            model.InstallFinished -= Model_InstallFinished;
            model.InstallFailed -= Model_InstallFailed;
            model.InstallCanceled -= Model_InstallCanceled;
            InstallTaskRemoved?.Invoke(this, model);
        }
    }



}
