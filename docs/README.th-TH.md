# อะไรคือ Starward?

**Starward**  มาจากสโลแกนของ Star Rail: ขอให้การเดินทางครั้งนี้ พาเรามุ่งไปสู่ดวงดาว (May This Journey Lead Us **Starward**) ซึ่งเหมาะมากที่จะใช้เป็นชื่อแอพ 
Starward เป็น launcer ที่รองรับเกมบนเดสก์ท็อปทั้งหมดของ miHoYo/HoYoverse โดยมีเป้าหมายที่จะแทนที่ launcher หลักอย่างเป็นทางการ พร้อมกับเพิ่มฟีเจอร์ใหม่ๆเข้าไปด้วย

นอกจากที่จะให้ผู้ใช้ดาวน์โหลดแล้วติดตั้งเกม ก็ยังมีฟีเจอร์เพิ่มเติม ดังนี้:

- นับเวลาที่เล่น
- สลับเปลี่ยนบัญชี
- ดูภาพหน้าจอ
- ดูประวัติการสุ่มกาชา
- HoYoLAB Toolbox

และมีอีกที่วางแผนไว้...

> Starward จะไม่มีฟีเจอร์ที่ต้องให้ผู้พัฒนาคอยอัพเดตข้อมูลและทรัพยากรของเกม เช่นรูปของไอเทม ฯลฯ

## การดาวน์โหลด

อย่างแรก อุปกรณ์ของคุณต้องตรงตามเงื่อนไขเหล่านี้:

- Windows 10 1809 (17763) ขึ้นไป
- ติดตั้ง [Visual C++ Runtime](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) 
- ติดตั้ง [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed

ดาวน์โหลดแพ็คเกจสำหรับสถาปัตยกรรม CPU ที่ใช้อยู่จาก [GitHub Release](https://github.com/Scighost/Starward/releases) แตกไฟล์ แล้วรัน 'Starward.exe' แล้วทำตามที่มันบอกจนเซ็ทอัพเสร็จ

Starward อาจ crash ได้บนบางอุปกรณ์ หากพบเจอปัญหานี้ให้สร้าง 'config.ini' ไฟล์ในโฟลเดอร์ 'Starward' แล้ววางข้อความต่อไปนี้ลงไป 
ไปที่ [docs/Configuration.md](./Configuration.md) หากต้องการข้อมูลเพิ่มเติมเกี่ยวกับ 'config.ini'

``` ini
UserDataFolder=.
```

## การแปลภาษา

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

Starward ใช้ [Crowdin](https://crowdin.com/project/starward) ในการแปลภาษาโดยให้ข้อความภาษาอังกฤษที่ถูกแปลด้วยเครื่องจักรเป็นภาษาต้นแบบ คุณสามารถช่วยแปลหรือตรวจสอบข้อความภาษาของคุณได้
เรายินดีต้อนรับคนที่สนใจช่วย หากคุณต้องการเพิ่มภาษาอื่นๆ สามารถสร้าง issue เพื่อบอกพวกเราได้

## การพัฒนา

ในการคอมไพล์โปรเจกต์นี้ คุณต้องติดตั้ง Visual studio 2022 แล้วเลือก workload ต่อไปนี้ :

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development

## คำขอบคุณ

ก่อนอื่น ขอขอบคุณคุณ [neon-nyan](https://github.com/neon-nyan) อย่างมาก โดยเฉพาะโปรเจกต์ [Collapse](https://github.com/neon-nyan/Collapse) ที่เป็นแรงบันดาลใจให้โปรเจกต์นี้
Starward ไม่เพียงแต่จะใช้ทรัพยากรบางส่วนที่เขาสร้างแล้ว ยังนำเอาแนวการออกแบบ UI มาใช้อีกด้วย 
ผมได้เรียนรู้เยอะมากจากโค้ดของ Collapse และได้ทำให้การพัฒนา Starwart เป็นไปได้อย่างราบรื่น

นอกจากนี้ผมต้องขอบคุณ CloudFlare ที่ได้ให้บริการ CDN ฟรีด้วย

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

และไลบราลีต่างๆ ดังนี้:

-  [Dapper](https://github.com/DapperLib/Dapper)
-  [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
-  [HDiffPatch](https://github.com/sisong/HDiffPatch)
-  [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon)
-  [HoYo-Glyphs](https://github.com/SpeedyOrc-C/HoYo-Glyphs)
-  [MiniExcel](https://github.com/mini-software/MiniExcel)
-  [ScottPlot](https://github.com/ScottPlot/ScottPlot)
-  [Serilog](https://github.com/serilog/serilog)
-  [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
-  [Vanara PInvoke](https://github.com/dahall/Vanara)
-  [WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
-  [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit)

## Screenshot

![screenshot](https://github.com/Scighost/Starward/assets/61003590/22dad10c-bc42-44a7-b47f-5a608dfc5b08)
