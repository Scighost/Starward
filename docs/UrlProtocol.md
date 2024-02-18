English | [简体中文](./UrlProtocol.zh-CN.md) | [Tiếng Việt](./UrlProtocol.vi-VN.md)
# URL Protocol

Other software even website could use url protocol `starward` to call some features of Starward. The url protocol is registered only when the user enables this feature in setting page.

![URL Protocol](https://user-images.githubusercontent.com/61003590/278273851-7c614cde-d8c4-403b-876e-cecc3570f684.png)


## Available features

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

### Test Url Protocol

```
starward://test/
```
Use this URL parameter to pop up a URL protocol testing window。
