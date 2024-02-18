

# URL 协议

其他软件甚至网站都可以使用 url 协议来调用 Starward 的某些功能。只有当用户在设置页面中启用此功能时，才会注册 url 协议。

![URL协议](https://github.com/Scighost/Starward/assets/113306265/8928f65f-c1a1-45af-9c50-7f09d712a67e)


## 可用功能

The parameter `game_biz`  in the following is game region identifier and can be viewed in [docs/Configuration.md](./Configuration.md#game-regions).

### Start game

```
starward://startgame/{game_biz}
```

**Acceptable query arguments**

|Key|Type|Description|
|---|---|---|
|uid| `number` | Switch to specific account before startup. |
|install_path| `string` | Full folder of game executable. |


### Record playtime

```
starward://playtime/{game_biz}
```

