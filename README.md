**v0.10.7** | English | [简体中文](./docs/README.zh-CN.md) | [Tiếng Việt](./docs/README.vi-VN.md) | [日本語](./docs/README.ja-JP.md) | [ภาษาไทย](./docs/README.th-TH.md) | [Русский](./docs/README.ru-RU.md)


The PC game launcher of [HoYoverse](https://www.hoyoverse.com) is one of the worst commercial software I have ever seen. The overall user experience is passable, but it performs terribly in certain details:

- Lacks support for high scaling ratios, resulting in a hazy aesthetic across the entire interface.
- Resource verification utilizes a single thread, unable to effectively utilize multiple cores, leading to a significant waste of time.
- Despite having a built-in browser engine, the interface design has remained unchanged for years, failing to capitalize on the flexibility of web pages and, instead, adding unnecessary bulk.


# Starward

> **Starward** comes from the slogan of Star Rail: May This Journey Lead Us **Starward**, which is very suitable to be used as an app name.

Starward is an open-source third-party launcher developed to address the aforementioned shortcomings. It supports all PC games on of HoYoverse and aims to completely replace the official launcher. In addition to the basic functions of a launcher, I will also incorporate some additional features based on individual needs, such as:

-  Record game time
-  Switch game accounts
-  View game screenshots
-  Save gacha records

More features are left for you to explore...


## Install

First, your device needs to meet the following requirements:

- Windows 10 1809 (17763) and above.
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) installed.
- For better experience, please enable **Transparency effects** and **Animation effects** in the system settings.

Next, download the package for your CPU architecture from [GitHub Release](https://github.com/Scighost/Starward/releases). Extract it, then run `Starward.exe` and follow the prompts.


## Localization

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ru%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ru)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

Starward uses [Crowdin](https://crowdin.com/project/starward) for in-app text localization work. You can contribute by helping us translate and proofread content in your local language. We look forward to having more people join us.

[Localization Guide](./docs/Localization.md)


## Development

To compile the project locally, you need to install Visual Studio 2022 and select the following workloads:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development


## Donation

Development is not easy. If you think Starward useful, you cloud donate me at https://donate.scighost.com.


## Thanks

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

First of all, I would like to express my sincerest thanks to all the contributors and translators of this project. Starward can only become better because of you.

Then, I want to express my special thanks to [@neon-nyan](https://github.com/neon-nyan). The inspiration and design for this project come directly from his project [Collapse](https://github.com/neon-nyan/Collapse). I have gained a lot of knowledge from the Collapse code, and with such a valuable reference, my development process has been much smoother.

Next, a big thanks to the main developer of [Snap Hutao](https://github.com/DGP-Studio/Snap.Hutao), [@Lightczx](https://github.com/Lightczx). His assistance has been invaluable during the development of Starward.

Additionally, thanks CloudFlare for providing free CDN services, contributing to a great update experience for everyone.

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

And the third-party libraries used in this project include:

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

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/34b7c31f-d8dc-4539-8fbb-12705676e382" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/95a33d10-3dc3-4b3c-875d-d898e5eb2f50" />
</picture>

Background image is from [Pixiv@七言不绝诗](https://www.pixiv.net/artworks/113506129)