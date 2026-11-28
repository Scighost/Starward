# Starward

> **Starward** 出自星穹铁道开服前的宣传语———愿此行，终抵群星 (May This Journey Lead Us **Starward**)。

Starward 是由 Scighost 创建的开源第三方启动器。Shiroha 维护此 fork，在米哈游 PC 游戏之外加入了《明日方舟：终末地》等鹰角游戏的支持。Starward 还提供以下功能。

-  记录游戏时间
-  切换游戏账号
-  浏览游戏截图
-  保存抽卡记录
-  切换游戏服务器

更多功能留给您自行探索。

[项目主页](https://github.com/NagiYume/Starward) · [问题反馈](https://github.com/NagiYume/Starward/issues) · [隐私政策](./Privacy.zh-CN.md) · [上游项目](https://github.com/Scighost/Starward)

## 安装

首先，您的设备需要满足以下要求：

- Windows 10 1809 (17763) 及以上的版本
- 已安装 [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2)
- 已安装 [WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg)
- 为了更好的使用体验，请在系统设置中开启**透明效果**和**动画效果**
>[WebP 映像扩展](https://apps.microsoft.com/detail/9pg2dk419drg) 一般情况下系统自带，如果程序无法正常显示背景图片请自行检查是否安装。

在 [GitHub Releases](https://github.com/NagiYume/Starward/releases) 下载对应 CPU 架构的安装包或便携版。运行安装程序，或解压便携版后运行 `Starward.exe`。

`v0.19.2-1` 是此 fork 更新渠道的首版，请手动安装一次。后续可在「应用设置 → 关于」中检查并安装增量更新。


## 本地化

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

Starward 使用 [Crowdin](https://crowdin.com/project/starward) 进行应用内文本的本地化工作，你可以帮助我们翻译和校对本地语言，我们期待有更多的人加入。

[本地化指南](./Localization.zh-CN.md)


## 开发

在本地编译应用，你需要安装 Visual Studio 2022 并选择以下负载：

-  .NET 桌面开发
-  使用 C++ 的桌面开发
-  通用 Windows 平台开发


## 赞助

[赞助开发者](https://www.akedata.wiki/?plugin=about)。


## 致谢

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

首先，我要向本项目的所有贡献者和翻译者致以最诚挚的感谢，有了你们，Starward 才能变得更好。

然后，我要特别感谢 [@neon-nyan](https://github.com/neon-nyan)，本项目的灵感和设计正是来源于他的项目 [Collapse](https://github.com/neon-nyan/Collapse)。我也从 Collapse 的代码中学到了很多知识，有此珠玉在前，我的开发过程顺利了很多。

其次，感谢[胡桃工具箱](https://github.com/DGP-Studio/Snap.Hutao)的主要开发者 [@Lightczx](https://github.com/Lightczx)，Starward 的开发过程中得到了他的很多帮助。

我还要感谢 [CloudFlare](https://www.cloudflare.com/) 提供的免费 CDN，以及 [SignPath Foundation](https://signpath.org/) 为开源项目提供免费的代码签名服务。

<img alt="cloudflare" height="72px" src="https://github.com/user-attachments/assets/c1fba88e-4cd1-45df-b681-bf5634215f41" />&nbsp;&nbsp;&nbsp;&nbsp;<img alt="signpath foundation" height="72px" src="https://github.com/user-attachments/assets/052c654a-13fa-4e7a-8f1d-9f57c83f438b" />

以及本项目中使用的[第三方库](./docs/ThirdParty.md)。


## 截图

<img width="1200" src="https://github.com/user-attachments/assets/ddd51a20-9705-4112-a454-75b07b7a6f8f" />
