# Application Configuration

เริ่มตั้งแต่เวอร์ชัน 0.8.0 Starward จะไม่ใช้ registry ในการเก็บข้อมูลตั้งค่าต่างๆอีกต่อไป แต่จะใช้ไฟล์แต่ฐานข้อมูลทำให้ง่ายขึ้นในการย้ายข้อมูลแอพพลิเคชั่นระหว่างอุปกรณ์ต่างๆ อย่างไรก็ตาม registry ยังคงถูกใช้หากโครงสร้างของไฟล์ไม่ได้ตรงกับเงื่อนไขดังต่อไปนี้ :

```
│ config.ini
│ starward.exe
│ version.ini
│
└─ app-0.8.0
   │ Starward.exe
   ...
```

แต่ไม่ต้องเป็นห่วง ไฟล์ที่ดาวน์โหลดจาก Github Release จะตรงกับเงื่อนไขแน่นอน มีแค่ตอนที่ผู้ใช้ดึงโค้ดและมาดีบั๊กเองเท่านั่นที่จะทำให้ต้องใช้ registry ในการเก็บข้อมูล

## config.ini

ไฟล์ `config.ini` มีการตั้งค่าแค่สองอย่างเท่านั้น

```ini
# ต้องการให้เปิดใช้งานคอนโซลบันทึก output หรือไม่ , True/False
EnableConsole=False
# ตำแหน่งที่ตั้งของโฟลเดอร์เก็บข้อมูล
UserDataFolder=.
```

'UserDataFolder' เป็นโฟลเดอร์ที่เก็บข้อมูลของผู้ใช้งาน ถ้าหากค่าดังกล่าวไม่ได้ตั้งไว้หรือโฟลเดอร์ไม่มีอยู่ แอพพลิเคชั่นจะแสดงหน้าต้อนรับเมื่อเปิด 
หาก 'UserDataFolder' ตั้งค่าเป็นโฟลเดอร์ของมันเองหรือว่าโฟลเดอร์ย่อยที่มีไฟล์ 'config.ini' สามารถใช้ **relative path** ได้ เช่น '.' แทนถึงโฟลเดอร์ปัจจุบัน ในกรณีอื่นๆ **จำเป็นต้องใช้ absolute path** (ในการระบุ path สามารถใช้ได้ทั้ง '/' และ '\')

Note: ไฟล์ 'config.ini' ต้องอยู่ที่โฟลเดอร์รูทของแอพพลิเคชั่น

## Database

การตั้งค่าทุกอย่างยกเว้นที่ระบุมาสองอันข้างต้นจะถูกเก็บอยู่ในฐานข้อมูล 'StarwardDatabse.db' ในโฟลเดอร์เก็บข้อมูลที่ผู้ใช้เลือก ไฟล์ข้างต้นเป็นไฟล์ SQLite Database ที่สามารถแก้ไขได้ด้วย [DB Browser for SQLite](https://sqlitebrowser.org/) หรือโปรแกรมอื่นๆ

ข้างในจะมีตารางชื่อ 'Setting' ที่เก็บข้อมูลการตั้งค่าไว้ มีโครงสร้างดังนี้ โดยมี key และ values เป็นข้อความ


```sql
CREATE TABLE Setting
(
    Key TEXT NOT NULL PRIMARY KEY.
    Value TEXT
).
```

มีการตั้งค่าสองประเภทในแอพพลิเคชั่น - การตั้งค่าแบบ static ที่ใช้ชื่อ 'ASettingKey' กับการตั้งแค่แบบ Dynamic ที่ใช้ชื่อ 'a_setting_key' ตามหลักการตั้งชื่อของ Pascal โดยแสดงถึงเกมต่างๆแยกภูมิภาคออกไป 

## ภูมิภาคของเกม

Starward ใช้ 'enum GameBiz' ในการระบุเกมแต่ละภูมิภาค โดยที่ชื่อเต็มของเกมเช่น 'StarRail' จะถูกเขียนลงในคอมเมนต์เมื่อใช้

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

ประเภทข้อมูล Type ในตารางด้านล่างใช้ใน C# และ - แทนค่าเริ่มต้นของประเภทนี้ในกรณีที่ไม่ได้กำหนดค่าเริ่มต้น

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
| AccentColor                     | string? | -             | The cached dynamic theme color, used to reduce the amount of calculations at startup, `#ARBG#ARBG`: the former is background color and the latter is text color. |
| VideoBgVolume                   | int     | 100           | The volume of the video background, `0 - 100`.                                                                                                                   |
| PauseVideoWhenChangeToOtherPage | bool    | -             | **Obsolete!** Pause the video when switch to a not launcher page.                                                                                                |
| UseOneBg                        | bool    | -             | Use the same background for all game regions, usually enabled when using video background.                                                                       |
| AcceptHoyolabToolboxAgreement   | bool    | -             | Accept the disclaimer of the HoYoLAB toolbox page.                                                                                                               |
| HoyolabToolboxPaneOpen          | bool    | true          | Is the navigation sidebar on the HoYoLAB Toolbox page open.                                                                                                      |

## Dynamic Settings

รายการตั้งค่าแบบไดนามิกจะมีค่าที่แตกต่างกันในแต่ละภูมิภาคของเกม และ Key ของการตั้งค่าเหล่านี้จะมีการเพิ่มส่วนท้ายที่เป็นชื่อภูมิภาคของเกม ตัวอย่างเช่น รายการตั้งค่า custom_bg ซึ่ง Key ของ Genshin Impact (Global) คือ custom_gb_hk4e_global

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
