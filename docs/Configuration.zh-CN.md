# 应用配置

从 0.8.0 版本开始，Starward 不再使用注册表存储配置信息，而是改用配置文件和数据库，使得整体应用在不同设备间的迁移更加方便。但是当文件结构不满足以下条件时，仍会使用注册表。

```
│  config.ini
│  Starward.exe
│  version.ini
│
└─ app-0.8.0
   │  Starward.exe
   ...
```

不必担心，从 GitHub Release 中下载的文件必定会满足此条件，只有自己拉取代码在本地调试时才会使用注册表存储配置。

## config.ini

`config.ini` 文件中仅包含两个设置项：

```ini
# 是否启用控制台输出日志，True/False
EnableConsole=False
# 应用窗口大小，0 - 标准，1 - 较小
WindowSizeMode=1
# 应用界面语言，如 `zh-CN` `en-US`，为空时代表跟随系统设置
Language=
# 用户数据文件夹的位置
UserDataFolder=.
```

`UserDataFolder` 是用户数据文件夹的位置，如果此值不存在或设置的文件夹不存在，应用在启动时显示欢迎页面。如果用户文件夹设置为 `config.ini` 文件所在的文件夹本身或子文件夹，则可以使用**相对路径**，例如上文中的一个点 `.` 就代表当前文件夹。而其他的情况则**必须**使用绝对路径。此外，正斜杠 `/` 和反斜杠 `\` 都可以使用。

注意：`config.ini` 文件必须在应用根目录处才会被读取到。

## 数据库

除了上述的两个设置项外，其他设置项都存储在用户数据文件夹中的 `StarwardDatabase.db` 数据库中。此文件是一个 SQLite 数据库，你可以使用 [DB Browser for SQLite](https://sqlitebrowser.org/) 或其他的软件进行编辑。

数据库中有一个名为 `Setting` 的表保存了应用的设置项，它的结构如下，设置项的键值都是用文本表示。

```sql
CREATE TABLE Setting
(
    Key   TEXT NOT NULL PRIMARY KEY,
    Value TEXT
);
```

应用内的设置项分为两种类型，一个是使用帕斯卡命名法 `ASettingKey` 的静态设置项，另一种是使用帕斯卡命名法 `a_setting_key` 的动态设置项，动态设置项代表每个游戏区域都存在一个相应的值。

## 游戏区域

Starward 使用 `enum GameBiz` 定义了不同的游戏区域，其中游戏全名如 `StarRail` 使用到时会在备注中说明。

| Key               | Value | Comment            |
| ----------------- | ----- | ------------------ |
| None              | 0     | 默认值             |
| All               | 1     | 所有               |
| **GenshinImpact** | 10    | 原神               |
| hk4e_cn           | 11    | 原神（国服）       |
| hk4e_global       | 12    | 原神（国际服）     |
| hk4e_cloud        | 13    | 云原神（国服）     |
| **StarRail**      | 20    | 星穹铁道           |
| hkrpg_cn          | 21    | 星穹铁道（国服）   |
| hkrpg_global      | 22    | 星穹铁道（国际服） |
| **Honkai3rd**     | 30    | 崩坏三             |
| bh3_cn            | 31    | 崩坏三（国服）     |
| bh3_global        | 32    | 崩坏三（国际服）   |
| bh3_jp            | 33    | 崩坏三（日服）     |
| bh3_kr            | 34    | 崩坏三（韩服）     |
| bh3_overseas      | 35    | 崩坏三（东南亚服） |
| bh3_tw            | 36    | 崩坏三（港澳台服） |

## 静态设置项

下表中的数据类型 `Type` 使用的是 C# 中的表达方法，`-` 代表此类型的默认值。

| Key                             | Type    | Default Value | Comment                                                                      |
| ------------------------------- | ------- | ------------- | ---------------------------------------------------------------------------- |
| ApiCDNIndex                     | int     | -             | Api CDN 选项，0 - CloudFlare，1 - GitHub，2 - jsDelivr                       |
| EnablePreviewRelease            | bool    | -             | 是否加入预览版更新渠道                                                       |
| IgnoreVersion                   | string? | -             | 忽略更新提醒的版本，新版本大于此值才会继续提醒                               |
| EnableBannerAndPost             | bool    | -             | 显示启动器页面的游戏公告                                                     |
| IgnoreRunningGame               | bool    | -             | 忽略正在运行的游戏，启用后启动器页面不会再显示游戏正在运行                   |
| SelectGameBiz                   | GameBiz | -             | 最后一次选择的游戏区域                                                       |
| ShowNoviceGacha                 | bool    | -             | 显示新手卡池抽卡记录                                                         |
| GachaLanguage                   | string? | -             | 获取抽卡记录时使用的语言，默认为游戏内的语言                                 |
| EnableDynamicAccentColor        | bool    | -             | 动态主题色，动态主题色通过背景图计算而来，关闭后使用系统主题色               |
| AccentColor                     | string? | -             | 缓存的动态主题色，用在启动时减少计算量，`#ARBG#ARBG`，前者背景色后者文字颜色 |
| VideoBgVolume                   | int     | 100           | 视频背景的音量，`0 - 100`                                                    |
| PauseVideoWhenChangeToOtherPage | bool    | -             | **过时的！** 切换到非启动器页面时暂停视频                                                 |
| UseOneBg                        | bool    | -             | 所有游戏区域使用同一个背景，一般在使用视频背景时启用                         |
| AcceptHoyolabToolboxAgreement   | bool    | -             | 接受米游社工具箱页面的免责声明                                               |
| HoyolabToolboxPaneOpen          | bool    | true          | 米游社工具箱页面的导航侧栏是否打开                                             |
| EnableSystemTrayIcon            | bool    | true          | 启用后开启应用单例模式，点击关闭窗口按键时应用将最小化到系统托盘                 |
| ExitWhenClosing                 | bool    | true          | 点击关闭窗口按键时直接退出进程                                            |

## 动态设置项

动态设置项在每个游戏区域都存在不同的值，其设置键的末尾会附加游戏区域，比如设置项 `custom_bg`，其原神国服的键就是 `custom_gb_hk4e_cn`。

| Key                          | Type    | Default Value | Comment                                                            |
| ---------------------------- | ------- | ------------- | ------------------------------------------------------------------ |
| bg                           | string? | -             | 官方背景图的文件名，文件在用户数据文件夹的 `bg` 子文件夹中         |
| custom_bg                    | string? | -             | 自定义背景图，图片是文件名，视频则是完整路径                       |
| enable_custom_bg             | bool    | -             | 是否启用自定义背景                                                 |
| install_path                 | string? | -             | 游戏安装的文件夹，不是官方启动器的文件夹                           |
| enable_third_party_tool      | bool    | -             | 是否启用第三方程序代替启动                                         |
| third_party_tool_path        | string? | -             | 第三方程序文件的路径                                               |
| start_argument               | string? | -             | 游戏的启动参数                                                     |
| last_gacha_uid               | long    | -             | 抽卡页面上一次选择的 uid                                           |
| last_region_of               | GameBiz | -             | 上一次选择的游戏区域，用于在应用顶部快速切换，末尾附加的是游戏全名 |
| last_select_game_record_role | long    | -             | 工具箱页面上一次选择游戏账号的 uid                                 |
