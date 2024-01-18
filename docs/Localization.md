**v1** | English | [简体中文](./Localization.zh-CN.md) | [Tiếng Việt](./Localization.vi-VN.md)

# Localization

Firstly, I would like to express my sincerest thanks to all contributors to this project. Thanks to your selfless contributions, Starward can be used by people in different languages ​​around the world. Whether your contribution is a line of code, a translation correction, or a suggestion, your work adds significant value to the project. Everyone is an indispensable part of this vibrant community.

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>


## Translation Guidance

If you wish to contribute to the localization efforts of this project, please read the following information. There are two parts of the project that require translation: the documentation stored in the GitHub repository and the text in the application.


## Document Translation

The documents mentioned in this article are Markdown files in the repository. Not all documents need to be translated. The headers of documents that need to be translated will have links to other language versions. For example:

**v1** | English | [简体中文](./Localization.zh-CN.md)

Before you commence with the translation, please remove the header links pointing to versions in other languages from the source document. Afterward, add the following content. The version number is the bolded content in the example above, and in brackets is the relative path to the source document you are translating.

> The version of this document is **v1**. If the version is behind, please refer to [original document](./Localization.md).

Some documents may include images. To keep the repository size manageable, large number of images to this project's repository is not allowed. If you wish to localize these images, you can create an [issue](https://github.com/Scighost/Starward/issues), upload the images, and then replace the links in the document.

After the translation is completed, please submit it back to this repository through a pull request.


## In-app Text Translation

The in-app text translation for Starward is hosted on the [Crowdin](https://crowdin.com/project/starward) platform, where you can modify the text content at any time. If you would like to add a new translation language, please create an [issue](https://github.com/Scighost/Starward/issues).

The changes you make on Crowdin will be synchronized to the [l10n/main](https://github.com/Scighost/Starward/tree/l10n/main) branch within an hour, triggering an automated build process. Find the latest workflow named `New Crowdin updates` in [GitHub Actions](https://github.com/Scighost/Starward/actions/workflows/build.yml) and download the compiled binary file (Artifacts). You can check the display effect of translated text in the app in real time. **The version under development may corrupt your personal database `StarwardDatabase.db`. Please make a backup before testing. This version should not be used for a long time.**

As my proficiency in English is limited, there might be various errors. Since the source text cannot be freely modified on Crowdin, if you find any areas in the English text that need correction, please submit your modifications through a pull request.

