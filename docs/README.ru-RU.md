# Что такое Starward?

**Starward** происходит от слогана Star Rail: May This Journey Lead Us **Starward**, который очень подходит для использования в качестве названия приложения. Starward - это игровой лаунчер, который поддерживает все компьютерные игры miHoYo. Цель этого проекта - полностью заменить официальный лаунчер, а затем добавить некоторые расширенные функции.

Помимо загрузки и установки игры, включены следующие возможности:

-  Запись игрового времени
-  Смена игровго аккаунта
-  Просмотр скриншотов игры
-  Сохранение записей гачи
-  Набор инструментов HoYoLAB 

Планируются дополнительные функции...

> Starward 1 не будут реализованы функции, требующие от разработчиков постоянного обновления игровых данных и ресурсов, таких как статистика для каждого события гачи.

## Установка

Во-первых, ваше устройство должно соответствовать следующим требованиям:

- Windows 10 1809 (17763) и выше
- Установлено [Visual C++ Runtime](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) 
- Установлено [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) 
- Для улучшения работы, пожалуйста, включите ** Эффекты прозрачности ** и ** Анимационные эффекты ** в системных настройках

Загрузите пакет для вашей архитектуры процессора с сайта [GitHub Release](https://github.com/Scighost/Starward/releases). Извлеките его, затем запустите `Starward.exe ` и следуйте инструкциям.

Starward может выйти из строя после запуска на некоторых устройствах. если вы столкнулись с этой проблемой, создайте файл `config.ini` в папке `Starward` и вставьте в него следующее. Смотрите [docs/Configuration.md](./Configuration.md) для получения дополнительной информации о `config.ini` .


``` ini
UserDataFolder=.
```


## Локализация

[![en-US translation](https://img.shields.io/badge/dynamic/json?color=blue&label=en-US&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27en-US%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/en-US)
[![ja-JP translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ja-JP&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ja%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ja)
[![ko-KR translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ko-KR&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ko%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ko)
[![th-TH translation](https://img.shields.io/badge/dynamic/json?color=blue&label=th-TH&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27th%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/th)
[![vi-VN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=vi-VN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27vi%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/vi)
[![zh-CN translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-CN&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-CN%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-CN)
[![zh-TW translation](https://img.shields.io/badge/dynamic/json?color=blue&label=zh-TW&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27zh-TW%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/zh-TW)
[![ru-RU translation](https://img.shields.io/badge/dynamic/json?color=blue&label=ru-RU&style=flat&logo=crowdin&query=%24.progress[?(@.data.languageId==%27ru%27)].data.translationProgress&url=https%3A%2F%2Fbadges.awesome-crowdin.com%2Fstats-15878835-595799.json)](https://crowdin.com/project/starward/ru)

Starward использует [Crowdin](https://crowdin.com/project/starward) для работы по локализации предоставьте машинный перевод английского текста в качестве оригинала. Вы можете помочь нам перевести и отредактировать текст на местном языке, и мы с нетерпением ждем участия большего числа людей. Если вы хотите добавить новый язык перевода, пожалуйста, создайте проблему.

## Разработка

Чтобы скомпилировать проект локально, вам необходимо установить Visual Studio 2022 и выбрать следующие Рабочие нагрузки:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development

## Спасибо

Прежде всего, я хотел бы поблагодарить[neon-nyan](https://github.com/neon-nyan) особенно, чей проект [Collapse](https://github.com/neon-nyan/Collapse) вдохновил на этот проект. Starward не только использовал некоторые созданные им ресурсы, но и имитировал дизайн пользовательского интерфейса. Я многому научился из кода Collapses, и это сделало мой процесс разработки намного более плавным.

Затем, спасибо CloudFlare за предоставление бесплатного CDN.

<img alt="cloudflare" width="300px" src="https://user-images.githubusercontent.com/61003590/246605903-f19b5ae7-33f8-41ac-8130-6d0069fde27a.png" />

И сторонние библиотеки, используемые в этом проекте, включают:

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

## Скриншот Лаунчера

![screenshot](https://github.com/Scighost/Starward/assets/88989555/8d83c941-cfcc-4655-8bf1-2ccd16792336)
