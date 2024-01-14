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

始めに使用しているデバイスが以下の条件を満たしている必要があります:

- Windows 10 1809 (17763) 以降の環境である事
- [Visual C++ ランタイム](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)がインストール済みである事
- [WebView2 ランタイム](https://developer.microsoft.com/microsoft-edge/webview2)がインストール済みである事
- Windows 10 を使用しているユーザーは、[Segoe Fluent Icons](https://aka.ms/SegoeFluentIcons) フォントのインストールを推奨します

[GitHub のリリースページ](https://github.com/Scighost/Starward/releases)から使用している CPU のアーキテクチャに対応したパッケージをダウンロードして展開を行い、`Starward.exe` のプロンプトに従って操作をしてください。

一部のデバイスで `Starward.exe` を実行時にクラッシュをする問題が発生する事があります。この問題が発生した場合は、`Starward.exe` が存在するフォルダーに `config.ini` のファイルを作成して、以下を貼り付けてください。`config.ini` の詳細は、[docs/Configuration.ja-JP.md](./Configuration.ja-JP.md) を参照してください。

``` ini
UserDataFolder=.
```


## 翻訳

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
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

 まず最初に、このプロジェクトのインスパイアとなる 「Collapse」 の開発者 [neon-nyan](https://github.com/neon-nyan) 氏に感謝をします。Starward は彼が作ったリソースだけでなく、「ユーザーインターフェース」も参考にしています。  [Collapse](https://github.com/neon-nyan/Collapse) のコードから多くの事を学び、私の開発プロセスをよりスムーズにしてくれました。

それから、無料の CDN を提供してくれた CloudFlare に感謝をします。

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

このプロジェクトで使用されているサードパーティー製ライブラリには以下が含まれます:

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

## スクリーンショット

![screenshot](https://github.com/reindex-ot/Starward/assets/32851879/ded2c30f-a6bd-4ed8-8e2f-5a44fc03b094)

