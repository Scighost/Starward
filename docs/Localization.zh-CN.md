> 本文的版本是 **v1**，若版本落后请以[原文](./Localization.md)为准。

# 本地化

首先，我要向本项目的所有贡献者致以最诚挚的感谢。因为你们的无私贡献，Starward 可以被世界各地不同语言的人们使用。无论你的贡献是一行代码、一个翻译修正，或是一个建议，你们的工作都为我们的项目增色不少。在这个充满活力的社区中，每一个人都是不可或缺的一部分。

<picture>
    <source srcset="https://github.com/Scighost/Starward/assets/61003590/9d369ec3-ab7c-408f-88c2-11bfe4453208" type="image/avif" />
    <img src="https://github.com/Scighost/Starward/assets/61003590/44552992-e2c5-451f-9c2a-73176e8e4e93" width="240px" />
</picture>


## 翻译指南

如果您想为本项目的本地化工作做出贡献，请阅读接下来的内容。本项目中需要翻译的内容有两部分，GitHub 仓库中的文档和应用内的文本。


## 文档翻译

本文所说的文档是仓库中的 Markdown 文件。不是所有的文档都需要翻译，需要翻译的文档的头部会有指向其他语言版本的链接。例如：

**v1** | English | [简体中文](./Localization.zh-CN.md)

开始翻译前，先删除源文档头部指向其他语言版本的链接，然后添加以下内容。版本号是上方示例中的加粗内容，括号内是您正在翻译的源文档的相对路径。

> 本文的版本是 **v1**，若版本落后请以[原文](./Localization.md)为准。

部分文档中可能会包含图片，为了尽可能限制仓库大小，本项目的仓库中不允许添加大量图片。如果您想要本地化这些图片，你可以创建一个 [issue](https://github.com/Scighost/Starward/issues) 并上传图片，然后替换文档中的链接。

翻译完成后，请通过 pull request 提交回本仓库。


## 应用内文本翻译

Starward 的应用内文本翻译托管在 [Crowdin](https://crowdin.com/project/starward) 平台上，您可以随时修改其中的文本内容。如果您想增加一个新的翻译语言，请创建一个 [issue](https://github.com/Scighost/Starward/issues)。

您在 Crowdin 中做出的修改会在一个小时内同步到 [l10n/main](https://github.com/Scighost/Starward/tree/l10n/main) 分支，并触发自动构建流程。在 [GitHub Actions](https://github.com/Scighost/Starward/actions/workflows/build.yml) 中找到最新的名为 `New Crowdin updates` 的 workflow，下载编译完成的二进制文件（Artifacts），您可以及时地检查翻译文本在应用内的显示效果。**开发中的版本可能会损坏您的个人数据库 `StarwardDatabase.db`，请做好备份后再测试，此版本不应该长时间使用。**

我个人的英文水平有限，难免会出现各种错误。但是 Crowdin 中无法自由修改源文本，如果您认为英文文本有需要修改的地方，请通过 pull request 提交您的修改内容。

