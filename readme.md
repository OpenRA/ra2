![screenshot](http://i.imgur.com/HxQWvXw.png)

##Roadmap:

1) Stay true to vanilla RA2 (for now)

2) Implement factions in order

3) Implement actor types in order for each faction

Factions:
* America
* Korea
* France
* Germany
* Great Britain
* Libya
* Cuba
* Iraq
* Russia

Actors:
* Structures (power plant → tech center)
* Support structures (walls, defenses, etc)
* Vehicles
* Infantry
* Aircraft
* Naval
* Special Units (including infiltration unlocks)
* Civilian actors
* Tech structures

After all actors are in the game we can focus on support powers / superweapons.

Reference links:
* [Allied units](http://cnc.wikia.com/wiki/Allied_Units_and_Structures_Summary#Great_World_War_III_.28Red_Alert_2.29)
* [Soviet units](http://cnc.wikia.com/wiki/Soviet_Units_and_Structures_Summary#Great_World_War_III_.28Red_Alert_2.29)
* [Icons](http://xhp.xwis.net/ra2_icons/)

##Development Notes:
* `mk` anims are in isotemp.mix (or isosno.. etc)
* `gg`/`ng` anims are in generic.mix
* `icon`s are in cameo.mix
* infantry are in conquer.mix

Sequence 'Tick' properties are the number of milliseconds per frame.

Tile type indices:
* 00 = Clear
* 01 = Ice
* 02 = Unknown
* 03 = Unknown
* 04 = Unknown
* 05 = Tunnel
* 06 = Railroad
* 07 = Rock
* 08 = Unknown
* 09 = Water
* 10 = Beach
* 11 = Road
* 12 = Some LAT Transitions
* 13 = Mostly Slopes
* 14 = Rough
* 15 = Cliff

Wall, Tiberium, and Weeds are somewhere in this list,
  even though they are overlays and not tiletypes,
  because they affect actor movement speed.

[Tutorial: Converting Westwood maps (TS/RA2) to OpenRA](https://gist.github.com/Phrohdoh/ab2524fe46b05b029d4b).

