> 本文的版本是 **v0.10.7**，若版本落后请以[原文](../README.md)为准。


[米哈游](https://www.mihoyo.com)的 PC 游戏启动器是我见到过最垃圾的商业软件之一，整体的使用体验尚可，但在部分细节方面做得非常糟糕：

- 不支持高缩放率，整个界面有一种朦胧的美。
- 校验资源使用单线程，重现一核有难、多核围观的名场面。
- 内置浏览器引擎但多年界面设计无任何变化，既没用上网页的灵活性，还徒增体积。


# Starward

> **Starward** 出自星穹铁道开服前的宣传语———愿此行，终抵群星 (May This Journey Lead Us **Starward**)。

Starward 是一个为了解决上述缺点而开发的开源第三方启动器，支持米哈游 PC 端的所有游戏，目标是完全替代官方启动器。除了启动器的基本功能外，我还会根据个人需求增加一些拓展功能，比如：

-  记录游戏时间
-  切换游戏账号
-  浏览游戏截图
-  保存抽卡记录
-  切换游戏服务器

更多功能留给您自己探索。。。


## 安装

首先，您的设备需要满足以下要求：

- Windows 10 1809 (17763) 及以上的版本
- 已安装 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2)
- 为了更好的使用体验，请在系统设置中开启**透明效果**和**动画效果**

然后在 [GitHub Release](https://github.com/Scighost/Starward/releases) 下载对应 CPU 架构的压缩包，解压后运行 `Starward.exe` 并按提示操作。


## 本地化

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ru%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ru)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

Starward 使用 [Crowdin](https://crowdin.com/project/starward) 进行应用内文本的本地化工作，你可以帮助我们翻译和校对本地语言，我们期待有更多的人加入。

[本地化指南](./Localization.zh-CN.md)


## 开发

在本地编译应用，你需要安装 Visual Studio 2022 并选择以下负载：

-  .NET 桌面开发
-  使用 C++ 的桌面开发
-  通用 Windows 平台开发


## 赞助

开发不易，如果你觉得 Starward 好用，可以在 https://donate.scighost.com 赞助我。


## 致谢

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

首先，我要向本项目的所有贡献者和翻译者致以最诚挚的感谢，有了你们，Starward 才能变得更好。

然后，我要特别感谢 [@neon-nyan](https://github.com/neon-nyan)，本项目的灵感和设计正是来源于他的项目 [Collapse](https://github.com/neon-nyan/Collapse)。我也从 Collapse 的代码中学到了很多知识，有此珠玉在前，我的开发过程顺利了很多。

其次，感谢[胡桃工具箱](https://github.com/DGP-Studio/Snap.Hutao)的主要开发者 [@Lightczx](https://github.com/Lightczx)，Starward 的开发过程中得到了他的很多帮助。

我还要感谢 CloudFlare 提供的免费 CDN，它带给了所有人良好的更新体验。

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

本项目中使用到的第三方库包括：

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

## 截图

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/6647e600-f4d8-4f11-8b23-411f42551abc" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/f778b829-35f7-45ab-b711-ece8b532450f" />
</picture>

背景图片来自 [Pixiv@コユコム](https://www.pixiv.net/artworks/114596167)
