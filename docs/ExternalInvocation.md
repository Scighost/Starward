# External Invocation

Starward currently supports two external invocation methods:

- URL Protocol
- Command Line

The parameter `game_biz`  in the following is game region identifier and can be viewed in [GameBiz.cs](https://github.com/Scighost/Starward/blob/main/src/Starward.Core/GameBiz.cs).

| game_biz (string) | Description                     |
| ----------------- | ------------------------------- |
| hk4e_cn           | Genshin Impact (Mainland China) |
| hk4e_global       | Genshin Impact (Global)         |
| hk4e_bilibili     | Genshin Impact (Bilibili)       |
| hkrpg_cn          | Star Rail (Mainland China)      |
| hkrpg_global      | Star Rail (Global)              |
| hkrpg_bilibili    | Star Rail (Bilibili)            |
| bh3_cn            | Honkai 3rd (Mainland China)     |
| bh3_global        | Honkai 3rd (Global)             |

## URL Protocol

Other software or websites can use the `starward` URL protocol to call some features of Starward. The URL protocol is registered only when the user enables this feature in the settings page.

![URL Protocol](https://user-images.githubusercontent.com/61003590/278273851-7c614cde-d8c4-403b-876e-cecc3570f684.png)

### Start game

```
starward://startgame/{game_biz}?install_path={install_path}
```

**Acceptable query arguments**

| Key | Type | Description |
| --- | --- | --- |
| install_path | `string` (Optional) | Full folder path of the game executable location. |

### Record playtime

```
starward://playtime/{game_biz}?pid={pid}
```

**Acceptable query arguments**

| Key | Type | Description |
| --- | --- | --- |
| pid | `int` (Optional) | Game process id. |

## Command Line

Starward also supports direct Command Line invocation through command arguments handled in the application entry point.

> [!NOTE]
> Ensure that the `Starward.exe` you invoke is the one in the same directory as `version.ini`.

### Start game

```
Starward.exe startgame --biz={game_biz} --install_path="{install_path}"
```

**Acceptable arguments**

| Key | Type | Description |
| --- | --- | --- |
| `--biz` | `string` (Required) | Game region identifier. Refer to `game_biz` |
| `--install_path` | `string` (Optional) | Full folder path of the game executable location. |

If install path is not provided, Starward will try to use the locally stored install path.

### Update game

```
Starward.exe updategame --action={action} --biz={game_biz} --install_path="{install_path}"
```

**Acceptable arguments**

| Key | Type | Description |
| --- | --- | --- |
| `--action` | `string` (Required) | One of: `check`, `update`, `repair`. |
| `--biz` | `string` (Required) | Game region identifier. Refer to `game_biz` |
| `--install_path` | `string` (Optional) | Full folder path of the game executable location. |

> [!NOTE]
> When `--action` is `repair` or `update`, Starward will run silently in the background until the task finishes.


