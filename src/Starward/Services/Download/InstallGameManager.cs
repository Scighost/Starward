using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Starward.Services.Download;

internal class InstallGameManager
{


    private readonly ILogger<InstallGameManager> _logger;


    private readonly ConcurrentDictionary<GameBiz, InstallGameService> _services;


    private readonly Stack<InstallGameService> _servicesStack;

    public InstallGameManager(ILogger<InstallGameManager> logger)
    {
        _logger = logger;
        _services = new();
        _servicesStack = new();
    }




    public InstallGameService CurrentInstallService { get; private set; }



    public double Speed { get; set; }



    public double SpeedLimit { get; set; }









    public bool TryGetInstallService(GameBiz gameBiz, [NotNullWhen(true)] out InstallGameService? service)
    {
        return _services.TryGetValue(gameBiz, out service);
    }




    public void AddInstallService(InstallGameService service)
    {
        CurrentInstallService?.Pause();
        _servicesStack.Push(service);
        _services[service.CurrentGameBiz] = service;
        CurrentInstallService = service;
    }








}



public partial class InstallGameStateModel : ObservableObject
{


    private const string PlayGlyph = "\uE768";


    private const string PauseGlyph = "\uE769";



    private InstallGameService _service;


    internal InstallGameStateModel(InstallGameService service)
    {
        _service = service;
        GameBiz = _service.CurrentGameBiz;
        Icon = new GameBizIcon { GameBiz = _service.CurrentGameBiz };
        _service.StateChanged += _service_StateChanged;
        _service.InstallFailed += _service_InstallFailed;
    }



    public GameBiz GameBiz { get; set; }


    public GameBizIcon Icon { get; set; }


    [ObservableProperty]
    private string _StateText;


    [ObservableProperty]
    private string _ButtonGlyph;


    [ObservableProperty]
    private double _Progress;



    private void _service_StateChanged(object? sender, InstallGameState e)
    {
        switch (e)
        {
            case InstallGameState.None:
                StateText = "Paused";
                break;
            case InstallGameState.Download:
                StateText = "Downloading";
                break;
            case InstallGameState.Verify:
                StateText = "Verifying";
                break;
            case InstallGameState.Decompress:
                StateText = "Decompressing";
                break;
            case InstallGameState.Clean:
                break;
            case InstallGameState.Finish:
                StateText = "Finished";
                break;
            case InstallGameState.Error:
                StateText = "Error";
                break;
            default:
                break;
        }
    }


    private void _service_InstallFailed(object? sender, System.Exception e)
    {
        NotificationBehavior.Instance.Error(e, $"Game ({GameBiz.ToGameName()} - {GameBiz.ToGameServer()}) install failed.");
    }




}
