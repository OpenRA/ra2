## Player
options-tech-level =
    .infantry-only = Infantry Only
    .low = Low
    .medium = Medium
    .unrestricted = Unrestricted

checkbox-redeployable-mcvs =
    .label = Redeployable MCVs
    .description = Allow undeploying Construction Yard

## World
options-starting-units =
    .mcv-only = MCV Only
    .light = Light
    .medium = Medium
    .heavy = Heavy

## ai.yaml
actor-player-modularbot-testai-name = Test AI

## aircraft.yaml
actor-shad =
    .name = Night Hawk
    .description =
    Infantry Transport Helicopter
    Undetectable by radar.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft

actor-shadhusk-name = Night Hawk
actor-zep-name = Kirov Airship
actor-zephusk-name = Kirov Airship

actor-orca =
    .name = Harrier
    .description =
    Fast assault fighter
      Strong vs Buildings, Vehicles
      Weak vs Infantry, Aircraft

actor-orcahusk-name = Harrier
actor-beag-name = Black Eagle
actor-beaghusk-name = Black Eagle
actor-pdplane-name = Cargo Plane
actor-pdplanehusk-name = Cargo Plane
actor-hornet-name = Hornet
actor-hornethusk-name = Hornet
actor-asw-name = Osprey

actor-aswhusk =
    .name = Osprey
    .norow--name = Osprey

## allied-infantry.yaml
actor-engineer =
    .description =
    Captures enemy structures.
      Unarmed
    .name = Engineer

actor-dog =
    .description =
    Anti-infantry unit.
    Can detect cloaked units and spies.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    .name = Attack Dog

actor-e1 =
    .description =
    General-purpose infantry.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    .name = G.I.

actor-snipe =
    .name = Sniper
    .description =
    Special anti-infantry unit.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft

actor-spy =
    .description =
    Infiltrates enemy structures for intel or
    sabotage. Exact effect depends on the
    building infiltrated.
      Unarmed
    Special Ability: Disguises
    .disguisetooltip-name = Spy
    .disguisetooltip-generic-name = Soldier

actor-ghost =
    .description =
    Elite commando infantry, armed with
    a sub machine gun and C4.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    Special Ability: Destroy Building with C4
    .name = Navy SEAL

actor-ccomand =
    .description =
    Elite commando infantry, armed with
    a sub machine gun and C4.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    Special Ability: Destroy Building with C4
    .name = Chrono Commando

actor-ptroop =
    .description =
    Psychic infantry. Can mind control enemy units.
      Strong vs Infantry, Vehicles, Buildings
      Weak vs Dogs, Terror Drones, Aircraft
    Special Ability: Destroy Building with C4
    .name = Psi Commando

actor-tany =
    .description =
    Elite commando infantry, armed with
    dual pistols and C4.
      Strong vs Infantry, Buildings
      Weak vs Vehicles, Aircraft
    Special Ability: Destroy Building with C4

    Maximum 1 can be trained.
    .name = Tanya

actor-jumpjet =
    .description =
    Airborne soldier.
      Strong vs Infantry, Aircraft
      Weak vs Vehicles
    .name = Rocketeer

actor-jumpjet-husk-name = Rocketeer

actor-cleg =
    .name = Chrono Legionnaire
    .description =
    High-tech soldier.
     Strong vs Infantry, Vehicles
     Weak vs Aircraft

## allied-naval.yaml
actor-lcrf =
    .description =
    General-purpose naval transport.
    Can carry infantry and vehicles.
      Unarmed
    .name = Amphibious Transport

actor-dest =
    .description =
    Allied Main Battle Ship armed with cannons and
     an Osprey helicopter.
    Can detect submarines and sea animals.
      Strong vs Naval units
      Weak vs Ground units, Aircraft
    .name = Destroyer

actor-aegis =
    .description =
    Anti-Air naval unit.
      Strong vs Aircraft
      Grounds units, Ships
    .name = Aegis Cruiser

actor-dlph =
    .description =
    Trained dolphin
    armed with sonic beams.
      Strong vs Ships
    .name = Dolphin

actor-carrier =
    .description =
    Aircraft carrier ship.
      Strong vs Tanks, Structures
      Weak vs Infantry
    .name = Aircraft Carrier

## allied-structures.yaml
actor-gacnst =
    .description = Builds base structures.
    .name = Construction Yard

actor-gapowr =
    .description = Provides power for other structures.
    .name = Power Plant

actor-gapile =
    .description = Trains infantry.
    .name = Barracks

actor-garefn =
    .description = Processes ore into credits.
    .name = Ore Refinery

actor-gaairc =
    .description =
    Provides radar
    Supports 4 aircraft.
    .name = Airforce Command Headquarters

actor-amradr =
    .name = American Airforce Command Headquarters
    .paratrooperspower-paratroopers-name = American Paratroopers
    .paratrooperspower-paratroopers-description =
    A Cargo Plane drops eight GIs
    anywhere on the map.

actor-gaweap =
    .description = Produces vehicles.
    .name = War Factory

actor-gayard =
    .name = Naval Yard
    .description =
    Produces and repairs ships,
    submarines, transports, and other naval units.

actor-gadept =
    .description = Repairs vehicles and removes Terror Drones (for a price).
    .name = Service Depot

actor-gatech =
    .description = Allows deployment of advanced units.
    .name = Battle Lab

actor-gawall =
    .description =
    Light wall.
    Crushable by vehicles.
    .name = Allied Wall

actor-gapill =
    .description = Automated anti-infantry defense.
    .name = Pill Box

actor-nasam =
    .description = Automated anti-aircraft defense.
    .name = Patriot Missile System

actor-gtgcan =
    .description = Automated, long ranged anti-ground defense.
    .name = Grand Cannon

actor-gaorep =
    .description = Refines all forms of income by 25%.
    .name = Ore Purifier

actor-gaspysat =
    .description = Reveals the entire battlefield.
    .name = Spy Satellite Uplink

actor-gagap =
    .name = Gap Generator
    .description =
    Obscures the enemy's view with shroud.
    Requires power to operate.

actor-gaweat =
    .description = Play God with deadly weather!
    .name = Weather Controller
    .weathercontrolsupportpower-lightningstorm-name = Weather Storm
    .weathercontrolsupportpower-lightningstorm-description = Control the weather to destroy enemy forces.

actor-gacsph =
    .description = Allows teleporting units in a 3x3 array.
    .name = Chronosphere
    .chronoshiftpower-chronoshift-name = Chronosphere
    .chronoshiftpower-chronoshift-description =
    Teleports a group of units across
    the map.

actor-atesla =
    .description =
    Advanced base defense.
    Requires power to operate.
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    .name = Prism Tower

actor-power-name = Power Plant
actor-refinery-name = Ore Refinery
actor-barracks-name = Infantry Production
actor-radar-name = Radar
actor-repairpad-name = Service Depot

## allied-vehicles.yaml
actor-amcv =
    .description = Deploys into a Construction Yard.
    .name = Mobile Construction Vehicle

actor-cmin =
    .description =
    Gathers Ore.
      Unarmed
    Special ability: Can teleport to own refineries
    .name = Chrono Miner

actor-mtnk =
    .name = Grizzly Battle Tank
    .description =
    Allied Main Battle Tank.
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft

actor-tnkd =
    .name = Tank Destroyer
    .description =
    Special anti-armor unit.
      Strong vs Vehicles, Ships
      Weak vs Infantry, Aircraft

actor-fv =
    .name = Infantry Fighting Vehicle
    .description =
    Multi-Purpose Vehicle.
    Without passenger:
      Strong vs Infantry, Aircraft
      Weak vs Vehicles, Ships
    Special Ability: Armament depends on passenger.

actor-sref =
    .description =
    Fires deadly beams of light.
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    .name = Prism Tank

actor-mgtk =
    .description =
    As tree disguised tank.
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    .name = Mirage Tank
    .miragetooltip-tree-name = Mirage Tank
    .miragetooltip-tree-generic-name = Tree

## animals.yaml
actor-cow-name = Cow
actor-all-name = Alligator
actor-polarb-name = Polar Bear
actor-josh-name = Monkey

## bridges.yaml
actor-cabhut-name = Bridge Repair Hut
meta-lowbridgeramp-name = Wooden Bridge
actor-lobrdb-a-name = Concrete Bridge
actor-lobrdb-a-d-name = Dead Bridge
actor-lobrdb-b-name = Concrete Bridge
actor-lobrdb-b-d-name = Dead Bridge
actor-lobrdb-r-se-name = Bridge Ramp
actor-lobrdb-r-nw-name = Bridge Ramp
actor-lobrdb-r-ne-name = Bridge Ramp
actor-lobrdb-r-sw-name = Bridge Ramp
actor-lobrdg-a-d-name = Dead Bridge
actor-lobrdg-b-d-name = Dead Bridge
actor-lobrdg-r-se-name = Bridge Ramp
actor-lobrdg-r-nw-name = Bridge Ramp
actor-lobrdg-r-ne-name = Bridge Ramp
actor-lobrdg-r-sw-name = Bridge Ramp
meta-elevatedbridgeplaceholder-name = Concrete Bridge
actor-bridgb1-name = Wooden Bridge
actor-bridgb2-name = Wooden Bridge

## civilian-naval.yaml
actor-tug-name = Tug Boat
actor-cruise-name = Cruise Ship
actor-cdest-name = Coast Guard Boat

## civilian-props.yaml
actor-camisc01-name = Barrels
actor-camisc02-name = Barrel
actor-camisc03-name = Dumpster
actor-camisc04-name = Mailbox
actor-camisc05-name = Pipes
actor-camisc06-name = V3 Ammunition
actor-camsc11-name = Tires
actor-camsc12-name = Practice Target
actor-camsc13-name = Derelict Tank
actor-ammocrat-name = Ammo Crates
actor-camsc01-name = Hot Dog Stand
actor-camsc02-name = Beach Umbrellas
actor-camsc03-name = Beach Umbrellas
actor-camsc04-name = Beach Towels
actor-camsc05-name = Beach Towels
actor-camsc06-name = Camp Fire
actor-caeuro05-name = Statue
actor-capark01-name = Park Bench
actor-capark02-name = Swing Set
actor-capark03-name = Merry Go Round
actor-castrt01-name = Traffic Light
actor-castrt02-name = Traffic Light
actor-castrt03-name = Traffic Light
actor-castrt04-name = Traffic Light
actor-castrt05-name = Bus Stop
actor-camov01-name = Drive In Movie Screen
actor-camov02-name = Drive In Movie Concession Stand
actor-pole01-name = Utility Pole
actor-pole02-name = Utility Pole
actor-hdstn01-name = AlringtonStones
actor-spkr01-name = Drive-In Speaker
actor-cakrmw-name = Kremlin Walls
actor-gagate-a-name = Guard Border Crossing

## civilian-structures.yaml
actor-cafncb-name = Black Fence
actor-cafncw-name = White Fence
actor-gasand-name = Sandbags
actor-cafncp-name = Prison Camp Fence
actor-cawt01-name = Water Tower
actor-cats01-name = Twin Silos
actor-cabarn02-name = Barn
actor-cawash01-name = White House
actor-cawsh12-name = Washington Monument
actor-cawash14-name = Jefferson Memorial
actor-cawash15-name = Lincoln Memorial
actor-cawash16-name = Smithsonian Castle
actor-cawash17-name = Smithsonian Natural History Museum
actor-cawash18-name = White House Fountain
actor-cawash19-name = Iwo Jima Memorial
actor-canewy04-name = Statue of Liberty
actor-canewy05-name = World Trade Center
actor-canewy20-name = Warehouse
actor-canewy21-name = Warehouse
actor-caswst01-name = Southwest Building
actor-caarmy01-name = Army Tent
actor-caarmy02-name = Army Tent
actor-caarmy03-name = Army Tent
actor-caarmy04-name = Army Tent
actor-cafarm01-name = Farm
actor-cafarm02-name = Farm Silo
actor-cafarm06-name = Lighthouse
actor-causfgl-name = US Flag
actor-carufgl-name = Russian Flag
actor-cairfgl-name = Iraqi Flag
actor-capofgl-name = Polish Flag
actor-caskfgl-name = South Korean Flag
actor-calbfgl-name = Libyan Flag
actor-cafrfgl-name = French Flag
actor-cagefgl-name = German Flag
actor-cacufgl-name = Cuban Flag
actor-caukfgl-name = British Flag
actor-cacolo01-name = Air Force Academy Chapel
actor-caind01-name = Factory
actor-calab-name = Einstein's Lab
actor-cagas01-name = Gas Station
actor-galite-name = Light Post
actor-city05-name = Battersea Power Station
actor-catech01-name = Communications Center
actor-catexs02-name = Alamo
actor-capars01-name = Eiffel Tower
actor-capars07-name = Phone Booth
actor-capars10-name = Bistro
actor-capars11-name = Arc de Triumphe
actor-capars12-name = Notre Dame
actor-capars13-name = Bistro
actor-capars14-name = Bistro
actor-cafrma-name = Farmhouse
actor-cafrmb-name = Outhouse
actor-caprs03-name = Louvre
actor-cagard01-name = Guard Shack
actor-carus01-name = St. Basil's Cathedral
actor-carus02g-name = Kremlin Wall Clock Tower
actor-carus03-name = Kremlin Palace
actor-camiam04-name = Lifeguard Hut
actor-camiam08-name = Arizona Memorial
actor-camex01-name = Mayan Pyramid
actor-camex02-name = Mayan Castillo
actor-camex03-name = Mayan Minor Temple
actor-camex04-name = Mayan Large Temple
actor-camex05-name = Mayan Platfrom
actor-caeur1-name = Cottage
actor-caeur2-name = Cottage
actor-cachig04-name = Associates Center
actor-cachig05-name = Sears Tower
actor-cachig06-name = Water Tower
actor-castl05a-name = Stadium
actor-castl05b-name = Stadium
actor-castl05c-name = Stadium
actor-castl05d-name = Stadium
actor-castl05e-name = Stadium
actor-castl05f-name = Stadium
actor-castl05g-name = Stadium
actor-castl05h-name = Stadium
actor-camsc07-name = Hut
actor-camsc08-name = Hut
actor-camsc09-name = Hut
actor-camsc10-name = McBurger Kong
actor-cabunk01-name = Concrete Bunker
actor-cabunk02-name = Concrete Bunker

## civilian-vehicles.yaml
actor-bus-name = School Bus
actor-limo-name = Presidential Limousine
actor-pick-name = Pickup Truck
actor-car-name = Automobile
actor-wini-name = Recreational Vehicle
actor-propa-name = Propaganda Truck
actor-cop-name = Police Car
actor-euroc-name = European Car
actor-cona-name = Excavator
actor-trucka-name = Truck
actor-truckb-name = Truck
actor-suvb-name = Black SUV
actor-suvw-name = White SUV

actor-stang =
    .name = Mustang
    .generic-name = Sports Car

actor-ptruck-name = Pickup Truck
actor-taxi-name = Taxi

## civilians.yaml
actor-civa-name = Texan
actor-civb-name = Texan
actor-civc-name = Texan
actor-civbbp-name = Baseball Player
actor-civbfm-name = Beach Fat Male
actor-civbf-name = Beach Fat Female
actor-civbtm-name = Beach Thin Male
actor-civsfm-name = Snow Fat Male
actor-civsf-name = Snow Fat Male
actor-civstm-name = Snow Thin Male
actor-vladimir-name = Vladimir
actor-pentgen-name = Pentagon General
actor-ssrv-name = Secret Service
actor-pres-name = President

## defaults.yaml
meta-civbuilding-name = Civilian Building

meta-civilianinfantry =
    .name = Civilian
    .generic-name = Civilian

meta-civvehicle-generic-name = Civilian Vehicle
meta-husk-generic-name = Destroyed Aircraft
meta-ship-generic-name = Ship
meta-oredrill-name = Ore Drill
meta-tree-name = Tree
meta-streetsign-name = Street Sign
meta-trafficlight-name = Traffic Light
meta-streetlight-name = Street Light
meta-rock-name = Rock

meta-crate =
    .name = Crate
    .generic-name = Crate

## misc.yaml
actor-mpspawn-name = (Multiplayer Spawnpoint)
actor-waypoint-name = (Waypoint for scripted behavior)
actor-camera-name = (reveals area to owner)
actor-ingalite-name = (Invisible Light Post)
actor-neglamp-name = (Invisible Negative Light Post)
actor-inyelwlamp-name = (Invisible Yellow Light Post)
actor-inpurplamp-name = (Invisible Purple Light Post)
actor-inoranlamp-name = (Invisible Orange Light Post)
actor-ingrnlmp-name = (Invisible Green Light Post)
actor-inredlmp-name = (Invisible Red Light Post)
actor-inblulmp-name = (Invisible Blue Light Post)

## soviet-infantry.yaml
actor-e2 =
    .description =
    Cheap rifle infantry.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    .name = Conscript

actor-flakt =
    .description =
    Anti-Air/Anti-Infantry unit.
      Strong vs Aircraft, Infantry
      Weak vs Vehicles
    .name = Flak Trooper

actor-shk =
    .description =
    Special armored unit using electricity.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
    Special ability: Charge tesla coils
    .name = Tesla Trooper

actor-terror =
    .description =
    Carries C4 charges taped to his body and kamikazes enemies
    blowing them up quickly and efficiently.
      Strong vs Ground units
      Weak vs Aircraft
    .name = Terrorist

actor-deso =
    .description =
    Carries a radiation-emitting weapon.
    Can deploy for area-of-effect damage.
      Strong vs Infantry, Light armor
      Weak vs Tanks, Aircraft
    .name = Desolator

actor-ivan =
    .description = Specialist for explosives. Can plant a Bomb on anything, even Cows.
    .name = Crazy Ivan

actor-civan =
    .description = Specialist for explosives. Can plant a Bomb on anything, even Cows. Can teleport on anywhere on the map.
    .name = Chrono Ivan

actor-yuri =
    .description =
    Psychic infantry. Can mind control enemy units.
    Can be deployed to unleash a powerful psychic wave.
      Strong vs Infantry, Vehicles
      Weak vs Terror Drones, Aircraft, Buildings
    .name = Yuri

actor-yuripr =
    .description =
    Psychic infantry. Can mind control enemy units from a great range.
    Can be deployed to unleash a powerful psychic wave.
      Strong vs Infantry, Vehicles
      Weak vs Terror Drones, Aircraft, Buildings

    Maximum 1 can be trained.
    .name = Yuri Prime

## soviet-naval.yaml
actor-sapc =
    .description =
    General-purpose naval transport.
    Can carry infantry and vehicles.
      Unarmed
    .name = Amphibious Transport

actor-sub =
    .description =
    Submerged anti-ship unit armed with
    torpedoes.
    Can detect other submarines and Giant Squids.
      Strong vs Ships
      Weak vs Ground units, Aircraft
    Special Ability: Submerge
    .name = Typhoon Attack Sub

actor-hyd =
    .description =
    Anti-Air/Anti-Infantry naval unit.
      Strong vs Aircraft, Infantry
      Weak vs Vehicles, Naval Units
    .name = Sea Scorpion

actor-sqd =
    .description =
    Ocean creature
    punches enemies in close combat.
      Strong vs Ships
    .name = Giant Squid

## soviet-structures.yaml
actor-nacnst =
    .description = Allows construction of base structures.
    .name = Construction Yard

actor-napowr =
    .description = Provides power for other structures.
    .name = Tesla Reactor

actor-nahand =
    .description = Produces infantry.
    .name = Barracks

actor-narefn =
    .description = Processes ore into credits.
    .name = Ore Refinery

actor-naradr =
    .description = Provides radar.
    .name = Radar Tower

actor-naweap =
    .description = Produces vehicles.
    .name = War Factory

actor-nayard =
    .name = Naval Yard
    .description =
    Produces and repairs ships,
    submarines, transports, and other naval units.

actor-nadept =
    .description = Repairs vehicles and removes Terror Drones (for a price).
    .name = Service Depot

actor-nanrct =
    .description = Provides power for other structures.
    .name = Nuclear Reactor

actor-natech =
    .description = Allows deployment of advanced units.
    .name = Battle Lab

actor-naclon =
    .description = Clones most trained infantry.
    .name = Cloning Vats

actor-napsis =
    .description = Detects enemy units and strikepoints
    .name = Psychic Sensor

actor-nairon =
    .description =
    Grants invulnerability to armored units.
    Fries fleshy units.
    .name = Iron Curtain Device
    .grantexternalconditionpower-ironcurtain-name = Iron Curtain
    .grantexternalconditionpower-ironcurtain-description =
    Makes a group of units invulnerable
    for 20 seconds.

actor-namisl =
    .description =
    Provides an atomic bomb.
    Requires power to operate.
      Special Ability: Atom Bomb
    Maximum 1 can be built.
    .name = Nuclear Missile Silo
    .nukepower-name = Nuclear Missile
    .nukepower-description =
    Launches a devastating atomic bomb
    at a target location.

actor-nawall =
    .description =
    Light wall.
    Crushable by vehicles.
    .name = Soviet Wall

actor-naflak =
    .description = Automated anti-aircraft defense.
    .name = Flak Cannon

actor-tesla =
    .description =
    Advanced base defense.
    Requires power to operate.
      Strong vs Infantry, Vehicles
      Weak vs Aircraft
    .name = Tesla Coil

actor-nalasr =
    .description = Automated anti-infantry defense.
    .name = Sentry Gun

## soviet-vehicles.yaml
actor-smcv =
    .description = Deploys into a Construction Yard.
    .name = Mobile Construction Vehicle

actor-harv =
    .description =
    Gathers Ore.
      Strong vs Infantry
      Weak vs Vehicles, Aircraft
    .name = War Miner

actor-dron =
    .name = Terror Drone
    .description =
    Strong vs Infantry, Vehicles
      Weak vs Aircraft

actor-htk =
    .name = Flak Track
    .description =
    Infantry Transport and Anti-Air/Anti-Infantry vehicle.
      Strong vs Aircraft, Infantry
      Weak vs Vehicles

actor-htnk =
    .name = Rhino Heavy Tank
    .description =
    Soviet Main Battle Tank.
      Strong vs Vehicles
      Weak vs Infantry, Aircraft

actor-apoc =
    .name = Apocalypse Tank
    .description =
    Soviet Advanced Battle Tank with Double Barrel
    and Anti-Aircraft Missile Launcher.
      Strong vs Vehicles, Aircraft
      Weak vs Infantry

actor-ttnk =
    .name = Tesla Tank
    .description =
    Russian special tank armed with dual small Tesla Coils.
      Strong vs Vehicles, Infantry
      Weak vs Aircraft

actor-dtruck =
    .description = Demolition Truck, actively armed with nuclear explosives.
    .name = Demolition Truck

## tech-structures.yaml
actor-caoild-name = Tech Oil Derrick

actor-caairp =
    .name = Tech Airport
    .paratrooperspower-allies-name = Allied Paratroopers
    .paratrooperspower-allies-description =
    A Cargo Plane drops six GIs
    anywhere on the map.
    .paratrooperspower-soviets-name = Soviet Paratroopers
    .paratrooperspower-soviets-description =
    A Cargo Plane drops nine conscripts
    anywhere on the map.

actor-cahosp-name = Civilian Hospital
actor-cathosp-name = Tech Hospital
actor-caoutp-name = Tech Outpost

## world.yaml
meta-baseworld =
    .faction-random-name = Random
    .faction-random-description =
    Random Country
    A random country will be chosen when the game starts.
    .faction-allies-name = Allies
    .faction-allies-description =
    Random Allied Country
    A random Allied country will be chosen when the game starts.
    .faction-soviets-name = Soviets
    .faction-soviets-description =
    Random Soviet Country
    A random Soviet country will be chosen when the game starts.
    .faction-1-name = America
    .faction-1-description =
    America
    Special Ability: Paratroopers
    .faction-2-name = Germany
    .faction-2-description =
    Germany
    Special Vehicle: Tank Destroyer
    .faction-3-name = England
    .faction-3-description =
    England
    Special Infantry: Sniper
    .faction-4-name = France
    .faction-4-description =
    France
    Special Building: Grand Cannon
    .faction-5-name = Korea
    .faction-5-description =
    Korea
    Special Aircraft: Black Eagle
    .faction-6-name = Cuba
    .faction-6-description =
    Cuba
    Special Infantry: Terrorist
    .faction-7-name = Libya
    .faction-7-description =
    Libya
    Special Vehicle: Demolition Truck
    .faction-8-name = Iraq
    .faction-8-description =
    Iraq
    Special Infantry: Desolator
    .faction-9-name = Russia
    .faction-9-description =
    Russia
    Special Vehicle: Tesla Tank

resource-minerals = Valuable Minerals