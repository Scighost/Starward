# Starward

> **Starward** berasal dari slogan Star Rail: May This Journey Lead Us **Starward**, yang sangat cocok dijadikan nama aplikasi.

Starward adalah launcher pihak ketiga open-source yang dikembangkan untuk mengatasi kekurangan HoYoPlay (miHoYo Launcher). Starward mendukung semua game PC HoYoverse dan bertujuan menggantikan launcher resmi sepenuhnya. Selain fungsi dasar launcher, saya juga menambahkan beberapa fitur berdasarkan kebutuhan, seperti:

-  Mencatat waktu bermain game
-  Mengganti akun game
-  Melihat tangkapan layar game
-  Menyimpan catatan gacha

Masih banyak fitur lain yang bisa kamu jelajahi...


## Instalasi

Pertama, perangkatmu harus memenuhi persyaratan berikut:

- Windows 10 1809 (17763) atau lebih baru.
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2) sudah terpasang.
- [WebP Image Extension](https://apps.microsoft.com/detail/9pg2dk419drg) sudah terpasang.
- Untuk pengalaman yang lebih baik, aktifkan **Efek transparansi** dan **Efek animasi** di pengaturan sistem.

>[WebP Imaging Extension](https://apps.microsoft.com/detail/9pg2dk419drg) biasanya sudah tersedia di sistemmu. Jika aplikasi tidak menampilkan gambar latar belakang dengan benar, pastikan ekstensi tersebut sudah terpasang.

Selanjutnya, unduh paket sesuai arsitektur CPU-mu dari [GitHub Release](https://github.com/Scighost/Starward/releases). Ekstrak paket tersebut, lalu jalankan `Starward.exe` dan ikuti petunjuknya.


## Lokalisasi

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

Starward menggunakan [Crowdin](https://crowdin.com/project/starward) untuk lokalisasi teks di dalam aplikasi. Kamu dapat ikut berkontribusi dengan membantu menerjemahkan dan memeriksa konten dalam bahasamu. Kami menantikan lebih banyak orang yang bergabung.

Terjemahan Bahasa Indonesia saat ini dikelola langsung di repositori ini.

[Panduan Lokalisasi](./Localization.md)


## Pengembangan

Untuk mengompilasi proyek di perangkat lokal, kamu perlu memasang Visual Studio 2022 dan memilih workload berikut:

-  .NET Desktop Development
-  C++ Desktop Development
-  Universal Windows Platform Development


## Donasi

Pengembangan tidaklah mudah. Jika menurutmu Starward bermanfaat, kamu dapat berdonasi di https://donate.scighost.com.


## Ucapan Terima Kasih

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>

Pertama-tama, saya ingin menyampaikan terima kasih yang sebesar-besarnya kepada semua kontributor dan penerjemah proyek ini. Starward dapat menjadi lebih baik hanya karena kalian.

Selanjutnya, saya ingin menyampaikan terima kasih khusus kepada [@neon-nyan](https://github.com/neon-nyan). Inspirasi dan desain proyek ini berasal langsung dari proyeknya, [Collapse](https://github.com/neon-nyan/Collapse). Saya banyak belajar dari kode Collapse, dan dengan referensi yang begitu berharga, proses pengembangan saya menjadi jauh lebih lancar.

Kemudian, terima kasih banyak kepada pengembang utama [Snap Hutao](https://github.com/DGP-Studio/Snap.Hutao), [@Lightczx](https://github.com/Lightczx). Bantuannya sangat berharga selama pengembangan Starward.

Selain itu, terima kasih kepada [CloudFlare](https://www.cloudflare.com/) yang menyediakan layanan CDN gratis dan [SignPath Foundation](https://signpath.org/) yang menyediakan penandatanganan kode gratis untuk proyek open-source.

<img alt="cloudflare" height="72px" src="https://github.com/user-attachments/assets/c1fba88e-4cd1-45df-b681-bf5634215f41" />&nbsp;&nbsp;&nbsp;&nbsp;<img alt="signpath foundation" height="72px" src="https://github.com/user-attachments/assets/052c654a-13fa-4e7a-8f1d-9f57c83f438b" />

Dan [pustaka pihak ketiga](./ThirdParty.md) yang digunakan dalam proyek ini.


## Tangkapan Layar

<img width="1200" src="https://github.com/user-attachments/assets/d1704d44-fadd-4672-aade-c09584b7f16c" />
