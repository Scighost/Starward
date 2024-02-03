> Phiên bản của tài liệu này là **v0.10.7**. Nếu phiên bản chưa được cập nhật, vui lòng tham khảo [tài liệu gốc](../README.md).

Trình khởi chạy trò chơi trên PC của [HoYoverse](https://www.hoyoverse.com) là một trong những phần mềm thương mại tệ nhất mà tôi từng thấy. Trải nghiệm người dùng tổng thể ở mức khá, nhưng nó hoạt động kém ở một số chi tiết nhất định:

- Thiếu hỗ trợ cho tỷ lệ màn hình mở rộng cao, dẫn đến thiếu tính thẩm mỹ trên toàn bộ giao diện.
- Xác minh tài nguyên sử dụng một luồng duy nhất, không thể sử dụng hiệu quả nhiều nhân, dẫn đến lãng phí thời gian đáng kể.
- Mặc dù có công cụ trình duyệt tích hợp nhưng thiết kế giao diện vẫn không thay đổi trong nhiều năm, không tận dụng được tính linh hoạt của các trang web mà thay vào đó là thêm số lượng lớn không cần thiết.

# Starward

> **Starward** xuất phát từ khẩu hiệu của Star Rail: May This Journey Lead Us **Starward**, rất phù hợp để dùng làm tên ứng dụng.
Starward là một trình khởi chạy mã nguồn mở của bên thứ ba được phát triển để giải quyết những thiếu sót nói trên. Nó hỗ trợ tất cả các trò chơi 
trên PC của HoYoverse và nhằm mục đích thay thế hoàn toàn trình khởi chạy chính thức. Ngoài những chức năng cơ bản của trình khởi chạy chính thức, tôi cũng sẽ tích hợp thêm một số tính năng dựa trên nhu cầu cá nhân, chẳng hạn như:

- Ghi lại thời gian chơi
- Chuyển đổi tài khoản HoYo
- Xem ảnh chụp màn hình trò chơi
- Lưu lịch sử gacha
- Hộp công cụ HoYoLAB

Còn nhiều tính năng khác để bạn khám phá...

## Cài đặt

Đầu tiên, thiết bị của bạn cần đáp ứng những điều kiện sau:

- Phiên bản Windows 10 1809 (17763) trở về sau.
- Đã cài đặt [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2).
- Để có trải nghiệm tốt hơn, vui lòng bật **Hiệu ứng trong suốt** và **Hiệu ứng động** trong cài đặt hệ thống.

Tiếp theo, tải xuống gói dành cho kiến ​​trúc CPU của bạn từ [GitHub Release](https://github.com/Scighost/Starward/releases). Giải nén nó, sau đó chạy `Starward.exe` và làm theo hướng dẫn.

## Dịch thuật

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
[![ja-JP translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/ko)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ru%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ru)
[![th-TH translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/th)
[![vi-VN translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](<https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json>)](https://crowdin.com/project/starward/zh-TW)

Starward sử dụng [Crowdin](https://crowdin.com/project/starward) cho công việc dịch văn bản trong ứng dụng. Bạn có thể đóng góp bằng cách giúp chúng tôi dịch và hiệu chỉnh nội dung bằng ngôn ngữ địa phương của bạn. Chúng tôi mong muốn có thêm nhiều người tham gia cùng chúng tôi.

[Hướng dẫn dịch thuật ở đây](./docs/Localization.md)

## Phát triển

Để biên dịch dự án cục bộ, bạn cần cài đặt Visual Studio 2022 và chọn những workloads sau:

- .NET Desktop Development
- C++ Desktop Development
- Universal Windows Platform Development

## Ủng hộ

Phát triển không phải là dễ dàng. Nếu bạn nghĩ Starward hữu ích, bạn có thể ủng hộ cho tôi qua https://donate.scighost.com.

## Cảm ơn

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

Đầu tiên, tôi xin gửi lời cảm ơn chân thành nhất tới tất cả những người đóng góp và dịch giả của dự án này. Starward chỉ có thể trở nên tốt hơn nhờ có bạn.

Sau đó, tôi muốn gửi lời cảm ơn đặc biệt tới [@neon-nyan](https://github.com/neon-nyan). Nguồn cảm hứng và thiết kế cho dự án này đến trực tiếp từ dự án [Collapse](https://github.com/neon-nyan/Collapse) của anh ấy. Mình đã thu được rất nhiều kiến ​​thức từ mã nguồn của Collapse, và với những tài liệu tham khảo quý giá như vậy, quá trình phát triển của mình đã suôn sẻ hơn rất nhiều

Tiếp theo, xin gửi lời cảm ơn sâu sắc đến nhà phát triển chính của [Snap Hutao](https://github.com/DGP-Studio/Snap.Hutao), [@Lightczx](https://github.com/Lightczx). Sự trợ giúp của anh ấy là vô giá trong quá trình phát triển Starward.

Ngoài ra, cảm ơn CloudFlare vì đã cung cấp dịch vụ CDN miễn phí, góp phần mang lại trải nghiệm cập nhật tuyệt vời cho mọi người.

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

Và các thư viện bên thứ ba được sử dụng trong dự án này bao gồm:

- [Dapper](https://github.com/DapperLib/Dapper)
- [GitHub Markdown CSS](https://github.com/sindresorhus/github-markdown-css)
- [HDiffPatch](https://github.com/sisong/HDiffPatch)
- [H.NotifyIcon](https://github.com/HavenDV/H.NotifyIcon)
- [HoYo-Glyphs](https://github.com/SpeedyOrc-C/HoYo-Glyphs)
- [MiniExcel](https://github.com/mini-software/MiniExcel)
- [ScottPlot](https://github.com/ScottPlot/ScottPlot)
- [Serilog](https://github.com/serilog/serilog)
- [SevenZipExtractor](https://github.com/adoconnection/SevenZipExtractor)
- [Vanara PInvoke](https://github.com/dahall/Vanara)
- [WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
- [WindowsCommunityToolkit](https://github.com/CommunityToolkit/WindowsCommunityToolkit)

## Ảnh chụp màn hình

<picture>
    <source srcset="https://github.com/phucho0237/Starward/assets/88989555/c31fd567-941a-414e-85fc-1b6ef3c0605d" type="image/avif" />
    <img src="https://github.com/phucho0237/Starward/assets/88989555/c31fd567-941a-414e-85fc-1b6ef3c0605d" />
</picture>

Hình nền từ [Pixiv@七言不绝诗](https://www.pixiv.net/artworks/113506129)
