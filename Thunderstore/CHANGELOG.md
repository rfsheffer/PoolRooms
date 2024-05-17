**<details><summary>Version 0.1.22</summary>**

* Update for LethalLevelLoader API updates

</details>

**<details><summary>Version 0.1.21</summary>**

* Size increase and details pass on the entrance
* Updated to the latest versions of support libraries (LethalLevelLoader, LethalLib)
* Fixed some misaligned vents
* Fixed some nav mesh in the walls of the wave room...

</details>

**<details><summary>Version 0.1.20</summary>**

* Overall dungeon size reduction
* Fire exits added to more tiles
* Upped to Evaisa 0.15.0

</details>

**<details><summary>Version 0.1.19</summary>**

* Fix for spraypaint by @drako1245
* Fix for water hindrance by @drako1245

</details>

**<details><summary>Version 0.1.18</summary>**

* Art improvement for the entrance
* Long lockers will no longer always have an openable locker door
* AI pathing improvements and fixes
* Fire exits will now only show up in a couple "room" types, no tunnels
* Loot distribution changes

</details>

**<details><summary>Version 0.1.17</summary>**

* Added collision to the props and trusses above the wave room, and it isn't so safe up there anymore...
* Pool lights turn red when pulling the apparatus
* Added some more extra/random details
* New long room

</details>

**<details><summary>Version 0.1.16</summary>**

* Changed "all", "custom", "modded", and "vanilla" configMoons to use dynamicLevelTagsList which should fixing the interior spawning on Custom moons
* More art details
* Reduced the dungeon main path length and branching lengths a bit
* Detecting teleports out of water and stopping the water splashing behavior when noticed
* Added radar rendering blockers on hallway blockers and to mask out 4 way and labyrinth non-traversable areas

</details>

**<details><summary>Version 0.1.15</summary>**

* Truss details added to all large rooms
* Details pass in locker rooms
* Connections will now be generated between rooms more often
* General generation tweaking based on feedback and testing

</details>

**<details><summary>Version 0.1.14</summary>**

* Updated LLL dependency to 1.1.6
* Minimum value of MaxGenerationScale set to 1.0
* Added a Nav mesh obstacle component with carve to all tile blockers just in case two rooms are close enough that nav mesh would be generated between them

</details>

**<details><summary>Version 0.1.13</summary>**

* Wet floor sign scrap
* Bathrooms
* Pool Area art and design pass
* New Labyrinth tunnel room type

</details>

**<details><summary>Version 0.1.12</summary>**

* Pool Rooms will now defer to Lethal Level Loaders moon configuration by default (generateAutomaticConfigurationOptions). You can turn this off by setting UsePoolRoomsMoonsConfig to true.
* Pool Rooms configuration now supports "Custom" moons. You can still name them individually if you choose but you can include "Custom:100" for example in the comma sep list and it will tell Lethal Level Loader to add all custom moons with a weight of 100. The "All" identifier has had "Custom" added to it as well so by default the mod adds all vanilla moons and now all custom moons.

</details>

**<details><summary>Version 0.1.11</summary>**

* Hopeful fix for the incompatibility with LethalThings (Index out of range exception)
* Added tuning for the Interiors custom scrap. Can now set each scraps weight, if it shows up at all, and if it should show up in all maps not just PoolRooms

</details>

**<details><summary>Version 0.1.10</summary>**

* Added a bit of extra room depth to the entrance outward from the entrance door so other rooms wont be generated on the other side of the entrance door
* All prefabs are now setup to be fixed by LethalLevelLoader instead of my own scripts, not including RandomMapObject spawners
* Improvements to level weight parsing from config
* Dungeon fire escape counts are handled by LethalLevelLoader now
* Improvements to setting up item groups for scrap and adding the custom items

</details>

**<details><summary>Version 0.1.9</summary>**

* Setting Dungeon min / max to default clamped 1.0 to 2.5 respectively.

</details>

**<details><summary>Version 0.1.8</summary>**

* Fixed sounds playing everywhere

</details>

**<details><summary>Version 0.1.7</summary>**

* Fixed splashing sounds playing outside of the facility if teleporting from a fire escape while standing in water
* Reduced the volume on splashing sounds
* Fixed fire escape teleport triggers not properly aligned to the doors
* Added controlling the Min and Max scale applied to the dungeon generation. Defaults to 1.0

</details>

**<details><summary>Version 0.1.6</summary>**

* Hopeful fix for the console spam

</details>

**<details><summary>Version 0.1.5</summary>**

* Fix for incorrect LethalLevelLoader version in the manifest

</details>

**<details><summary>Version 0.1.4</summary>**

* Thunderstore Release

</details>

**<details><summary>Version 0.1.3</summary>**

* Initial Thunderstore release
* Now using Lethal Level Loader
* Highly customizable settings for which moons to show Pool Rooms
* First appearance dungeon music
* General Tweaks and fixes

</details>

**<details><summary>Version 0.1.2</summary>**

* Sauna room end cap room will show up off of locker rooms
* The apparatus room will always have doors to it giving an even higher chance of a locked door
* More tweaks to the generation

</details>

**<details><summary>Version 0.1.1</summary>**

* Pit fall / Mechanical room now has geometry and a proper skill testing jump to make. No textures yet but they are coming.
* General tuning to try and push the fire exit more into the level

</details>

**<details><summary>Version 0.1.0</summary>**

* The plugin has reached a pretty far stage of development and just needs polish now.
* Added player water interaction. As the player moves through water rooms they make splashes and wading sounds and particles to show their movement.
* Numerous bug fixes and improvements during testing
* Added lockers to blocked paths in the pump room. The room itself is getting an art pass next.

</details>
