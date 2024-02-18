> 本文的版本是 **v1**，若版本落后请以[原文](../UrlProtocol.md)为准。
# URL 协议

其他软件甚至网站都可以使用 url 协议来调用 `starward` 的某些功能。只有当用户在设置页面中启用此功能时，才会注册 url 协议。

![URL协议](https://github.com/Scighost/Starward/assets/113306265/8928f65f-c1a1-45af-9c50-7f09d712a67e)


## 可用功能

以下的参数 `game_biz` 是游戏参数，可在 [docs/Configuration.md](./Configuration.md#game-regions) 中查看。

**可以使用的命令参数**

|值|类型|描述|
|---|---|---|
|uid| `number` | 启动前切换到特定帐户。 |
|install_path| `string` | 游戏本体所在的完整文件夹 |


### 启动游戏

```
starward://startgame/{game_biz}
```

### 记录游玩时间

```
starward://playtime/{game_biz}
```

### 测试URL协议 

```
starward://test/
```
使用这个url参数可以弹出url协议测试窗口
