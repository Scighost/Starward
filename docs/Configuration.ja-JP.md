English | [简体中文](./Configuration.zh-CN.md) | [Tiếng Việt](./Configuration.vi-VN.md)

# アプリケーションの構成

バージョン 0.8.0から、Starward は設定の保存にレジストリを使用しなくなりました。その代わりにファイルとデータベースを使用するようになり、デバイス間でのアプリケーション全体の移行が容易になりました。ファイル構成が以下の条件になっていない場合は、レジストリを使用します。

```
│ config.ini
│ starward.exe
│ version.ini
│
└─ app-0.8.0
   │ Starward.exe
   ...
```

大丈夫です、心配はしないで。GitHub のリリースからダウンロードをしたファイルは間違いなくこの条件を満たしています。コードを引っ張ってローカル上でデバッグをするときのみ、レジストリを使用して構成を保存します。

## config.ini

`config.ini` ファイルは 2 つの設定項目のみになります:

```ini
# コンソール出力ロギングを有効化するかどうか True/False
EnableConsole=False
# ユーザーフォルダーの場所
UserDataFolder=.
```

`UserDataFolder` はユーザーデータのフォルダーです。この値が存在しない場合は、アプリケーションは起動時にようこそページを表示します。`UserDataFolder` がフォルダー自体、または `config.ini` がサブフォルダーに設定されている場合に**相対パス**を使用する事ができます。 例えば `.` (ドット 1 つ)は現在のフォルダーを指します。それ以外の場合は、**絶対パス**を使用してください。また、 `/` (スラッシュ) と `\` (バックスラッシュ)を使用する事が可能です。

注意: `config.ini` ファイルはアプリケーションフォルダーの直下に配置しなければなりません

## データベース

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

## ゲームのリージョン

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
| Language                        | string? | -             | The language of the application interface, such as `zh-CN` `en-US`, which follows the system setting if empty.                                                   |
| WindowSizeMode                  | int     | -             | The application window size, 0 - standard, 1 - smaller                                                                                                           |
| ApiCDNIndex                     | int     | -             | Api CDN options, 0 - CloudFlare, 1 - GitHub, 2 - jsDelivr                                                                                                        |
| EnablePreviewRelease            | bool    | -             | Whether to join the preview release channel.                                                                                                                     |
| IgnoreVersion                   | string? | -             | Ignore the version of the update alert, newer versions will continue to be alerted only if they are greater than this value.                                     |
| EnableBannerAndPost             | bool    | -             | Show game announcements in the launcher page.                                                                                                                    |
| IgnoreRunningGame               | bool    | -             | Ignore running games, the launcher page will no longer show `Game is Running` when enabled.                                                                      |
| SelectGameBiz                   | GameBiz | -             | The last selected game region.                                                                                                                                   |
| ShowNoviceGacha                 | bool    | -             | Show novice gacha stats.                                                                                                                                         |
| GachaLanguage                   | string? | -             | Get the language used for gacha records, the default is the in-game language.                                                                                    |
| EnableDynamicAccentColor        | bool    | -             | The dynamic theme color is calculated from the background image, and the system theme color is used when it is turned off.                                       |
| AccentColor                     | string? | -             | The cached dynamic theme color, used to reduce the amount of calculations at startup, `#ARBG#ARBG`: the former is background color and the latter is text color/ |
| VideoBgVolume                   | int     | 100           | The volume of the video background, `0 - 100`.                                                                                                                   |
| PauseVideoWhenChangeToOtherPage | bool    | -             | Pause the video when switch to a not launcher page.                                                                                                              |
| UseOneBg                        | bool    | -             | Use the same background for all game regions, usually enabled when using video background.                                                                       |
| AcceptHoyolabToolboxAgreement   | bool    | -             | Accept the disclaimer of the HoYoLAB toolbox page.                                                                                                               |

## ダイナミック設定

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
