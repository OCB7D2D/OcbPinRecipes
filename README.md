# OCB Pin Recipes Mod - 7 Days to Die (A20) Addon

A small harmony mod enabling to pin recipes on the UI.

Needs to be installed client and server side. And both need
EAC (Easy Anti-Cheat) to be turned off! There is no server-side
only version of this mod as it contains custom code. The mod
must also be installed on dedicated servers if you want to
use it in that setup, to persist pinned recipes for users.

![In-Game Pinned Recipes](Screens/in-game-screen-pins.jpg)

Tip: Set your UI foreground opacity in video settings to 95%.
Otherwise all foreground items are forced to be fully opaque.

![In-Game Details Pinned](Screens/in-game-left-pins.png)

The craft & clear button is still a bit experimental, as
I had to copy a bit more code than wanted in order to do
all the necessary checks. So there is a chance that I
missed a few edge cases in that regard. Please report
here on GitHub if you find any issues with it.

## Changelog

### Version 0.3.2

- Optimize CPU usage further (less fps drain)

### Version 0.3.2

- Fixed missing update when fuel is added
- Auto enable campfire when enqueuing recipe
- Add excess material label to show surplus

### Version 0.3.1

- Fix frame-drop issue (called update to often)
- Little UI cleanup (got rid of sprite backgrounds)

### Version 0.3.0

- Amount of items is now also pinned
- Added craft & clear recipe button
- Added increment/decrement buttons
- Only show action buttons when station is open
- Refactored data persisting to work on server
- Old pinned recipes will be lost on upgrade

### Version 0.2.0

- Persist pinned recipes over sessions
- Hide pinned recipes when game pauses
- Fixed improper recipe name displayed
- Improved UI look and feel a little
- Also allow to pin locked recipes
- Add tooltip for each ingredient
- Add static global manager class
- Recreated ULM pin icon from scratch

### Version 0.1.0

- Initial version

## Compatibility

I've developed and tested this Mod against version a20.b218.

[1]: https://github.com/OCB7D2D/A20BepInExPreloader