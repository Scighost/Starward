using Dapper;
using Starward.Core;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace Starward.Features.Database;


internal class DapperSqlMapper
{

    private static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true };


    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override DateTimeOffset Parse(object value)
        {
            if (value is string str)
            {
                return DateTimeOffset.Parse(str);
            }
            else
            {
                return new DateTimeOffset();
            }
        }

        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
        {
            parameter.Value = value.ToString();
        }
    }



    public class TravelersDiaryPrimogemsMonthGroupStatsListHandler : SqlMapper.TypeHandler<List<TravelersDiaryPrimogemsMonthGroupStats>>
    {
        public override List<TravelersDiaryPrimogemsMonthGroupStats> Parse(object value)
        {
            if (value is string str)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return JsonSerializer.Deserialize<List<TravelersDiaryPrimogemsMonthGroupStats>>(str, JsonSerializerOptions)!;
                }
            }
            return new();
        }

        public override void SetValue(IDbDataParameter parameter, List<TravelersDiaryPrimogemsMonthGroupStats>? value)
        {
            parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
        }
    }


    public class TrailblazeCalendarMonthDataGroupByListHandler : SqlMapper.TypeHandler<List<TrailblazeCalendarMonthDataGroupBy>>
    {
        public override List<TrailblazeCalendarMonthDataGroupBy> Parse(object value)
        {
            if (value is string str)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return JsonSerializer.Deserialize<List<TrailblazeCalendarMonthDataGroupBy>>(str, JsonSerializerOptions)!;
                }
            }
            return new();
        }

        public override void SetValue(IDbDataParameter parameter, List<TrailblazeCalendarMonthDataGroupBy>? value)
        {
            parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
        }
    }


    public class StringListHandler : SqlMapper.TypeHandler<List<string>>
    {
        public override List<string> Parse(object value)
        {
            if (value is string str)
            {
                if (!string.IsNullOrWhiteSpace(str))
                {
                    return JsonSerializer.Deserialize<List<string>>(str)!;
                }
            }
            return new();
        }

        public override void SetValue(IDbDataParameter parameter, List<string>? value)
        {
            parameter.Value = JsonSerializer.Serialize(value, JsonSerializerOptions);
        }
    }


    public class GameBizHandler : SqlMapper.TypeHandler<GameBiz>
    {
        public override GameBiz Parse(object value)
        {
            return new GameBiz(value as string);
        }

        public override void SetValue(IDbDataParameter parameter, GameBiz value)
        {
            parameter.Value = value.Value;
        }
    }


}


