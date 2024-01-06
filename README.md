# Pool Rooms Dungeon for Lethal Company
A Work-in-progress mod for Lethal Company featuring a dungeon layout all about water and classic swimming pool asthetic.

This repo is templated from https://github.com/rfsheffer/LethalDungeon which it will cherry pick merge to and from.

# How to build and install
- Pull this repo to a folder to work in
- Grab the DunGen plugin from here: https://assetstore.unity.com/packages/tools/utilities/dungen-15682
- Place the DunGen folder into the projects Assets folder, do not replace anything as the .meta files need to be maintained as they are
- Open the Unity project in unity 2022.3.9f1
- Run from the drop down "Lethal Dungeon/Build Dungeon Bundle". This will generate the bundle into Assets\DungeonBundles
- Grab the bundle "exampledungeon" for later
- In your lethal company folder where BepinEx is create a folder called "LethalDungeon" in BepInEx\plugins
- Copy "Plugin\Prebuilt\LethalDungeon.dll" into the "LethalDungeon"
- Copy "exampledungeon" into the "LethalDungeon"
- Make sure to have LethalLib and HookGenPatcher installed into your BepInEx plugins
- Run the game and close. You can now modify "BepInEx\config\LethalDungeon.cfg" to assign the dungeon to the moons you would like using the "Moons" setting

# Checking how the dungeon will look before opening Lethal Company
In Unity open the GenerateDungeon scene and run it. From this you can see what it will look like in game minus the post process effects Lethal Company has.

# Dependencies
- You will need a copy of the plugin DunGen to use this project. The files must be placed in Assets/DunGen. Do not replace the .meta files already in there.
- LethalLib : https://github.com/EvaisaDev/LethalLib

# Attributions and licenses
The plugin is based on Badhamknibbs SCP dungeon code which can be found here:
 https://github.com/Badhamknibbs/SCPCB_DunGen_LC

The code of this mod is provided under the MIT license (see license file)
