English | [简体中文](./Configuration.zh-CN.md) | [Tiếng Việt](./Configuration.vi-VN.md) | [日本語](./Configuration.ja-JP.md) | [ภาษาไทย](./Configuration.th-TH.md)

# Application Configuration

Starting with version 0.8.0, Starward no longer uses the registry to store configuration, but instead uses file and database, making it easier to migrate the overall application between devices. However, the registry will still be used when the file structure does not meet the following conditions:

```
│ config.ini
│ starward.exe
│ version.ini
│
└─ app-0.8.0
   │ Starward.exe
   ...
```

Don't worry, files downloaded from GitHub Release will definitely satisfy this condition, only when you pull the code and debug it locally will you use the registry to store the configuration.

## config.ini

The `config.ini` file contains only two setting items:

```ini
# Whether to enable console output logging, True/False.
EnableConsole=False
# The application window size, 0 - standard, 1 - smaller.
WindowSizeMode=1
# The language of the application interface, such as `zh-CN` `en-US`, which follows the system setting if empty.
Language=
# The location of the user folder.
UserDataFolder=.
```

`UserDataFolder` is the folder of the user's data. If this value does not exist or the set folder does not exist, the application displays the welcome page at startup. If `UserDataFolder` is set to the folder itself or a subfolder where the `config.ini` file is located, you can use **relative paths**, e.g. one of the dots `.` represents the current folder. In other cases, you **must** use an absolute path. In addition, both slash `/` and backslash `\` can be used.

Note: The `config.ini` file must be at the application root folder.

## Database

All setting items except for the two above are stored in the database `StarwardDatabase.db` in the user data folder. This file is a SQLite database, which you can edit with [DB Browser for SQLite](https://sqlitebrowser.org/) or other software.

There is a table named `Setting` in the database that holds the application's setting items, and it has the following structure, with the keys and values represented as text.

```sql
CREATE TABLE Setting
(
    Key TEXT NOT NULL PRIMARY KEY.
    Value TEXT
).
```

There are two types of setting items within the application, static setting items using Pascal nomenclature `ASettingKey`, and dynamic setting items using Pascal nomenclature `a_setting_key`, which represent the existence of a corresponding value for each game region.

## Game Regions

Starward uses `enum GameBiz` to define different game regions, where the full name of the game such as `StarRail` will be specified in the comments when used.

| Key               | Value | Comment                                 |
| ----------------- | ----- | --------------------------------------- |
| None              | 0     | Default value                           |
| All               | 1     | All                                     |
| **GenshinImpact** | 10    | Genshin Impact                          |
| hk4e_cn           | 11    | Genshin Impact (Mainland China)         |
| hk4e_global       | 12    | Original Gods (Global)                  |
| hk4e_cloud        | 13    | Genshin Impact · Cloud (Mainland China) |
| **StarRail**      | 20    | Honkai: Star Rail                       |
| hkrpg_cn          | 21    | Star Rail (Mainland China)              |
| hkrpg_global      | 22    | Star Rail (Global)                      |
| **Honkai3rd**     | 30    | Honkai 3rd                              |
| bh3_cn            | 31    | Honkai 3rd (Mainland China)             |
| bh3_global        | 32    | Honkai 3rd (Global)                     |
| bh3_jp            | 33    | Honkai 3rd (Japan)                      |
| bh3_kr            | 34    | Honkai 3rd (Korea)                      |
| bh3_overseas      | 35    | Honkai 3rd (Southeast Aisa)             |
| bh3_tw            | 36    | Honkai 3rd (TW/HK/MO)                   |

## Static Settings

The data type `Type` in the following table uses the expression in C#, and `-` represents the default value of this type.

| Key                             | Type    | Default Value | Comment                                                                                                                                                          |
| ------------------------------- | ------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| ApiCDNIndex                     | int     | -             | Api CDN options, 0 - CloudFlare, 1 - GitHub, 2 - jsDelivr                                                                                                        |
| EnablePreviewRelease            | bool    | -             | Whether to join the preview release channel.                                                                                                                     |
| IgnoreVersion                   | string? | -             | Ignore the version of the update alert, newer versions will continue to be alerted only if they are greater than this value.                                     |
| EnableBannerAndPost             | bool    | -             | Show game announcements in the launcher page.                                                                                                                    |
| IgnoreRunningGame               | bool    | -             | Ignore running games, the launcher page will no longer show `Game is Running` when enabled.                                                                      |
| SelectGameBiz                   | GameBiz | -             | The last selected game region.                                                                                                                                   |
| ShowNoviceGacha                 | bool    | -             | Show novice gacha stats.                                                                                                                                         |
| GachaLanguage                   | string? | -             | Get the language used for gacha records, the default is the in-game language.                                                                                    |
| EnableDynamicAccentColor        | bool    | -             | The dynamic theme color is calculated from the background image, and the system theme color is used when it is turned off.                                       |
| AccentColor                     | string? | -             | The cached dynamic theme color, used to reduce the amount of calculations at startup, `#ARBG#ARBG`: the former is background color and the latter is text color. |
| VideoBgVolume                   | int     | 100           | The volume of the video background, `0 - 100`.                                                                                                                   |
| PauseVideoWhenChangeToOtherPage | bool    | -             | **Obsolete!** Pause the video when switch to a not launcher page.                                                                                                |
| UseOneBg                        | bool    | -             | Use the same background for all game regions, usually enabled when using video background.                                                                       |
| AcceptHoyolabToolboxAgreement   | bool    | -             | Accept the disclaimer of the HoYoLAB toolbox page.                                                                                                               |
| HoyolabToolboxPaneOpen          | bool    | true          | Is the navigation sidebar on the HoYoLAB Toolbox page open.                                                                                                      |
| EnableSystemTrayIcon            | bool    | true          | Enabled to turn on the singleton mode, and app will be minimized to the system tray when click the close button of window.                                       |
| ExitWhenClosing                 | bool    | true          | Whether to exit the process after clicking the close button of window                                                                                            |

## Dynamic Settings

Dynamic setting items have different values in each game region, and their setting keys will have the game region appended to the end, for example, the setting item `custom_bg`, whose key of Genshin Impact (Global) is `custom_gb_hk4e_global`.

| Key                          | Type    | Default Value | Comment                                                                                                                            |
| ---------------------------- | ------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| bg                           | string? | -             | The official background image file name, the file is in the `bg` subfolder of the user data folder.                                |
| custom_bg                    | string? | -             | The custom background image, the image is the file name and the video is the full path.                                            |
| enable_custom_bg             | bool    | -             | Whether to enable custom background.                                                                                               |
| install_path                 | string? | -             | The folder where the game is installed, not the official launcher folder.                                                          |
| enable_third_party_tool      | bool    | -             | Whether to enable third-party tool to start game instead.                                                                          |
| third_party_tool_path        | string? | -             | The path to the file of third-party tool.                                                                                          |
| start_argument               | string? | -             | The game start argument                                                                                                            |
| last_gacha_uid               | long    | -             | The last selected uid in gacha records page.                                                                                       |
| last_region_of               | GameBiz | -             | The last selected game region, used for quick switching at the top of the app, with the full name of the game appended at the end. |
| last_select_game_record_role | long    | -             | The last selected uid of game role in HoYoLAB toolbox page.                                                                        |
