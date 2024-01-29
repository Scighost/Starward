> Версия данного документа - **v0.10.7**. Если версия устарела, пожалуйста, обратитесь к [оригинальному документу](../README.md).  

Лаунчер для компьютерных игр [HoYoverse](https://www.hoyoverse.com) - одно из худших коммерческих программ, которые я когда-либо видел. В целом пользовательский опыт сносный, но в некоторых деталях он работает ужасно:

- Отсутствует поддержка высоких коэффициентов масштабирования, что приводит к туманной эстетике всего интерфейса.
- Проверка ресурсов использует один поток, неспособный эффективно использовать несколько ядер, что приводит к значительной потере времени.
- Несмотря на наличие встроенного движка браузера, дизайн интерфейса оставался неизменным в течение многих лет, не сумев извлечь выгоду из гибкости веб-страниц и, вместо этого, добавив ненужный объем.

# Starward

> **Starward** происходит от слогана Star Rail: May This Journey Lead Us **Starward**, который очень подходит для использования в качестве названия приложения.
> 
Starward - это сторонний лаунчер с открытым исходным кодом, разработанный для устранения вышеупомянутых недостатков. Он поддерживает все компьютерные игры на HoYoverse и призван полностью заменить официальный лаунчер. В дополнение к основным функциям лаунчера, я также включу некоторые дополнительные функции, основанные на индивидуальных потребностях, такие как:

Помимо загрузки и установки игры, включены следующие возможности:

-  Запись игрового времени
-  Смена игровго аккаунта
-  Просмотр скриншотов игры
-  Сохранение записей гачи
-  Набор инструментов HoYoLAB 

Планируются дополнительные функции...

## Установка

Во-первых, ваше устройство должно соответствовать следующим требованиям:

- Windows 10 1809 (17763) и выше
- Установлено [Visual C++ Runtime](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) 
- Установлено [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) 
- Для улучшения работы, пожалуйста, включите ** Эффекты прозрачности ** и ** Эффекты анимаций ** в системных настройках

Загрузите пакет для вашей архитектуры процессора с сайта [GitHub Release](https://github.com/Scighost/Starward/releases). Извлеките его, затем запустите `Starward.exe` и следуйте инструкциям.


## Локализация

[![en_US translation](https://img.shields.io/badge/any_text-100%25-blue?logo=crowdin&label=en-US)](https://crowdin.com/project/starward)
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

## Пожертвование

Разработка - дело нелегкое. Если вы считаете Starward полезным, вы можете пожертвовать мне на https://donate.scighost.com.

## Спасибо

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

Прежде всего, я хотел бы выразить свою искреннюю благодарность всем участникам и переводчикам этого проекта. Starward может стать лучше только благодаря вам.

Затем я хочу выразить свою особую благодарность [neon-nyan](https://github.com/neon-nyan). Вдохновение и дизайн для этого проекта взяты непосредственно из его проекта [Collapse](https://github.com/neon-nyan/Collapse). Я почерпнул много знаний из кода Collapse, и благодаря такой ценному источника информации мой процесс разработки прошел намного более гладко.

Далее, огромная благодарность основному разработчику [Snap Hutao](https://github.com/DGP-Studio/Snap.Hutao), [@Lightczx](https://github.com/Lightczx). Его помощь была бесценной в ходе разработки Starward."

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

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/40138340/8ae8270d-661a-42ab-90b4-e0fe8ce0529e" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/40138340/8ae8270d-661a-42ab-90b4-e0fe8ce0529e" />
</picture>

