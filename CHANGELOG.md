# Changelog

## 0.9.7

- Added configurable keyboard and mouse bindings through the standard BepInEx configuration system.
- Added independent keyboard and mouse entries for every action; either binding can be disabled by setting it to `None`.
- The configuration is generated at `BepInEx/config/Zombified_Initiative.cfg` on first launch.

## 0.9.6

- Sentry bots are here. Stops follow and travel actions by setting status `queued`, and restores them by setting status `none`.
- Use the Q action menu to tell a bot or all bots to toggle sentry mode. They will no longer follow you, but will shoot enemies in combat.
- This will not work too well if BetterBots teleporting is active, but that can be disabled in the BetterBots configuration.
- Do not let sentry bots run out of ammo. Hitting 0% on the special weapon might stop them from using their main weapon too, making them completely useless.

## 0.9.5

- Bug fixes for resource drops and hotkeys not working correctly.
- Clients can now zombify too, using the Network API.
- Hotkeys are locked while not in first-person mode, including while using terminals, menus, and the Q menu.
- Known caveat: do not lock bot slots while in the lobby before a mission. Something evil happens and Zombified Initiative bricks. Not fixing.

## 0.9.4

- Partial rewrite of the logic; functions are split into a MonoBehaviour added to each bot.
- `action.Remove` is back, but actions are handled better and anti-bot-bricking measures are in place.
- The menu now has an AllBots submenu for issuing commands to every bot.
- Q → 6 → 6 contains AllBots at position 0, followed by every bot that has been seen, with space for eight entries.
- LobbyExpander support is untested and not guaranteed.

## 0.9.3

- Replaced `action.Remove` with `StopAction`. Thanks to falcon86 for the feedback, suggestion, and testing.

## 0.9.2

- Moved the Zombified Initiative Q menu under “6 WHAT I NEED” to avoid a conflict with ChatterReborn.

## 0.9.1

- Hotfix for the Q menu.
- Plugin-added text datablocks now use `localizationService.Add`, even though only English text is provided. Thanks to the people sharing GitHub code, especially ChatterReborn.

## 0.9.0

- Built the Q menu.
- All bots, including Woods, are always listed, but only bots currently present lead to a working submenu.
- Supports bots leaving and rejoining; menu entries are enabled or disabled approximately every three seconds.

## 0.3.2

- Major fix for the previous resource-related bug.
- Rewrote the bot-finding code so it runs every two seconds instead of only once per game.

## 0.3.1

- Minor fix for a resource-related bug.

## 0.3.0

Art says:

> In this new version: Fixed raycasting through the walls when marking enemies to kill. J enables/disables automatic resource pickups, disabled by default as before. K enables/disables automatic resource use, disabled by default as before. Added Zombified Initiative to the Q communication menu. When it is activated, you can see a tiny button in the top-left corner where you choose a bot for further actions.
>
> And those further actions, well... they must be what the mod already does using all those custom keys, but unfortunately it is where I leave developing this mod at this point :> Hopefully some more experienced developers who already know GTFO's insides will pick it up. Happy to help :)

New source code: [Pastebin](https://pastebin.com/K23mybyJ) (included).

## 0.0.2

Art says:

> I've completely reworked the mod and made it better extendable for others to pick up where I've left. It works wa-a-a-a-a-ay better than the previous version.

New source code: [Pastebin](https://pastebin.com/26NwQTPQ) (`source_code.txt` updated).

## 0.0.1

- Initial release.
