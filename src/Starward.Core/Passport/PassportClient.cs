using Starward.Core.DeviceFp;
using Starward.Core.GameRecord;
using System.Net;

namespace Starward.Core.Passport;

public class PassportClient
{

    private readonly HttpClient _httpClient;


    public PassportClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }



    public string Language { get; set => field = LanguageUtil.FilterLanguage(value); }

    public string DeviceId { get; set; }

    public string DeviceFp { get; set; }



    private void AddPassportHeaders(HttpRequestMessage request, PassportUserInfo userInfo)
    {
        request.Headers.Add("x-rpc-app_id", userInfo.IsGlobalServer ? "ddxf6vlr1reo" : "ddxf5qt290cg");
        request.Headers.Add("x-rpc-client_type", "3");
        request.Headers.Add("x-rpc-game_biz", userInfo.IsGlobalServer ? "hyp_global" : "hyp_cn");
        request.Headers.Add("x-rpc-sdk_version", userInfo.IsGlobalServer ? "2.55.3" : "2.55.2");
        request.Headers.Add("x-rpc-device_id", DeviceId);
        request.Headers.Add("x-rpc-device_fp", DeviceFp);
        request.Headers.Add("x-rpc-device_model", "x86_64");
        request.Headers.Add("x-rpc-device_name", Environment.MachineName);
        request.Headers.Add("x-rpc-device_os", Environment.OSVersion.Version.Build > 22000 ? "Windows 11 Version 2009" : "Windows 10 Version 1909");
        request.Headers.Add("x-rpc-language", Language);
    }



    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) where T : class
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var wrapper = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), PassportJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<T>;
        if (wrapper is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        return wrapper.Data;
    }



    public async Task<GameRecordUser> GetBbsUserInfoAsync(ServerRegion region, long aid, CancellationToken cancellation = default)
    {
        string url = region switch
        {
            ServerRegion.HoYoverse => $"https://bbs-api-os.hoyolab.com/community/user/wapi/getUserFullInfo?uid={aid}",
            ServerRegion.miHoYo => $"https://bbs-api.miyoushe.com/user/wapi/getUserFullInfo?uid={aid}",
            _ => throw new NotSupportedException($"Server region '{region}' is not supported.")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Referrer = new Uri("https://" + new Uri(url).Host);
        return (await SendAsync<GameRecordUserWrapper>(request, cancellation)).User;
    }





    #region Passport



    /// <summary>
    /// 校验会话 stoken
    /// </summary>
    public async Task<SessionVerifyResult> VerifySessionAsync(PassportUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request;
        if (userInfo.IsGlobalServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api-sg.hoyoverse.com/account/ma-passport/token/verifySToken")
            {
                Content = JsonContent.Create(new VerifySTokenRequest
                {
                    Mid = userInfo.Mid,
                    Stoken = userInfo.Stoken,
                }, PassportJsonContext.Default.VerifySTokenRequest),
            };
        }
        else if (userInfo.IsChinaServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-session/app/verify")
            {
                Content = JsonContent.Create(new SessionVerifyRequest
                {
                    Token = new PassportToken { TokenType = TokenType.SToken, Token = userInfo.Stoken },
                    Refresh = true,
                    Mid = userInfo.Mid,
                }, PassportJsonContext.Default.SessionVerifyRequest),
            };
        }
        else
        {
            throw new NotSupportedException($"Passport region '{userInfo.Region}' is not supported.");
        }
        AddPassportHeaders(request, userInfo);
        return await SendAsync<SessionVerifyResult>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 登出
    /// </summary>
    public async Task LogoutAsync(PassportUserInfo userInfo, CancellationToken cancellationToken = default)
    {
        var payload = new LogoutRequest
        {
            Mid = userInfo.Mid,
            Token = new PassportToken { TokenType = TokenType.SToken, Token = userInfo.Stoken },
        };
        HttpRequestMessage request;
        if (userInfo.IsGlobalServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api-sg.hoyoverse.com/account/ma-passport/api/logout")
            {
                Content = JsonContent.Create(payload, PassportJsonContext.Default.LogoutRequest),
            };
        }
        else if (userInfo.IsChinaServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-session/app/logout")
            {
                Content = JsonContent.Create(payload, PassportJsonContext.Default.LogoutRequest),
            };
        }
        else
        {
            throw new NotSupportedException($"Passport region '{userInfo.Region}' is not supported.");
        }
        AddPassportHeaders(request, userInfo);
        await SendAsync<LogoutResult>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 跨端免密登录
    /// </summary>
    public async Task<string> CreateCrossLoginTokenAsync(PassportUserInfo userInfo, string dest, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request;
        if (userInfo.IsGlobalServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api-sg.hoyoverse.com/account/ma-passport/token/getCrossTokenUrlBySToken")
            {
                Content = JsonContent.Create(new GetCrossTokenUrlBySTokenRequest
                {
                    Mid = userInfo.Mid,
                    Stoken = userInfo.Stoken,
                    Dest = dest,
                }, PassportJsonContext.Default.GetCrossTokenUrlBySTokenRequest),
            };
        }
        else if (userInfo.IsChinaServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-session/app/createCrossLoginToken")
            {
                Content = JsonContent.Create(new CreateCrossLoginTokenRequest
                {
                    Stoken = userInfo.Stoken,
                    Mid = userInfo.Mid,
                    Aid = userInfo.Aid.ToString(),
                    Dest = dest,
                }, PassportJsonContext.Default.CreateCrossLoginTokenRequest),
            };
        }
        else
        {
            throw new NotSupportedException($"Passport region '{userInfo.Region}' is not supported.");
        }
        AddPassportHeaders(request, userInfo);
        var result = await SendAsync<CrossLoginTokenResult>(request, cancellationToken).ConfigureAwait(false);
        return result.Url;
    }


    /// <summary>
    /// 游戏授权登录
    /// </summary>
    public async Task<AuthTicketResult> CreateAuthTicketAsync(PassportUserInfo userInfo, string gameBiz, CancellationToken cancellationToken = default)
    {
        HttpRequestMessage request;
        if (userInfo.IsGlobalServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api-sg.hoyoverse.com/account/ma-verifier/api/createAuthTicketBySToken")
            {
                Content = JsonContent.Create(new CreateAuthTicketBySTokenRequest
                {
                    Mid = userInfo.Mid,
                    Stoken = userInfo.Stoken,
                    BizName = gameBiz,
                }, PassportJsonContext.Default.CreateAuthTicketBySTokenRequest),
            };
        }
        else if (userInfo.IsChinaServer)
        {
            request = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-verifier/app/createAuthTicketByGameBiz")
            {
                Content = JsonContent.Create(new CreateAuthTicketByGameBizRequest
                {
                    GameBiz = gameBiz,
                    Stoken = userInfo.Stoken,
                    Uid = userInfo.Aid,
                    Mid = userInfo.Mid,
                }, PassportJsonContext.Default.CreateAuthTicketByGameBizRequest),
            };
        }
        else
        {
            throw new NotSupportedException($"Passport region '{userInfo.Region}' is not supported.");
        }
        AddPassportHeaders(request, userInfo);
        return await SendAsync<AuthTicketResult>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 国服更换 token
    /// </summary>
    /// <param name="userInfo"></param>
    /// <param name="destToken"></param>
    /// <param name="androidDevice"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<ExchangeTokenResponse> ExchangeTokenAsync(PassportUserInfo userInfo, TokenType destToken, AndroidDeviceInfo? androidDevice, CancellationToken cancellation = default)
    {
        if (!userInfo.IsChinaServer)
        {
            throw new NotSupportedException($"Server region '{userInfo.Region}' is not supported.");
        }
        var request = ExchangeTokenRequest.FromStoken(userInfo.Stoken, userInfo.Mid);
        request.DestTokenType = destToken;
        var message = new HttpRequestMessage(HttpMethod.Post, "https://passport-api.mihoyo.com/account/ma-cn-session/app/exchange")
        {
            Content = JsonContent.Create(request, PassportJsonContext.Default.ExchangeTokenRequest)
        };
        message.Headers.Add("DS", DynamicSecret.CreatePassportDS(userInfo.Region, request));
        if (androidDevice is null)
        {
            AddPassportHeaders(message, userInfo);
        }
        else
        {
            message.Headers.Add("x-rpc-app_id", "ddxf5qt290cg");
            message.Headers.Add("x-rpc-client_type", "2");
            message.Headers.Add("x-rpc-device_id", androidDevice.ExtFields.AndroidId);
            message.Headers.Add("x-rpc-device_fp", androidDevice.DeviceFp);
            message.Headers.Add("x-rpc-device_name", androidDevice.ExtFields.DeviceName.Replace(' ', '+'));
            message.Headers.Add("x-rpc-device_model", androidDevice.ExtFields.Model);
            message.Headers.Add("x-rpc-sys_version", androidDevice.ExtFields.OsVersion);
            message.Headers.Add("x-rpc-game_biz", "bbs_cn");
            message.Headers.Add("x-rpc-app_version", GameRecordClientBase.HyperionAppVersion);
            message.Headers.Add("x-rpc-sdk_version", GameRecordClientBase.HyperionSdkVersion);
            message.Headers.Add("x-rpc-lifecycle_id", DynamicSecret.LifeCycleId);
            message.Headers.Add("x-rpc-account_version", GameRecordClientBase.HyperionSdkVersion);
        }
        return await SendAsync<ExchangeTokenResponse>(message, cancellation);
    }


    /// <summary>
    /// 国际服更换 token
    /// </summary>
    /// <param name="userInfo"></param>
    /// <param name="destTokenTypes"></param>
    /// <param name="androidDevice"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<GetBySTokenResponse> GetBySTokenAsync(PassportUserInfo userInfo, IReadOnlyCollection<TokenType> destTokenTypes, AndroidDeviceInfo? androidDevice, CancellationToken cancellation = default)
    {
        if (!userInfo.IsGlobalServer)
        {
            throw new NotSupportedException($"Server region '{userInfo.Region}' is not supported.");
        }
        var request = new GetBySTokenRequest { DestTokenTypes = [.. destTokenTypes] };
        var message = new HttpRequestMessage(HttpMethod.Post, "https://sg-public-api.hoyoverse.com/account/ma-passport/token/getBySToken")
        {
            Content = JsonContent.Create(request, PassportJsonContext.Default.GetBySTokenRequest)
        };
        message.Headers.Add("DS", DynamicSecret.CreatePassportDS(userInfo.Region, request));
        message.Headers.Add("Cookie", $"stoken={userInfo.Stoken};mid={userInfo.Mid};stuid={userInfo.Aid}");
        if (androidDevice is null)
        {
            AddPassportHeaders(message, userInfo);
        }
        else
        {
            message.Headers.Add("x-rpc-app_id", "ddxf6vlr1reo");
            message.Headers.Add("x-rpc-client_type", "2");
            message.Headers.Add("x-rpc-game_biz", "bbs_oversea");
            message.Headers.Add("x-rpc-sdk_version", GameRecordClientBase.HoyolabSdkVersion);
            message.Headers.Add("x-rpc-app_version", GameRecordClientBase.HoyolabAppVersion);
            message.Headers.Add("x-rpc-language", Language);
            message.Headers.Add("x-rpc-package_name", "com.mihoyo.hoyolab");
            message.Headers.Add("x-rpc-age_gate", "true");
            message.Headers.Add("x-rpc-age_gate_eu", "true");
            message.Headers.Add("x-rpc-age_gate_br", "true");
            if (androidDevice?.ExtFields is { } extFields)
            {
                message.Headers.Add("x-rpc-device_id", extFields.AndroidId);
                message.Headers.Add("x-rpc-device_fp", androidDevice.DeviceFp);
                message.Headers.Add("x-rpc-device_name", extFields.DeviceName.Replace(' ', '+'));
                message.Headers.Add("x-rpc-device_model", extFields.Model);
                message.Headers.Add("x-rpc-device_os", extFields.OsVersion);
            }
            else
            {
                message.Headers.Add("x-rpc-device_id", DeviceId);
                message.Headers.Add("x-rpc-device_fp", DeviceFp);
                message.Headers.Add("x-rpc-device_name", Environment.MachineName);
                message.Headers.Add("x-rpc-device_model", "x86_64");
                message.Headers.Add("x-rpc-device_os", Environment.OSVersion.Version.Build > 22000 ? "Windows 11 Version 2009" : "Windows 10 Version 1909");
            }
        }
        return await SendAsync<GetBySTokenResponse>(message, cancellation);
    }


    /// <summary>
    /// 获取账号下已绑定的所有游戏角色
    /// </summary>
    /// <param name="userInfo"></param>
    /// <param name="deviceInfo"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public async Task<List<GameRecordRole>> GetUserGameRolesAsync(PassportUserInfo userInfo, AndroidDeviceInfo? deviceInfo, CancellationToken cancellation = default)
    {
        string url = userInfo.Region switch
        {
            ServerRegion.HoYoverse => $"https://api-account-os.hoyolab.com/binding/api/getUserGameRolesBySToken",
            ServerRegion.miHoYo => $"https://api-takumi.miyoushe.com/binding/api/getUserGameRolesByStoken",
            _ => throw new NotSupportedException($"Server region '{userInfo.Region}' is not supported.")
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("x-rpc-client_type", "2");
        request.Headers.Add("Cookie", $"stuid={userInfo.Aid};stoken={userInfo.Stoken};mid={userInfo.Mid};");
        if (userInfo.IsGlobalServer)
        {
            request.Headers.Add("x-rpc-app_id", "ddxf6vlr1reo");
            request.Headers.Add("DS", DynamicSecret.CreateAppDS2(userInfo.Region, url));
            request.Headers.Add("x-rpc-app_version", GameRecordClientBase.HoyolabAppVersion);
            request.Headers.Add("x-rpc-language", Language);
            request.Headers.Add("x-rpc-uid", userInfo.Aid.ToString());
            request.Headers.Add("User-Agent", GameRecordClientBase.HoyolabUAContent);
            request.Headers.Referrer = new Uri("https://app.hoyolab.com");
        }
        else
        {
            request.Headers.Add("x-rpc-app_id", "ddxf5qt290cg");
            request.Headers.Add("DS", DynamicSecret.CreateAppDS1(userInfo.Region));
            request.Headers.Add("x-rpc-app_version", GameRecordClientBase.HyperionAppVersion);
            request.Headers.Add("User-Agent", GameRecordClientBase.HyperionUAContent);
            request.Headers.Referrer = new Uri("https://app.mihoyo.com");
        }

        if (deviceInfo?.ExtFields is { } extFields)
        {
            request.Headers.Add("x-rpc-sys_version", extFields.OsVersion);
            request.Headers.Add("x-rpc-channel", userInfo.IsGlobalServer ? "google" : "miyousheluodi");
            request.Headers.Add("x-rpc-device_id", extFields.AndroidId);
            request.Headers.Add("x-rpc-device_fp", deviceInfo.DeviceFp);
            request.Headers.Add("x-rpc-device_name", $"{extFields.Manufacturer} {extFields.Model}");
            request.Headers.Add("x-rpc-device_model", extFields.Model);
        }
        var data = await SendAsync<GameRecordRoleWrapper>(request, cancellation).ConfigureAwait(false);
        List<GameRecordRole> list = data.List ?? [];
        foreach (var item in list)
        {
            item.Aid = userInfo.Aid;
        }
        return list;
    }


    #endregion


}
