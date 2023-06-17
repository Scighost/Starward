English | [简体中文](./README.zh-CN.md)


# What is Starward?

**Starward** comes from the slogan of Star Rail: May This Journey Lead Us **Starward**, witch is very suitable to be used as an app name. Starward is a game launcher that supports all desktop games of miHoYo. The goal of this project is to replace the official launcher completely, and then add some extended features.

In addition to the download and installation of the game, the following features are included:

- Record game time
- Switching game accounts
- View game screenshots
- Save gacha records

More features are being planned...

> Starward will not achieve features that require developers to continually update game data and resources, such as gacha item images.


## Download

> Only Windows 10 1809 (17763) and above are supported

You can download the latest release from the [Release](https://github.com/Scighost/Starward/releases) page. The app uses incremental updates, both easy and convenient.


## Localization

[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress.1.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![en-US translation](https://img.shields.io/badge/dynamic/json?color=blue&label=en-US&style=flat&logo=crowdin&query=%24.progress.0.data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/en-US)

Starward uses [Crowdin](https://crowdin.com/project/starward) for localization work, providing machine translated English text as the original. You can help us translate and proofread the local language. If you would like to add a new translation language, please create an issue.


## Development

To compile the project locally, you need to install Visual Studio 2022 and select the following workloads:

- .NET Desktop Development
- C++ Desktop Development
- Universal Windows Platform Development


## Thanks

First of all, I'd like to thank [neon-nyan](https://github.com/neon-nyan) specially, whose project [Collapse](https://github.com/neon-nyan/Collapse) inspired this project. Starward not only used some resources he created, but also imitated the user interface design. I learned a lot from Collapse's code, and it made my development process much smoother.

Then, thanks to CloudFlare for providing free CDN.

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

And the third-party libraries used in this project include:

- [Dapper](https://github.com/DapperLib/Dapper)
- [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
- [HDiffPatch](https://github.com/sisong/HDiffPatch)
- [Markdig](https://github.com/xoofx/markdig)
- [MiniExcel](https://github.com/mini-software/MiniExcel)
- [Serilog](https://github.com/serilog/serilog)
- [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
- [Vanara PInvoke](https://github.com/dahall/Vanara)
- [WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
- [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit)


## Screenshot

![screenshot](https://github.com/Scighost/Starward/assets/61003590/22dad10c-bc42-44a7-b47f-5a608dfc5b08)
