[English](../README.md) | [简体中文](./README.zh-CN.md) | Tiếng Việt | [日本語](./README.ja-JP.md)

# Starward là gì?

**Starward** xuất phát từ khẩu hiệu của Star Rail: Hi vọng cuộc hành trình này sẽ đưa chúng ta đến những vì sao (May This Journey Lead Us **Starward**), rất thích hợp để sử dụng làm tên ứng dụng. Starward là trình khởi chạy trò chơi hỗ trợ tất cả các trò chơi trên máy tính của miHoYo. Mục tiêu của dự án này là thay thế hoàn toàn trình khởi chạy chính thức, và sau đó thêm một số tính năng mở rộng.

Ngoài việc tải xuống và cài đặt trò chơi, nó còn bao gồm những tính năng dưới đây:

-  Ghi lại thời gian chơi
-  Chuyển đổi tài khoản
-  Xem ảnh chụp màn hình trò chơi
-  Lưu lịch sử gacha
-  Hộp công cụ HoYoLAB

Và nhiều tính năng khác đang được lên kế hoạch...

> Starward sẽ không đạt được những tính năng yêu cầu nhà phát triển liên tục cập nhật dữ liệu trò chơi và tài nguyên, như là hình ảnh vật phẩm gacha.

## Tải xuống

Đầu tiên, thiết bị của bạn cần đáp ứng những điều kiện sau:

- Windows 10 1809 (17763) và trở đi.
- Đã cài đặt [Visual C++ Runtime](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist).
- Đã cài đặt [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2).
- Người dùng Windows 10 được khuyến khích cài đặt font [Segoe Fluent Icons](https://aka.ms/SegoeFluentIcons)

Tải gói dành cho kiến trúc CPU của bạn từ [GitHub Release](https://github.com/Scighost/Starward/releases). Giải nén nó, và chạy `Starward.exe` và làm theo hướng dẫn.

Starward có thể crash sau khi chạy trên một số thiết bị. Nếu bạn gặp phải vấn đề này, hãy tạo tập tin `config.ini` trong thư mục `Starward` và dán những dòng sau đây vào. Xem [docs/Configuration.vi-VN.md](./Configuration.vi-VN.md) để có thêm thông tin về `config.ini`.

``` ini
EnableConsole=False
UserDataFolder=.
```

## Bản địa hoá

[![en-US translation](https://img.shields.io/badge/dynamic/json?color=blue&label=en-US&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27en-US%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/en-US)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)

Starward sử dụng [Crowdin](https://crowdin.com/project/starward) cho việc bản địa hoá, cung cấp bản dịch máy tiếng Anh như bản gốc. Bạn có thể giúp chúng tôi dịch và sửa lỗi ngôn ngữ địa phương, và chúng tôi mong muốn có nhiều người tham gia hơn. Nếu bạn muốn tạo thêm ngôn ngữ dịch mới, vui lòng tạo issue.

## Phát triển

Để biên dịch dự án cục bộ, bạn cần cài đặt Visual Studio 2022 và chọn những workloads sau:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development

## Cảm ơn

Đầu tiên, tôi muốn dành lời cảm ơn đặc biệt đến [neon-nyan](https://github.com/neon-nyan), có dự án [Collapse](https://github.com/neon-nyan/Collapse) đã truyền cảm hứng cho dự án này. Starward không chỉ sử dụng một số tài nguyên do anh ấy tạo ra, mà còn bắt chước thiết kế giao diện người dùng. Tôi đã học được rất nhiều từ code của Collapse, và nó giúp quá trình phát triển của tôi suôn sẻ hơn nhiều.

Sau đó, cảm ơn CloudFlare vì đã cung cấp CDN miễn phí.

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

Và các thư viện bên thứ ba được sử dụng trong dự án này bao gồm:

-  [Dapper](https://github.com/DapperLib/Dapper)
-  [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
-  [HDiffPatch](https://github.com/sisong/HDiffPatch)
-  [MiniExcel](https://github.com/mini-software/MiniExcel)
-  [ScottPlot](https://github.com/ScottPlot/ScottPlot)
-  [Serilog](https://github.com/serilog/serilog)
-  [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
-  [Vanara PInvoke](https://github.com/dahall/Vanara)
-  [WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
-  [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit)

## Ảnh chụp màn hình

![screenshot](https://github.com/Scighost/Starward/assets/88989555/d02d1448-e2cb-4836-8d4c-a6e3962808f3)
