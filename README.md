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

![In-Game Details Pinned](Screens/in-game-detail-pins.png)

The craft & clear button is still a bit experimental, as
I had to copy a bit more code than wanted in order to do
all the necessary checks. So there is a chance that I
missed a few edge cases in that regard. Please report
here on GitHub if you find any issues with it.

[![GitHub CI Compile Status][4]][3]

## Grab ingredients QOL feature

With version 0.6.0 I've added another Quality of Life feature.
Loot containers (e.g. your own storage chests) and vehicle
containers get an additional button next to the "sort container"
button. When you press this "grab ingredient" button, it will try
to load as many items from the container as the pinned recipes
require. For even more convenience, this action is also available
by pressing `G` while the container is open. This key-binding
can be adjusted (or removed) in the XML config if required.

Currently the same button is also shown as a hand above the
pinned recipes. This is mostly to make users aware of this
feature and might be removed in the future. It may also be
helpful if your UI overwrites the item stack controller.

## Download and Install

Simply [download here from GitHub][2] and put into your A20 Mods folder:

- https://github.com/OCB7D2D/OcbPinRecipes/archive/master.zip (master branch)

## Changelog

### Version 0.6.0

- Add "grab ingredients" functionality
- Prepare for more compatibility patches

### Version 0.5.2

- Fix issue with persisting recipes

### Version 0.5.1

- Fix issues with new worlds/players

### Version 0.5.0

- Improves performance due to extensive caching
- Less overhead due to using more correct hooks
- Allows to change recipe count via mouse wheel
- Shows small red overlay for locked recipes

### Version 0.4.3

- Added Simplified Chinese translations (thx @future93)
- Automated deployment and release packaging

### Version 0.4.2

- Added Japanese translations (thx @RikeiR)

### Version 0.4.1

- Small UI adjustments for better readability
- [Alternative bigger UI][1] added (thx tdrhart)

### Version 0.4.0

- Fix issue when loading another map (reloading xui)
- Move window to the right side for less UI interference
- Reduce maximum amount of pinned recipes to 6
- Fix edge case with ingredients that have qualities
- Fix typo in controls.xml (pinned_recipe_ingredient_row)

### Version 0.3.4

- Moved `hasCraftArea` binding to root UI element
- Ditched fuel requirement to enqueue item to craft
- Moved some shared methods to PinRecipesManager
- Optimize updates to include all item stack changes
- Optimize and cache a few expensive calls

### Version 0.3.3

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

[1]: https://github.com/OCB7D2D/OcbPinRecipesUiTdrHart
[2]: https://github.com/OCB7D2D/OcbPinRecipes/releases
[3]: https://github.com/OCB7D2D/OcbPinRecipes/actions/workflows/ci.yml
[4]: https://github.com/OCB7D2D/OcbPinRecipes/actions/workflows/ci.yml/badge.svg
