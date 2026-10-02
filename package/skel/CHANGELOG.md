# Changelog

## [2.0.0] - 2026-10-1

### Fixed
- Now compatible with latest steam release version
- Added Cosmetic spawn scaling matching valuable scaling

## [1.2.4] - 2025-6-15
### Fixed
- Now compatible with latest beta version thanks to [SolarAaron](https://github.com/SolarAaron) PR [#11](https://github.com/cadyn/PlayerScaling/pull/11)

## [1.2.3] - 2025-6-1
### Fixed
- Valuable scaling now actually works, previously the dll was uploaded without the patch.

## [1.2.2] - 2025-5-31
### Fixed
- Valuable scaling now works again in the beta thanks to [SolarAaron](https://github.com/SolarAaron) PR [#10](https://github.com/cadyn/PlayerScaling/pull/10)

## [1.2.1] - 2025-5-21

### Fixed
- Change in code made mod previously incompatible with beta branch. Fixed thanks to [SolarAaron](https://github.com/SolarAaron) PR [#9](https://github.com/cadyn/PlayerScaling/pull/9)

## [1.2.0] - 2025-5-5

### Added

- Options for downscaling for fewer players
- Options for helping to continue to increase the difficulty past the default limit of 11 days.

### Tweaked

- Refocused towards map scaling, with other scaling being based off it.
- Config options removed and added to meet this goal.
- Honed in on trying to make this a vanilla plus experience by default with customization for the player to make the experience however they like it.

### Fixed

- Game crash/freeze when loading levels past a certain point. This was due to a bit of a silly error on my part and should no longer be an issue outside of players tweaking the config to make the map huge.
- Some potential oddities in level generation that resulted from the mistake, including having fewer extractions than expected.

## [1.1.0] - 2025-3-29
### Added

- Config options for disabling or changing how scaling works