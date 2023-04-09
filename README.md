# UniversalUnityDemosaics

This is the plugin of BepInEx what can some uncensored unity games. (soft uncensor)

Overview:
General Demosaics for Unity (BepInEx plugin)
This is UNOFFICIAL plugin. I was steal the source from ManlyMarco / UniversalUnityDemosaics. ( https://github.com/ManlyMarco/UniversalUnityDemosaics )

Features:
Removes mosaics on several games.

Installation:
copy correct dll file(mono/IL2CPP) to BepInEx \ plugins


It is based on ManlyMarco / UniversalUnityDemosaics
It's plugin for BepInEx. So need BepInEx.


About IL2CPP version of BepInEx
Recently, BepInEx Bleeding Edge (BE) builds release #6xx versions. But #6xx version is not compatible with BepInEx plugins(5.x ver).
Use #533.
Or use BepInEx 6.0.0-pre.1

Tested on
    IL2CPP : Holy Knight Ricca, Succubus Heaven, AiKagura
    Mono : AGH(奉課後輪姦中毒), Rambo Hentai(Rampant Pervert vs the Public Discipline Committee), SecretSurvey, Fallen Princess Knight, Object Control(may be no need, It has patched resource), MGBuster(魔法少女(メスガキ)はやっぱりバトルでわからせたい by JSK Koubou), Imperial Harem, Oni Boku, 代わりの女 , Yami’s Wonder Mansion, OshikakeShoujo, Masochistic Male Bullying Classroom 3D, 父と娘のすやすやセックス, SuccubusReborn, ...


May be works on other unity games. And It makes little bit frame drop.

Put it on plugins folder(BepInEx \ plugins) and run game, config file is created on BepInEx/Config folder. It's like

    [Config]

    ## Parts of strings what compared with. (Ricca needs VColMosaic)
    # Setting type: String
    # Default value:
    FilterStrings = mozaic,mosaic,mozaik,mosaik,pixelate,censor,cenzor,masaike

    ## Find Unlit material and replace censored one with unlit shader what found. (Fallen Princess Knight)
    # Setting type: Boolean
    # Default value: false
    UseUnlitMaterial = false

    ## To must remove material or shader's filter (Elf Knight Giselle needs 'MoveMoza')
    # Setting type: String
    # Default value:
    ForceRemoveFilter = movemoza

    ## Dumps materials and shaders name. Once runned, changed it to 'false'.
    # Setting type: Boolean
    # Default value: false
    NameDumpMode = false



    May be don't need to edit. Options are...

    FilterStrings
    The keywords what will be searched. You can add/remove keywords. ScanType = 0 uses default value as
    Some games are must to be edited.
    ex) Ricca need just "VColMosaic". Rambo hentai is need to remove "mosaic ". It removes building on title screen.

    UseUnlitMaterial
    Some games uses Live2D and uses Unlit shader. If set to true, save a unlit shader and replace it to shader which name have a censored one.
    MICO games, and some games uses that method.
    If games was uncensored, but precious things are disappeared from screen, use this option. (ex. Succubus Reborn)

    ForceRemoveFilter
    Force to remove filters. ex) 'Elf Knight Giselle' has uncensored textures. but shader isn't. Can remove "moza" FilterStrings.
    but Giselle' important area is blocked by another texture. So use "movemoza" to ForceRemoveFilter, It can be resolved.

    NameDumpMode
    You can dump materials/shaders names to file. If once used, The value is changed to "false". 


Changes Log
v1.4.4.0
    Config file's location is moved to BepInEx/Config folder.
    
v1.4.3.0
    Support more old unity version. (working on unity v4.5.5)
    Removed "maximumLOD". It's value is constant -2..
    New "ForceRemoveFilter" config. Force to remove material/shader what matches it. (same use way with "FilterStrings")
    New "NameDumpMode" config. It can dump materials/shaders names to "DumbRendererDemosaic.csv" file.(append mode)

v1.4.2.6
    Removed "ScanType" in setting file
    Removed "LoopTimeLimit" in setting file.
    Added "UseUnlitMaterial" in setting file.
    Fixed a bug repeat remove shader what has been removed.

v1.4.2.5
    Change the find method of "Unlit shader". (check shader's name contains "cubism")

v1.4.2.4
    Renderer index management changed. Reset "check index" on changes on Renderer.

v1.4.2.3
    Sometime not working at screen change.
    Fix error on Gallery to Title screen. (when Renderers counts changed nnn to 0)

v1.4.2.2
    debug little bit bug. It's appeared on Lord Knight Complex. H-scene with monster is no problem. but just dress off on alone, demosaic not works.
