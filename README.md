# Links
- Read more about my mods at [Website](https://cotlmod.infernodragon.net/)
- Join the discord server for support, feedback, suggestions and general modding talk: [modding discord](https://discord.gg/MUjww9ndx2)
- [Save Converter](https://cotlminimod.infernodragon.net/saveconverter) (if you are updating from 0.1.6 COTL API to 0.1.7 COTL API and above) 
- If you like the mod, consider donating at [kofi](https://ko-fi.com/infernodragon0) or (new) [patreon](https://www.patreon.com/InfernoDragon0)! Thank you for checking out the mod!

### Notes
NOTE: You must click one of the rally flags again after every run to allow your followers to join the fight. All of the art in this mod are currently placeholders. You can contribute to the art by DMing me on discord: InfernoDragon1

# Supercharged Series: Followers

![image](https://i.imgur.com/9oZbUBS.png)

### v1.0.3 - [AI Improvements]
### 1 New Structure
- Added a Super Rally Flag to rally or un-rally all followers

### Balancing
- Commander will now strike 3 times per attack
- Reduced delay between attacks to 0.6 seconds (from 1 second)

### Performance improvements & Bugfixes
- Rewritten (again) the follower AI to only have one per follower
- Improved pathfinding and reduced delays for each follower finding enemies
- Should continue fighting if new enemies spawn after the original enemies are cleared
- Removed white flashing & camera shake from followers attacking
- Follower brain no longer stop working at random chances

---

### Structures
- Rally Flag: Left click to rally your followers to battle. Right click to set a commander for your followers
- Barracks: Left click to change your follower class for different sets of bonuses! Right click for Prestige leveling

## Features

### Rallying your followers
- You can bring your followers as actual fighters into battle, they will find the nearest enemy and start attacking them for you.
- You can rally as many followers as you want to bring them into battle.
- Different bonus stats are provided to followers based on the equipment you provide to them, stated below. 

### Prestige Levels
- Gain Prestige by bringing followers to battle, for each follower alive when the run ends, gain 1 Prestige (maximum 12 per run)
- Give prestige to the followers to boost stats.
- Up to 10 prestige levels per follower
- View and give prestige in the barracks. prestige required per level: 3/6/9/12/15/20/40/60/80/100

## Equipment & Bonuses for your followers
### Commander Boosts (Set Commander via Rally Flag)
- Only 1 commander can be set, via the Rally Flag
- Attack +3
- Health +10
- Attack Speed +1
- Movement Speed +9
- Regen Per Room +1.5
- Double the follower size

### Necklace Boosts
- Feather: Movement Speed +2
- Flower: Attack Speed +1
- Moon: Damage +1
- Nature: Regen 0.25 health per room clear
- Skull: Health +3
- Golden Skull: Revives on room clear (except boss room)

### Class Boosts (Set class via Barracks)
- Missionary: Movement Speed +2, Health +1
- Holiday: Health +4
- Warrior: Damage +2, Health +2
- Prayer: Damage +4
- Undertaker: Regenerates 0.5 heart on finishing a room

### Prestige Boosts (Level Prestige via Barracks)
- Level 1: Base attack increase by 0.5 damage, Base health increases by 0.5 hearts
- Level 2: Min/Max Attack delay reduced by 0.25 seconds, Base health increases by 0.5 hearts
- Level 3: Movement Speed increases by 25%, Base health increases by 1 hearts
- Level 4: Base attack increase by another 0.5 damage, Base health increases by 0.5 hearts
- Level 5: Min/Max Attack delay reduced by another 0.5 seconds, Base health increases by 0.5 hearts
- Level 6: Regenerates 0.5 hearts when killing an enemy
- Level 7: 20% chance of dropping blue half hearts when killing an enemy
- Level 8: Regenerates a small amount of Curse and Relic charge when hitting an enemy
- Level 9: 10% chance of dropping Prestige when killing an enemy
- Level 10: 10% chance of dealing crit damage (5x damage) on a hit

# Credits

### Developer
- [InfernoDragon0](https://github.com/InfernoDragon0)

### Tester
- danylopez123: for testing the many iterations of fixing the follower's combat brain

### How to Contribute
Like testing mods or contributing art? DM me on Discord: Infernodragon1 or join the discord linked above!

# Changelog

### v1.0.1
- Potentially fix an issue with followers loading early and not pathfinding anymore

### v1.0.2 - [Optimization & Bugfixes]
- Performance improvements by removing stacking coroutines, reducing the chances of crashing
- Teleport followers into the map again if out of bounds
- Added config to change transparency of followers and commander (Defaults to 0.5 transparency, set to 1 for fully opaque)
- Improved pathfinding for followers, preventing them from hitting air
- Custom Random Targetting priority instead of closest target
- Fixed an issue where prestige is given instead of taken if follower was max level
- Fixed Regeneration per room not working as expected