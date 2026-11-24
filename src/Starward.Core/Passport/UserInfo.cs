using System.Text.Json.Serialization;

namespace Starward.Core.Passport;


/// <summary>
/// 账号信息，对应各接口返回的 user_info。
/// </summary>
public class UserInfo
{

    /// <summary>
    /// 账号数字 ID，接口返回的是字符串。
    /// </summary>
    [JsonPropertyName("aid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long Aid { get; set; }


    /// <summary>
    /// 账号字符串标识，形如 0t14vgp8et_mhy。
    /// </summary>
    [JsonPropertyName("mid")]
    public string Mid { get; set; }


    /// <summary>
    /// 通行证账号名，可能为空。
    /// </summary>
    [JsonPropertyName("account_name")]
    public string AccountName { get; set; }


    /// <summary>
    /// 邮箱（已掩码）。
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }


    /// <summary>
    /// 邮箱是否已验证：1 = 已验证。
    /// </summary>
    [JsonPropertyName("is_email_verify")]
    public int IsEmailVerify { get; set; }


    /// <summary>
    /// 手机区号。
    /// </summary>
    [JsonPropertyName("area_code")]
    public string AreaCode { get; set; }


    /// <summary>
    /// 手机号（已掩码）。
    /// </summary>
    [JsonPropertyName("mobile")]
    public string Mobile { get; set; }


    /// <summary>
    /// 安全手机区号。
    /// </summary>
    [JsonPropertyName("safe_area_code")]
    public string SafeAreaCode { get; set; }


    /// <summary>
    /// 安全手机号（已掩码）。
    /// </summary>
    [JsonPropertyName("safe_mobile")]
    public string SafeMobile { get; set; }


    /// <summary>
    /// 实名姓名（已掩码）。
    /// </summary>
    [JsonPropertyName("realname")]
    public string Realname { get; set; }


    /// <summary>
    /// 身份证号（已掩码）。
    /// </summary>
    [JsonPropertyName("identity_code")]
    public string IdentityCode { get; set; }


    /// <summary>
    /// 待换绑手机区号。
    /// </summary>
    [JsonPropertyName("rebind_area_code")]
    public string RebindAreaCode { get; set; }


    /// <summary>
    /// 待换绑手机号。
    /// </summary>
    [JsonPropertyName("rebind_mobile")]
    public string RebindMobile { get; set; }


    /// <summary>
    /// 待换绑手机时间。
    /// </summary>
    [JsonPropertyName("rebind_mobile_time")]
    public string RebindMobileTime { get; set; }


    /// <summary>
    /// 第三方绑定列表。
    /// </summary>
    [JsonPropertyName("links")]
    public List<UserLink> Links { get; set; }


    /// <summary>
    /// 国家 / 地区。
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; }


    /// <summary>
    /// 密码修改时间（秒级时间戳字符串，0 表示未设置）。
    /// </summary>
    [JsonPropertyName("password_time")]
    public string PasswordTime { get; set; }


    /// <summary>
    /// 是否成年：1 = 成年。
    /// </summary>
    [JsonPropertyName("is_adult")]
    public int IsAdult { get; set; }


    /// <summary>
    /// 未掩码邮箱。
    /// </summary>
    [JsonPropertyName("unmasked_email")]
    public string UnmaskedEmail { get; set; }


    /// <summary>
    /// 未掩码邮箱类型。
    /// </summary>
    [JsonPropertyName("unmasked_email_type")]
    public int UnmaskedEmailType { get; set; }

}



/// <summary>
/// 账号绑定的第三方信息，对应 user_info.links 的元素。
/// </summary>
public class UserLink
{

    /// <summary>
    /// 第三方类型：tp = TapTap，ap = Apple，sn = Sony，st = Steam，hw = 华为，nt = 非独占 Steam。
    /// </summary>
    [JsonPropertyName("thirdparty")]
    public string ThirdParty { get; set; }


    /// <summary>
    /// 第三方唯一标识。
    /// </summary>
    [JsonPropertyName("union_id")]
    public string UnionId { get; set; }


    /// <summary>
    /// 第三方昵称（已掩码）。
    /// </summary>
    [JsonPropertyName("nickname")]
    public string Nickname { get; set; }


    /// <summary>
    /// 第三方邮箱。
    /// </summary>
    [JsonPropertyName("email")]
    public string Email { get; set; }


    /// <summary>
    /// 子类型。
    /// </summary>
    [JsonPropertyName("subType")]
    public string SubType { get; set; }


    /// <summary>
    /// 子标识。
    /// </summary>
    [JsonPropertyName("sub_union_id")]
    public string SubUnionId { get; set; }


    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; }


    [JsonPropertyName("refreshToken")]
    public string RefreshToken { get; set; }


    [JsonPropertyName("scope")]
    public string Scope { get; set; }


    [JsonPropertyName("tokenExpireAt")]
    public string TokenExpireAt { get; set; }

}
