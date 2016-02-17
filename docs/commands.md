# StormBot command table.
This file was automatically generated at 17-02-2016 13:43:35 UTC.


### Preface
This document contains every command, that has been registered in the CommandService system, their paramaters, their desciptions and their default permissions.
Every command belongs to a cetain module. These modules can be enabled and disabled at will using the Modules module. Each comamnd is seperated into their parent modules command table.



Each and every one of these commands can be triggered by saying `}<command>` or `@StormBot-Dev <command>`

## Commands

#### Dynamic Permissions
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`dynperm set` |  `[-]` | Sets the dynamic permissions for this server.**Pastebin links are supported.**Use the dynperm help command for more info. | ServerAdmin
`dynperm show` |  | Shows the Dynamic Permissions for this server. | ServerAdmin
`dynperm clear` |  `<areyousure>` | Clears the Dynamic Permissions. This cannot be undone. Pass yes as an argument for this to work. | ServerAdmin
`dynperm help` |  | help | ServerAdmin

#### Bot
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`io save` |  | Saves data used by the bot. | BotOwner
`io load` |  | Loads data that the bot loads at runtime. | BotOwner
`set name` |  `[-]` | Changes the name of the bot | BotOwner
`set avatar` |  `<name>` | Changes the avatar of the bot | BotOwner
`set game` |  `[-]` | Sets the current played game for the bot. | BotOwner
`killbot` |  | Kills the bot. | BotOwner
`gc` |  | Lists used memory, then collects it. | BotOwner
`gc collect` |  | Calls GC.Collect() | BotOwner
`gencmdmd` |  |  | BotOwner
`join` |  `<invite>` | Joins a server by invite. | User
`leave` |  | Instructs the bot to leave this server. | User
`cleanmsg` |  | Removes the last 100 messages sent by the bot in this channel. | User
`gc list` |  | Calls GC.GetTotalMemory() | User

#### Server Management
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`channel list text` |  | Lists all text channels in server | ServerAdmin
`channel list voice` |  | Lists all voice channels in server | ServerAdmin
`channel prune` |  | Deletes the last 100 messages sent in this channel. | ServerAdmin
`channel topic` |  `<channelid>` `[-]` | Sets the topic of a channel, found by its id. | ServerAdmin
`channel name` |  `<channelid>` `<name>` | Sets the name of a channel, found by its id. | ServerAdmin
`role list` |  | Lists all the roles the server has | ServerAdmin
`role add` |  `[-]` | Adds a role with the given name if it doesn't exist. | ServerAdmin
`role rem` |  `<roleid>` | Removes a role with the given id if it exists. | ServerAdmin
`role edit perm` |  `<roleid>` `<permission>` `<value>` | Edits permissions for a given role, found by id, if it exists. | ServerAdmin
`role edit color` |  `<roleid>` `<hex>` | Edits the color (RRGGBB) for a given role, found by id, if it exists.  | ServerAdmin
`role listperm` |  `<roleid>` | Lists the permissions for a given roleid | ServerAdmin
`ued list` |  | Lists users in server and their UIDs | ServerModerator
`ued mute` |  `<usermention>` `<val>` | Mutes(true)/unmutes(false) the userid | ServerModerator
`ued deaf` |  `<usermention>` `<val>` | Deafens(true)/Undeafens(false) @user | ServerModerator
`ued move` |  `<usermention>` `[-]` | Moves a @user to a given voice channel | ServerModerator
`ued role add` |  `<usermention>` `<roleid>` | Adds a role, found by id, to @user if they dont have it. | ServerModerator
`ued role list` |  `<usermention>` | Returns a list of roles a @user has. | ServerModerator
`ued role rem` |  `<usermention>` `<roleid>` | Removes a roleid from a @user if they have it. | ServerModerator
`ued kick` |  `<userMention>` | Kicks a @user. | ServerModerator
`ued ban` |  `<userMention>` `[pruneDays]` | Bans an @user. Also allows for message pruning for a given amount of days. | ServerModerator

#### Audio
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`stream goto` |  `<time>` | Skips to the given point in the track. | User
`stream stop` |  | Stops playback of the playlist. | User
`stream forcestop` |  | Forcefully stops playback of the playlist, track and leaves the voice channel. | User
`stream next` |  | Skips the current track and plays the next track in the playlist. | User
`stream prev` |  | Skips the current track and plays the previus track in the playlist. | User
`stream current` |  | Displays information about the currently played track. | User
`stream pause` |  | Pauses/unpauses playback of the current track. | User
`stream start` *Aliases*: `stream play`  |  `[-]` | Starts the playback of the playlist. | User
`stream add` |  `[-]` | Adds a track to the music playlist. | User
`stream setpos` *Aliases*: `stream set`  |  `<index>` | Sets the position of the current played track index to a given number. | User
`stream remove` *Aliases*: `stream rem`  |  `<index>` | Removes a track at the given position from the playlist. | User
`stream list` |  | List the songs in the current playlist. | User
`stream clear` |  | Stops music and clears the playlist. | User
`stream channel` |  `[-]` | Sets the channel in which the audio will be played in. Use .c to set it to your current channel. | User

#### QoL
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`remind list` |  | Lists the reminders you have set. | User
`remind` |  `<timespan>` `[-]` | Reminds you about something after the given time span has passed. | User
`google` |  `[-]` | Lmgtfy | User
`quote` |  | Prints a quote out from your servers' quote list. | User
`addquote` |  `[-]` | Adds a quote to your servers' quote list. | User
`coin` |  | Flips a coin. | User
`color set` |  `<hex>` | Sets your username to a hex color. Format: RRGGBB | User
`color clear` |  | Removes your username color, returning it to default. | User
`color clean` |  | Removes unused color roles. Gets automatically called whenever a color is set. | User

#### Test
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`test callback` |  `[-]` |  | BotOwner

#### Information
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`whois` |  `<username>` | Displays information about the given user. | User
`whoami` |  | Displays information about the callers user. | User
`chatinfo` |  | Displays information about the current chat channel. | User
`info` |  | Displays information about the bot. | User
`contact` |  | Contact @SSStormy | User

#### Modules
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`module channel enable` |  `[-]` | Enables a module on the current channel. | ServerAdmin
`module channel disable` |  `[-]` | Disable a module on the current channel. | ServerAdmin
`module server enable` |  `[-]` | Enables a module on the current server. | ServerAdmin
`module server disable` |  `[-]` | Disables a module for the current server. | ServerAdmin
`module list` |  | Lists all available modules. | ServerAdmin

#### Execute
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`exec` |  `[-]` | Compiles and runs a C# script. | BotOwner

#### Terraria Relay
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`terraria info` |  | Shows the info for the terraria server connected to this channel. | User
`terraria disconnect` |  | Disconnects from the terraria server connected to this channel. | User
`terraria world` |  | Displays information about the servers world. | User
`terraria travmerch` |  | Displays what the travelling merchant has in stock if they are currently present in the world. | User
`terraria connect` |  `<ip>` `<port>` `[password]` | Connects this channel to a terraria server. | User

#### Twitch Relay
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`twitch connect` |  `<channel>` | Connects this channel to a given twitch channel, relaying the messages between them. | ChannelModerator
`twitch disconnect` |  `<channel>` | Disconnects this channel from the given twitch channel. | ChannelModerator
`twitch list` |  | Lists all the twitch channels this discord channel is connected to. | User

#### Announcements
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`announce disable` |  | Disables bot owner announcements on this server. | ServerModerator
`announce enable` |  | Enabled but owner announcements on this server. | ServerModerator
`announce channel` |  `[-]` | Sets the default channel of any announcements from the bot's owner. | ServerModerator
`announce current` |  | Returns the current announcement channel. | ServerModerator
`announce message` |  `[-]` |  | ServerModerator
`autorole create` |  `[-]` | Enables the bot to add a given role to newly joined users. | ServerAdmin
`autorole destroy` |  | Destoys the auto role assigner for this server. | ServerAdmin
`autorole role` |  `[-]` | Changes the role of the auto role assigner for this server. | ServerAdmin
`newuser syntax` |  | Syntax rules for newuser commands. | ServerModerator
`newuser join message` |  `[-]` | Sets the join message for this current server. | ServerModerator
`newuser join channel` |  `[-]` | Sets the callback channel for this servers join announcements. | ServerModerator
`newuser join destroy` |  | Stops announcing when new users have joined this server. | ServerModerator
`newuser join enable` |  | Enables announcing for when a new user joins this server. | ServerModerator
`newuser leave message` |  `[-]` | Sets the leave message for this current server. | ServerModerator
`newuser leave channel` |  `[-]` | Sets the callback channel for this servers leave announcements. | ServerModerator
`newuser leave destroy` |  | Stops announcing when users have left joined this server. | ServerModerator
`newuser leave enable` |  | Enables announcing for when a user leaves this server. | ServerModerator

#### Vermintide
Commands | Parameters | Description | Default Permissions
--- | --- | --- | ---
`verm roulette` |  `[difficulty]` | Picks a random mission and a random difficulty. (Can be overriden with an optional param.) | User
