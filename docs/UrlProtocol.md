# URL Protocol

Other software even website could use url protocol `starward` to call some features of Starward. The url protocol is registered only when the user enables this feature in setting page.

![URL Protocol](https://user-images.githubusercontent.com/61003590/278273851-7c614cde-d8c4-403b-876e-cecc3570f684.png)


## Available features

The parameter `game_biz`  in the following is game region identifier and can be viewed in [GameBiz.cs](https://github.com/Scighost/Starward/blob/main/src/Starward.Core/GameBiz.cs).

| game_biz          | Value | Description                             |
| ----------------- | ----- | --------------------------------------- |
| None              | 0     | Default value                           |
| All               | 1     | All                                     |
| **GenshinImpact** | 10    | Genshin Impact                          |
| hk4e_cn           | 11    | Genshin Impact (Mainland China)         |
| hk4e_global       | 12    | Original Gods (Global)                  |
| hk4e_cloud        | 13    | Genshin Impact · Cloud (Mainland China) |
| hk4e_bilibili     | 14    | Genshin Impact (Bilibili)               |
| **StarRail**      | 20    | Honkai: Star Rail                       |
| hkrpg_cn          | 21    | Star Rail (Mainland China)              |
| hkrpg_global      | 22    | Star Rail (Global)                      |
| hkrpg_bilibili    | 24    | Star Rail (Bilibili)                    |
| **Honkai3rd**     | 30    | Honkai 3rd                              |
| bh3_cn            | 31    | Honkai 3rd (Mainland China)             |
| bh3_global        | 32    | Honkai 3rd (Europe & Americas)          |
| bh3_jp            | 33    | Honkai 3rd (Japan)                      |
| bh3_kr            | 34    | Honkai 3rd (Korea)                      |
| bh3_overseas      | 35    | Honkai 3rd (Southeast Asia)             |
| bh3_tw            | 36    | Honkai 3rd (Traditional Chinese)        |


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
