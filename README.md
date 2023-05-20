# What is Starward?

**Starward** 出自星穹铁道开服前的宣传语———愿此行，终抵群星 (May This Journey Lead Us **Starward**)，虽然这不是一个正确的英文单词，但是很适合拿来用作应用名。本项目的灵感来自 [neon-nyan](https://github.com/neon-nyan) 大佬的米家启动器 [Collapse](https://github.com/neon-nyan/Collapse)，在此基础上我加入了自己需要但是 Collapse [不会实现的功能](https://github.com/neon-nyan/Collapse/blob/main/CONTRIBUTING.md#restrictions-for-new-features)。我要特别感谢 neon-nyan 和 Collapse，有此珠玉在前，我的开发过程顺利了很多。

Starward 是一个米家游戏启动器，它除了提供了统一的启动游戏的入口外，还包含以下功能：

- 切换已登录的游戏账号
- 浏览游戏截图
- 保存抽卡记录

这些功能支持的游戏有：

- 原神（国服，国际服，云原神）
- 崩坏：星穹铁道（国服，国际服）

更多的功能和游戏支持正在开发中。。。

> Starward 不会加入需要开发者持续更新游戏数据和资源的功能，比如给抽卡记录加上物品图片。

# 下载 & 更新

> 仅支持 Windows 10 1809 (17763) 及以上的版本

你可在 [Release](https://github.com/Scighost/Starward/releases) 页面下载最新发布的版本，应用使用增量更新的方式，既简单又便捷。

你还可以在[这里](https://github.com/Scighost/Starward/tree/metadata/dev)找到每次提交代码后自动生成的版本，但是不能保证稳定性和兼容性。

# 开发环境

在本地生成应用，你需要安装 Visual Studio 2022 并选择以下负载：

- .NET 桌面开发
- 使用 C++ 的桌面开发
- 通用 Windows 平台开发

单个组件中还需要勾选：

- Windows 应用 SDK C# 模板
- Windows 11 SDK (10.0.22621.0)
- MSVC v143 - VS 2022 C++ x64/x86 生成工具
- MSVC v143 - VS 2022 C++ ARM64 生成工具 (可选)

