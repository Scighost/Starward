using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

// POST https://hyp-api.mihoyo.com/hyp/hyp-connect/api/reserveGame
public class ReserveGameRequest
{
    [JsonPropertyName("launcher_id")]
    public string LauncherId { get; set; }

    [JsonPropertyName("game_id")]
    public string GameId { get; set; }

    [JsonPropertyName("auto_download")]
    public bool AutoDownload { get; set; }

    // RESERVATION_SOURCE_WEBSITE
    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }

    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("mid")]
    public string Mid { get; set; }
}



public class ReserveGamenResponse
{
    [JsonPropertyName("reserved")]
    public bool Reserved { get; set; }

    [JsonPropertyName("auto_download")]
    public bool AutoDownload { get; set; }
}


// POST https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getUserGameReservations
public class GetUserGameReservationsRequest
{
    [JsonPropertyName("launcher_id")]
    public string LauncherId { get; set; }

    [JsonPropertyName("game_ids")]
    public List<string> GameIds { get; set; }

    [JsonPropertyName("stoken")]
    public string Stoken { get; set; }

    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("mid")]
    public string Mid { get; set; }
}


public class GetUserGameReservationsResponse
{
    [JsonPropertyName("reservations")]
    public List<UserGameReservations> Reservations { get; set; }
}


public class UserGameReservations
{
    [JsonPropertyName("game_id")]
    public string GameId { get; set; }

    [JsonPropertyName("website_reserved")]
    public bool WebsiteReserved { get; set; }

    [JsonPropertyName("reserved")]
    public bool Reserved { get; set; }

    [JsonPropertyName("auto_download")]
    public bool AutoDownload { get; set; }
}