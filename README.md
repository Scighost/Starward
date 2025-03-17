English | [简体中文](./docs/README.zh-CN.md) | [Tiếng Việt](./docs/README.vi-VN.md) | [日本語](./docs/README.ja-JP.md) | [ภาษาไทย](./docs/README.th-TH.md) | [Русский](./docs/README.ru-RU.md)


# Starward

> **Starward** comes from the slogan of Star Rail: May This Journey Lead Us **Starward**, which is very suitable to be used as an app name.

Starward is an open-source third-party launcher developed to address the shortcomings of HoYoPlay (miHoYo Launcher). It supports all PC games on of HoYoverse and aims to completely replace the official launcher. In addition to the basic functions of a launcher, I will also incorporate some additional features based on individual needs, such as:

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

[![de-DE translation](https://img.shields.io/badge/dynamic/json?color=blue&label=de-DE&style=flat&logo=crowdin&query=%24.progress.0.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/de)
[![en-US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![it-IT translation](https://img.shields.io/badge/dynamic/json?color=blue&label=it-IT&style=flat&logo=crowdin&query=%24.progress.2.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/it)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress.3.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress.4.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress.5.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ru)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress.6.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress.7.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress.8.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress.9.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

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

<img width="1200" src="https://github.com/user-attachments/assets/d1704d44-fadd-4672-aade-c09584b7f16c" />
