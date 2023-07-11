English | [简体中文](./docs/README.zh-CN.md) | [Tiếng Việt](./docs/README.vi-VN.md) | [日本語](./docs/README.ja-JP.md)

# Starward って何ですか?

**Starward** は、崩壊:スターレイルのスローガン「May This Journey Lead Us Starward」(この旅が私たちを星へと導くように)に由来しています。 **Starward** は、miHoYo のすべてのデスクトップ版ゲームをサポートするゲームランチャーです。このプロジェクトの目標は、公式ランチャーを完全に置き換える事と更にいくつかの拡張機能を追加する事になります。

ゲームのダウンロードとインストールに加えて以下の機能が含まれます:

-  ゲームのプレイ時間を記録
-  ゲームアカウントの切り替え
-  ゲームのスクリーンショットを表示
-  ガチャの記録を保存
-  HoYoLAB ツールボックス

さらに多くの機能の追加を計画しています...

> Starward は、ガチャアイテムの画像など開発者がゲームデータやリソース関連と言った断続的に更新を必要とする物は実装しません。

## ダウンロード

> [WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) がインストールされた Windows 10 1809 (17763) 以降の環境を使用する必要があります。

最新版のリリースは、[GitHub のリリース](https://github.com/Scighost/Starward/releases) ページからダウンロードできます。このアプリケーションはインクリメンタルアップデートを採用していますので、簡単で便利になっています。

一部のデバイスで実行後にクラッシュをする問題が発生する可能性があります。この問題が発生した場合は、`Starward` のフォルダーに `config.ini` ファイルを作成し、以下を貼り付けてください。`config.ini` の詳細は、[docs/Configuration.ja-JP.md](./docs/Configuration.ja-JP.md) を参照してください。

``` ini
EnableConsole=False
UserDataFolder=.
```


## 翻訳

[![en-US translation](https://img.shields.io/badge/dynamic/json?color=blue&label=en-US&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27en-US%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/en-US)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

Starward はローカライズに[Crowdin](https://crowdin.com/project/starward) を使用しています。機械翻訳がされた英文を原文として提供をしています。現地語の翻訳や校正が行えますのでご参加を是非ともお待ちしています。 新しい言語を翻訳したい場合は、Issue を作成してください。

## 開発

プロジェクトをローカルでコンパイルするには、Visual Studio 2022をインストールして以下のワークロードを選択する必要があります:

-  .NET デスクトップ開発
-  C++ によるデスクトップ開発
-  ユニバーサル Windows プラットフォーム開発

## 謝辞

 まず最初に、『崩壊』でこのプロジェクトにインスピレーションを与えてくれた [neon-nyan](https://github.com/neon-nyan) に感謝をします。Starward は彼が作ったリソースだけでなく、「ユーザーインターフェースのデザインを似せた物」も作りました。  [Collapse](https://github.com/neon-nyan/Collapse) のコードから多くの事を学び、私の開発プロセスをよりスムーズにしてくれました。

それから、無料の CDN を提供してくれた CloudFlare に感謝をします。

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

このプロジェクトで使用されているサードパーティー製ライブラリには以下が含まれます:

-  [Dapper](https://github.com/DapperLib/Dapper)
-  [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
-  [HDiffPatch](https://github.com/sisong/HDiffPatch)
-  [Markdig](https://github.com/xoofx/markdig)
-  [MiniExcel](https://github.com/mini-software/MiniExcel)
-  [Serilog](https://github.com/serilog/serilog)
-  [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
-  [Vanara PInvoke](https://github.com/dahall/Vanara)
-  [WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
-  [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit)

## スクリーンショット

![screenshot](https://github.com/Scighost/Starward/assets/61003590/22dad10c-bc42-44a7-b47f-5a608dfc5b08)
