using Dapper;
using Microsoft.Data.Sqlite;
using SharpSevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Starward.Features.Database;

internal static class DatabaseService
{


    private static string _databasePath;


    private static string _connectionString;


    private static Lock _lock = new();


    static DatabaseService()
    {
        SqlMapper.AddTypeHandler(new DapperSqlMapper.DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.TravelersDiaryPrimogemsMonthGroupStatsListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.TrailblazeCalendarMonthDataGroupByListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.StringListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.GameBizHandler());
    }




    public static SqliteConnection CreateConnection()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }



    public static void SetDatabase(string folder)
    {
        if (Directory.Exists(folder))
        {
            _databasePath = Path.GetFullPath(Path.Combine(folder, "StarwardDatabase.db"));
            _connectionString = $"DataSource={_databasePath};";
            InitializeDatabase();
        }
    }



    private static void InitializeDatabase()
    {
        lock (_lock)
        {
            using var con = CreateConnection();
            int version = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
            if (version == 0)
            {
                con.Execute("PRAGMA JOURNAL_MODE = WAL;");
            }
            foreach (var sql in DatabaseSqls.Skip(version))
            {
                con.Execute(sql);
            }
        }
    }



    public static void BackupDatabase(string file)
    {
        using var backupCon = new SqliteConnection($"DataSource={file}; Pooling=False;");
        backupCon.Open();
        using var con = CreateConnection();
        con.Execute("VACUUM;", commandType: CommandType.Text);
        con.BackupDatabase(backupCon);
    }



    public static void AutoBackupToLocalLow()
    {
        try
        {
            string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\DatabaseBackup");
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"StarwardDatbase_AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            string archive = Path.ChangeExtension(file, ".7z");
            string archive_tmp = archive + "_tmp";
            string[] files = Directory.GetFiles(folder, "StarwardDatbase_AutoBackup_*.7z");
            if (files.Length == 0)
            {
                BackupDatabase(file);
                new SharpSevenZipCompressor().CompressFiles(archive_tmp, file);
                File.Move(archive_tmp, archive, true);
                File.Delete(file);
            }
            else
            {
                string last = files.OrderByDescending(File.GetLastWriteTime).First();
                if (DateTime.Now - File.GetLastWriteTime(last) > TimeSpan.FromDays(7))
                {
                    BackupDatabase(file);
                    new SharpSevenZipCompressor().CompressFiles(archive_tmp, file);
                    File.Move(archive_tmp, archive, true);
                    File.Delete(file);
                    File.Delete(last);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private class KVT
    {

        public KVT() { }


        public KVT(string key, string value, DateTime time)
        {
            Key = key;
            Value = value;
            Time = time;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Time { get; set; }
    }





    public static T? GetValue<T>(string key, out DateTime time, T? defaultValue = default)
    {
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return defaultValue;
                }
                return (T?)converter.ConvertFromString(kvt.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        catch
        {
            return defaultValue;
        }
    }



    public static bool TryGetValue<T>(string key, out T? result, out DateTime time, T? defaultValue = default)
    {
        result = defaultValue;
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return false;
                }
                result = (T?)converter.ConvertFromString(kvt.Value);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }



    public static void SetValue<T>(string key, T value, DateTime? time = null)
    {
        try
        {
            using var con = CreateConnection();
            con.Execute("INSERT OR REPLACE INTO KVT (Key, Value, Time) VALUES (@Key, @Value, @Time);", new KVT(key, value?.ToString() ?? "", time ?? DateTime.Now));

        }
        catch { }
    }





    #region Database Structure


    private static readonly List<string> DatabaseSqls = [Sql_v1, Sql_v2, Sql_v3, Sql_v4, Sql_v5, Sql_v6, Sql_v7, Sql_v8, Sql_v9, Sql_v10, Sql_v11, Sql_v12, Sql_v13, Sql_v14];


    private const string Sql_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS KVT
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL,
            Time  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GameAccount
        (
            SHA256  TEXT    NOT NULL PRIMARY KEY,
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Url     TEXT    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (GameBiz, Uid)
        );

        CREATE TABLE IF NOT EXISTS GenshinGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_Id ON GenshinGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_RankType ON GenshinGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_GachaType ON GenshinGachaItem (GachaType);

        CREATE TABLE IF NOT EXISTS StarRailGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            GachaId   INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_Id ON StarRailGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_RankType ON StarRailGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_GachaType ON StarRailGachaItem (GachaType);

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v2 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS PlayTimeItem
        (
            TimeStamp INTEGER PRIMARY KEY,
            GameBiz   INTEGER NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL,
            Message   TEXT
        );
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem(GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem(Pid);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_State ON PlayTimeItem(State);

        PRAGMA USER_VERSION = 2;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v3 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GameAccount_dg_tmp
        (
            SHA256  TEXT    NOT NULL,
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (SHA256, GameBiz)
        );

        INSERT INTO GameAccount_dg_tmp(SHA256, GameBiz, Uid, Name, Value, Time)
        SELECT SHA256, GameBiz, Uid, Name, Value, Time
        FROM GameAccount;

        DROP TABLE IF EXISTS GameAccount;
        ALTER TABLE GameAccount_dg_tmp RENAME TO GameAccount;
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        PRAGMA USER_VERSION = 3;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v4 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS Setting
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT
        );

        CREATE TABLE IF NOT EXISTS GameRecordUser
        (
            Uid       INTEGER NOT NULL,
            IsHoyolab INTEGER NOT NULL,
            Nickname  TEXT,
            Avatar    TEXT,
            Introduce TEXT,
            Gender    INTEGER NOT NULL,
            AvatarUrl TEXT,
            Pendant   TEXT,
            Cookie    TEXT,
            PRIMARY KEY (Uid, IsHoyolab)
        );

        CREATE TABLE IF NOT EXISTS GameRecordRole
        (
            Uid        INTEGER NOT NULL,
            GameBiz    TEXT    NOT NULL,
            Nickname   TEXT,
            Level      INTEGER NOT NULL,
            Region     TEXT    NOT NULL,
            RegionName TEXT,
            Cookie     TEXT,
            PRIMARY KEY (Uid, GameBiz)
        );
        CREATE INDEX IF NOT EXISTS IX_GameRecordRole_GameBiz ON GameRecordRole (GameBiz);

        CREATE TABLE IF NOT EXISTS SpiralAbyssInfo
        (
            Uid              INTEGER NOT NULL,
            ScheduleId       INTEGER NOT NULL,
            StartTime        TEXT    NOT NULL,
            EndTime          TEXT    NOT NULL,
            TotalBattleCount INTEGER NOT NULL,
            TotalWinCount    INTEGER NOT NULL,
            MaxFloor         TEXT,
            TotalStar        INTEGER NOT NULL,
            Value            TEXT    NOT NULL,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_SpiralAbyssInfo_ScheduleId ON SpiralAbyssInfo (ScheduleId);

        CREATE TABLE IF NOT EXISTS TravelersDiaryMonthData
        (
            Uid                   INTEGER NOT NULL,
            Year                  INTEGER NOT NULL,
            Month                 INTEGER NOT NULL,
            CurrentPrimogems      INTEGER NOT NULL,
            CurrentMora           INTEGER NOT NULL,
            LastPrimogems         INTEGER NOT NULL,
            LastMora              INTEGER NOT NULL,
            CurrentPrimogemsLevel INTEGER NOT NULL,
            PrimogemsChangeRate   INTEGER NOT NULL,
            MoraChangeRate        INTEGER NOT NULL,
            PrimogemsGroupBy      TEXT    NOT NULL,
            PRIMARY KEY (Uid, Year, Month)
        );

        CREATE TABLE IF NOT EXISTS TravelersDiaryAwardItem
        (
            Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Uid        INTEGER NOT NULL,
            Year       INTEGER NOT NULL,
            Month      INTEGER NOT NULL,
            Type       INTEGER NOT NULL,
            ActionId   INTEGER NOT NULL,
            ActionName TEXT,
            Time       TEXT    NOT NULL,
            Number     INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_TravelersDiaryAwardItem_Uid_Year_Month ON TravelersDiaryAwardItem (Uid, Year, Month);
        CREATE INDEX IF NOT EXISTS IX_TravelersDiaryAwardItem_Type ON TravelersDiaryAwardItem (Type);
        CREATE INDEX IF NOT EXISTS IX_TravelersDiaryAwardItem_Time ON TravelersDiaryAwardItem (Time);

        CREATE TABLE IF NOT EXISTS ForgottenHallInfo
        (
            Uid        INTEGER NOT NULL,
            ScheduleId INTEGER NOT NULL,
            BeginTime  TEXT    NOT NULL,
            EndTime    TEXT    NOT NULL,
            StarNum    INTEGER NOT NULL,
            MaxFloor   TEXT,
            BattleNum  INTEGER NOT NULL,
            HasData    INTEGER NOT NULL,
            Value      TEXT    NOT NULL,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_ForgottenHallInfo_ScheduleId ON ForgottenHallInfo (ScheduleId);

        CREATE TABLE IF NOT EXISTS SimulatedUniverseRecord
        (
            Uid           INTEGER NOT NULL,
            ScheduleId    INTEGER NOT NULL,
            FinishCount   INTEGER NOT NULL,
            ScheduleBegin TEXT    NOT NULL,
            ScheduleEnd   TEXT    NOT NULL,
            HasData       INTEGER NOT NULL,
            Value         TEXT    NOT NULL,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_SimulatedUniverseRecord_ScheduleId ON SimulatedUniverseRecord (ScheduleId);

        CREATE TABLE IF NOT EXISTS TrailblazeCalendarMonthData
        (
            Uid              INTEGER NOT NULL,
            Month            TEXT    NOT NULL,
            CurrentHcoin     INTEGER NOT NULL,
            CurrentRailsPass INTEGER NOT NULL,
            LastHcoin        INTEGER NOT NULL,
            LastRailsPass    INTEGER NOT NULL,
            HcoinRate        INTEGER NOT NULL,
            RailsRate        INTEGER NOT NULL,
            GroupBy          TEXT    NOT NULL,
            PRIMARY KEY (Uid, Month)
        );

        CREATE TABLE IF NOT EXISTS TrailblazeCalendarDetailItem
        (
            Id         INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Uid        INTEGER NOT NULL,
            Month      TEXT    NOT NULL,
            Type       INTEGER NOT NULL,
            Action     TEXT    NOT NULL,
            ActionName TEXT    NOT NULL,
            Time       TEXT    NOT NULL,
            Number     INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_TrailblazeCalendarDetailItem_Uid_Month ON TrailblazeCalendarDetailItem (Uid, Month);
        CREATE INDEX IF NOT EXISTS IX_TrailblazeCalendarDetailItem_Type ON TrailblazeCalendarDetailItem (Type);
        CREATE INDEX IF NOT EXISTS IX_TrailblazeCalendarDetailItem_Time ON TrailblazeCalendarDetailItem (Time);

        PRAGMA USER_VERSION = 4;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v5 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GenshinGachaInfo
        (
            Id          INTEGER NOT NULL PRIMARY KEY,
            Name        TEXT,
            Icon        TEXT,
            Element     INTEGER NOT NULL,
            Level       INTEGER NOT NULL,
            CatId       INTEGER NOT NULL,
            WeaponCatId INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaInfo_Name ON GenshinGachaInfo (Name);

        CREATE TABLE IF NOT EXISTS StarRailGachaInfo
        (
            ItemId         INTEGER NOT NULL PRIMARY KEY,
            ItemName       TEXT,
            IconUrl        TEXT,
            DamageType     INTEGER NOT NULL,
            Rarity         INTEGER NOT NULL,
            AvatarBaseType INTEGER NOT NULL,
            WikiUrl        TEXT,
            IsSystem       INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaInfo_Name ON StarRailGachaInfo (ItemName);

        PRAGMA USER_VERSION = 5;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v6 = """
        BEGIN TRANSACTION;

        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_Name ON GenshinGachaItem (Name);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_ItemId ON GenshinGachaItem (ItemId);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_Name ON StarRailGachaItem (Name);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_ItemId ON StarRailGachaItem (ItemId);

        PRAGMA USER_VERSION = 6;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v7 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GenshinQueryItem
        (
            Uid      INTEGER NOT NULL,
            Id       INTEGER NOT NULL,
            AddNum   INTEGER NOT NULL,
            Reason   TEXT,
            DateTime TEXT,
            Type     INTEGER NOT NULL,
            Icon     TEXT,
            Level    INTEGER NOT NULL,
            Quality  INTEGER NOT NULL,
            Name     TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinQueryItem_Id ON GenshinQueryItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GenshinQueryItem_AddNum ON GenshinQueryItem (AddNum);
        CREATE INDEX IF NOT EXISTS IX_GenshinQueryItem_Reason ON GenshinQueryItem (Reason);
        CREATE INDEX IF NOT EXISTS IX_GenshinQueryItem_DateTime ON GenshinQueryItem (DateTime);
        CREATE INDEX IF NOT EXISTS IX_GenshinQueryItem_Type ON GenshinQueryItem (Type);

        CREATE TABLE IF NOT EXISTS StarRailQueryItem
        (
            Id              INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
            Uid             INTEGER NOT NULL,
            Type            INTEGER NOT NULL,
            Action          TEXT,
            AddNum          INTEGER NOT NULL,
            Time            TEXT,
            RelicName       TEXT,
            RelicLevel      INTEGER NOT NULL,
            RelicRarity     INTEGER NOT NULL,
            EquipmentName   TEXT,
            EquipmentLevel  INTEGER NOT NULL,
            EquipmentRarity INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailQueryItem_Uid ON StarRailQueryItem (Uid);
        CREATE INDEX IF NOT EXISTS IX_StarRailQueryItem_Type ON StarRailQueryItem (Type);
        CREATE INDEX IF NOT EXISTS IX_StarRailQueryItem_Action ON StarRailQueryItem (Action);
        CREATE INDEX IF NOT EXISTS IX_StarRailQueryItem_AddNum ON StarRailQueryItem (AddNum);
        CREATE INDEX IF NOT EXISTS IX_StarRailQueryItem_Time ON StarRailQueryItem (Time);

        PRAGMA USER_VERSION = 7;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v8 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS PureFictionInfo
        (
            Uid        INTEGER NOT NULL,
            ScheduleId INTEGER NOT NULL,
            BeginTime  TEXT    NOT NULL,
            EndTime    TEXT    NOT NULL,
            StarNum    INTEGER NOT NULL,
            MaxFloor   TEXT,
            BattleNum  INTEGER NOT NULL,
            HasData    INTEGER NOT NULL,
            Value      TEXT    NOT NULL,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_PureFictionInfo_ScheduleId ON PureFictionInfo (ScheduleId);

        PRAGMA USER_VERSION = 8;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v9 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS ZZZGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_Id ON ZZZGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_RankType ON ZZZGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_GachaType ON ZZZGachaItem (GachaType);

        CREATE TABLE IF NOT EXISTS ApocalypticShadowInfo
        (
            Uid           INTEGER NOT NULL,
            ScheduleId    INTEGER NOT NULL,
            BeginTime     TEXT    NOT NULL,
            EndTime       TEXT    NOT NULL,
            UpperBossIcon TEXT    NOT NULL,
            LowerBossIcon TEXT    NOT NULL,
            StarNum       INTEGER NOT NULL,
            MaxFloor      TEXT,
            BattleNum     INTEGER NOT NULL,
            HasData       INTEGER NOT NULL,
            Value         TEXT    NOT NULL,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_ApocalypticShadowInfo_ScheduleId ON ApocalypticShadowInfo (ScheduleId);

        PRAGMA USER_VERSION = 9;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v10 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS ZZZQueryItem
        (
            Id           INTEGER NOT NULL,
            Uid          INTEGER NOT NULL,
            Type         INTEGER NOT NULL,
            Reason       TEXT,
            AddNum       INTEGER NOT NULL,
            DateTime     TEXT,
            EquipName    TEXT,
            EquipRarity  INTEGER NOT NULL,
            EquipLevel   INTEGER NOT NULL,
            WeaponName   TEXT,
            WeaponRarity INTEGER NOT NULL,
            WeaponLevel  INTEGER NOT NULL,
            ClientIp     TEXT,
            ActionName   TEXT,
            CardType     INTEGER NOT NULL,
            ItemName     TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_ZZZQueryItem_Id ON ZZZQueryItem (Id);
        CREATE INDEX IF NOT EXISTS IX_ZZZQueryItem_Type ON ZZZQueryItem (Type);
        CREATE INDEX IF NOT EXISTS IX_ZZZQueryItem_Reason ON ZZZQueryItem (Reason);
        CREATE INDEX IF NOT EXISTS IX_ZZZQueryItem_AddNum ON ZZZQueryItem (AddNum);
        CREATE INDEX IF NOT EXISTS IX_ZZZQueryItem_DateTime ON ZZZQueryItem (DateTime);

        CREATE TABLE IF NOT EXISTS ImaginariumTheaterInfo
        (
            Uid          INTEGER NOT NULL,
            ScheduleId   INTEGER NOT NULL,
            StartTime    TEXT,
            EndTime      TEXT,
            DifficultyId INTEGER NOT NULL,
            MaxRoundId   INTEGER NOT NULL,
            Heraldry     INTEGER NOT NULL,
            MedalNum     INTEGER NOT NULL,
            Value        TEXT,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_ImaginariumTheaterInfo_ScheduleId ON ImaginariumTheaterInfo (ScheduleId);

        PRAGMA USER_VERSION = 10;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v11 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS InterKnotReportSummary
        (
            Uid       INTEGER NOT NULL,
            DataMonth TEXT    NOT NULL,
            Value     TEXT,
            PRIMARY KEY (Uid, DataMonth)
        );

        CREATE TABLE IF NOT EXISTS InterKnotReportDetailItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            DataMonth TEXT    NOT NULL,
            DataType  TEXT    NOT NULL,
            Action    TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            Number       INTEGER NOT NULL,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_InterKnotReportDetailItem_DataMonth ON InterKnotReportDetailItem (DataMonth);
        CREATE INDEX IF NOT EXISTS IX_InterKnotReportDetailItem_DataType ON InterKnotReportDetailItem (DataType);
        CREATE INDEX IF NOT EXISTS IX_InterKnotReportDetailItem_Time ON InterKnotReportDetailItem (Time);

        PRAGMA USER_VERSION = 11;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v12 = """
        BEGIN TRANSACTION;

        UPDATE PlayTimeItem SET GameBiz = 'hk4e_cn' WHERE GameBiz IN (11, 13);
        UPDATE PlayTimeItem SET GameBiz = 'hk4e_global' WHERE GameBiz = 12;
        UPDATE PlayTimeItem SET GameBiz = 'hk4e_bilibili' WHERE GameBiz = 14;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_cn' WHERE GameBiz = 21;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_global' WHERE GameBiz = 22;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_bilibili' WHERE GameBiz = 24;
        UPDATE PlayTimeItem SET GameBiz = 'bh3_cn' WHERE GameBiz = 31;
        UPDATE PlayTimeItem SET GameBiz = 'bh3_global' WHERE GameBiz IN (32, 33, 34, 35, 36);
        UPDATE PlayTimeItem SET GameBiz = 'nap_cn' WHERE GameBiz = 41;
        UPDATE PlayTimeItem SET GameBiz = 'nap_global' WHERE GameBiz = 42;
        UPDATE PlayTimeItem SET GameBiz = 'nap_bilibili' WHERE GameBiz = 44;

        CREATE TABLE IF NOT EXISTS PlayTimeItem_dg_tmp
        (
            TimeStamp INTEGER PRIMARY KEY,
            GameBiz   TEXT    NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL,
            Message   TEXT
        );
        INSERT INTO PlayTimeItem_dg_tmp(TimeStamp, GameBiz, Pid, State, CursorPos, Message) SELECT TimeStamp, GameBiz, Pid, State, CursorPos, Message FROM PlayTimeItem;
        DROP TABLE IF EXISTS PlayTimeItem;
        ALTER TABLE PlayTimeItem_dg_tmp RENAME TO PlayTimeItem;
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem(GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem(Pid);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_State ON PlayTimeItem(State);

        DROP TABLE IF EXISTS GachaLogUrl;
        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            GameBiz TEXT    NOT NULL,
            Uid     INTEGER NOT NULL,
            Url     TEXT    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (GameBiz, Uid)
        );

        ALTER TABLE GameRecordRole ADD COLUMN HeadIcon TEXT;

        PRAGMA USER_VERSION = 12;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v13 = """
        BEGIN TRANSACTION;

        DROP TABLE IF EXISTS GameAccount;
        CREATE TABLE IF NOT EXISTS GameAccount
        (
            SHA256  TEXT    NOT NULL,
            GameBiz INTEGER NOT NULL,
            Uid     TEXT    NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (SHA256, GameBiz)
        );
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        CREATE TABLE IF NOT EXISTS ZZZGachaInfo
        (
            Id          INTEGER NOT NULL PRIMARY KEY,
            Name        TEXT,
            Icon        TEXT,
            Rarity      INTEGER NOT NULL,
            ElementType INTEGER NOT NULL,
            Profession  INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaInfo_Name ON ZZZGachaInfo (Name);

        PRAGMA USER_VERSION = 13;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v14 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS ShiyuDefenseInfo
        (
            Uid            INTEGER NOT NULL,
            ScheduleId     INTEGER NOT NULL,
            BeginTime      TEXT    NOT NULL,
            EndTime        TEXT    NOT NULL,
            HasData        INTEGER NOT NULL,
            MaxRating      Text,
            MaxRatingTimes INTEGER NOT NULL,
            MaxLayer       INTEGER NOT NULL,
            Value          TEXT,
            PRIMARY KEY (Uid, ScheduleId)
        );
        CREATE INDEX IF NOT EXISTS IX_ShiyuDefenseInfo_ScheduleId ON ShiyuDefenseInfo (ScheduleId);

        CREATE TABLE IF NOT EXISTS DeadlyAssaultInfo
        (
            Uid            INTEGER NOT NULL,
            ZoneId         INTEGER NOT NULL,
            StartTime      TEXT    NOT NULL,
            EndTime        TEXT    NOT NULL,
            HasData        INTEGER NOT NULL,
            RankPercent    INTEGER NOT NULL,
            TotalScore     INTEGER NOT NULL,
            TotalStar      INTEGER NOT NULL,
            Value          TEXT,
            PRIMARY KEY (Uid, ZoneId)
        );
        CREATE INDEX IF NOT EXISTS IX_DeadlyAssaultInfo_ZoneId ON DeadlyAssaultInfo (ZoneId);

        PRAGMA USER_VERSION = 14;
        COMMIT TRANSACTION;
        """;

    #endregion



}
