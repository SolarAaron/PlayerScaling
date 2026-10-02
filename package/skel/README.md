# PlayerScaling
This mod was created to allow for the map to be expanded to accomodate larger groups, and also has options for making the 

## Config
<details>
  <summary>Click me to expand</summary>

Feature enable toggles immediately remove or reinstall this mod's own Harmony patches when changed.

### Global Scaling Multiplier
Multiplies the player scaling factor by this number.
### Player Scaling Minimum
The player scaling factor is set to 1 unless there are at least this many players, or downscaling is enabled.
### Player Scaling Divisor
The player scaling factor will go up by 1 each time the number of players goes up by this amount. Decrease for more player scaling, increase for less.
### Down Scaling Enabled
Disabled by default, enable if you want the map to get smaller for fewer players.
### Minimum Downscaling Multiplier
The lowest the multiplier will go for downscaling, default 0.5x. I don't recommend putting this below 0.5 as it's untested and likely to break the game.
### Map Scaling Enabled
If this is disabled, the map-generation patches are not installed and map size remains vanilla. The map scaling multiplier is ignored.
### Map Iterator Rewrite Enabled
Controls the map-generation iterator transpiler. Disable this to test the map prefix without the custom iterator rewrite; the game's original module and extraction logic will be used.
### Map Scaling Multiplier
Multiplies the map size by this amount while Map Scaling is enabled.
### Enemy Scaling Enabled
Controls whether the enemy-count scaling patch is installed.
### Enemies Scaling Multiplier
Changes how quickly enemies increase in number while Enemy Scaling is enabled. Higher means they will grow in numbers more quickly, but will not change their maximum amount.
### Max Enemy Density
This affects the maximum number of total enemies you will see per map module (the big squares on the map). Default is in line with vanilla. Set higher to have the enemy count continue scaling past round 11.
### Valuable Scaling Enabled
Controls whether valuable-density and cosmetic world-object roll scaling patches are installed.
### Valuables Scaling Multiplier
Multiplies the amount of valuables by this amount while Valuable Scaling is enabled.
### Difficulty Scaling Enabled
If this is disabled, the difficulty-scaling patch is not installed and difficulty remains vanilla. The difficulty multiplier and offset are ignored.
### Difficulty Scaling Multiplier
Multiplies the difficulty which mostly affects room types and enemy spawning delay while Difficulty Scaling is enabled.
### Difficulty Scaling Offset
Adds to the difficulty to have it start slightly higher.
</details>

## Map Size
The map size scales linearly with players and will break past the normal limits. You will also see more than 4 extracts on these larger maps.

## Enemy Count and Valuables
Enemies and Valuables are multiplied in accordance with the map size changes, so you can expect to see a similar density of enemies and valuables as with a normal map, but there will be more overall.

## General Difficulty Scaling
The Game's general difficulty scaling will be accelerated for larger groups, but more subtly than other factors. Specifically the difficulty is multiplied by the square root of the player scaling factor.

## Other recommended mods
I highly recommend all of these mods for large groups for a better experience! <br>
[UpgradeEveryRound](https://thunderstore.io/c/repo/p/Redfops/UpgradeEveryRound/) is another mod by me that allows each player to get one free upgrade every time you visit the shop. <br>
[MoreShopItems_Updated](https://thunderstore.io/c/repo/p/Jettcodey/MoreShopItems_Updated/) is a mod by Jettcodey, an updated version of the original by GalaxyMods, that increases the number of available items in the shop.
