using Microsoft.Extensions.Logging;
using Starward.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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




    public InstallGameService CurrentInstallService;



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





    public void Pause()
    {

    }




    public void Continue()
    {

    }






}
