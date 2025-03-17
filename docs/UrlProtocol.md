# URL Protocol

Other software even website could use url protocol `starward` to call some features of Starward. The url protocol is registered only when the user enables this feature in setting page.

![URL Protocol](https://user-images.githubusercontent.com/61003590/278273851-7c614cde-d8c4-403b-876e-cecc3570f684.png)


## Available features

The parameter `game_biz`  in the following is game region identifier and can be viewed in [GameBiz.cs](https://github.com/Scighost/Starward/blob/main/src/Starward.Core/GameBiz.cs).

| game_biz (string) | Description                             |
| ----------------- | --------------------------------------- |
| hk4e_cn           | Genshin Impact (Mainland China)         |
| hk4e_global       | Original Gods (Global)                  |
| hk4e_bilibili     | Genshin Impact (Bilibili)               |
| hkrpg_cn          | Star Rail (Mainland China)              |
| hkrpg_global      | Star Rail (Global)                      |
| hkrpg_bilibili    | Star Rail (Bilibili)                    |
| bh3_cn            | Honkai 3rd (Mainland China)             |
| bh3_global        | Honkai 3rd (Global)                     |


### Start game

```
starward://startgame/{game_biz}?install_path={install_path}
```

**Acceptable query arguments**

|Key|Type|Description|
|---|---|---|
|install_path| `string` (Option) | Folder full path of game executable. |


### Record playtime

```
starward://playtime/{game_biz}?pid={pid}
```

**Acceptable query arguments**

|Key|Type|Description|
|---|---|---|
|pid| `int` (Option) | Game process id. |
