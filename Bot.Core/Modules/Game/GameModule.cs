using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.Commands.Permissions.Levels;
using Discord.Modules;
using Stormbot.Bot.Core.Services;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules.Game
{
    public class GameModule : IDataModule
    {
        [DataLoad, DataSave] private GameSessionManager _sesh;
        private GameSessionManager GameSesh => _sesh ?? (_sesh = new GameSessionManager());

        private DiscordClient _client;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;

            manager.CreateCommands("game", group =>
            {
                group.CreateCommand("join")
                    // check if the player already exists.
                    .AddCheck((cmd, usr, chl) => !GameSesh.PlayerExists(usr.Id))
                    .Do(async e =>
                    {
                        await GameSesh.Join(e.User);
                    });
            });

            manager.CreateCommands("char", group =>
            {
                group.AddCheck((cmd, usr, chl) =>
                {
                    GamePlayer player = GameSesh.GetPlayer(usr);
                    if (player == null) return false;
                    return !player.IsInCharacterCreation;
                }); // only allow these commnads for users who have a player and the player did the character creation.

                group.CreateCommand("info")
                    .Description("Displays information about your character.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.Channel.SendMessage(player.ToString());
                    });

                group.CreateCommand("inv add")
                    .Parameter("id")
                    .Parameter("val")
                    .Description("Adds an item id with a certain amount to your inventory.")
                    .MinPermissions((int) PermissionLevel.BotOwner)
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        Item add = new Item(uint.Parse(e.GetArg("id")), int.Parse(e.GetArg("val")));

                        if (!player.Inventory.AddItem(add))
                        {
                            await e.Channel.SendMessage("Couldn't fully add all the requested items to your inventory.");
                            return;
                        }
                        await e.Channel.SendMessage($"Added {add.ItemDef.Name} x{add.Amount} to your inventory.");
                    });

                group.CreateCommand("inv show")
                    .Description("Shows you your inventory.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.User.SendPrivate($"{Format.Bold("Your inventory:")}\r\n`{player.Inventory}`");
                    });

                group.CreateCommand("loc nearby")
                    .Description("Lists locations that you enter from your current location.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.Channel.SendMessage($"**Nearby locations:**\r\n{player.Location.ToStringNearby()}");
                    });

                group.CreateCommand("loc interact")
                    .Alias("loc int")
                    .Description("Interact with a object in your current location")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {;
                        string objName = e.GetArg("name").ToLower();
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        LocObject obj = player.Location.Objects.FirstOrDefault(o => o.Name.ToLower() == objName);

                        if (obj == null)
                        {
                            await e.Channel.SendMessage($"Could not find object `{objName}` in current location (`{player.Location.Name}`)");
                            return;
                        }
                        await e.Channel.SendMessage($"You interact with `{objName}`");
                        await obj.OnInteract(player);
                    });

                group.CreateCommand("loc enter")
                    .Description("Enters a nearby location, found by it's name.")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        string name = e.GetArg("name").ToLower();
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        Location loc = player.Location.NearbyLocations.All.FirstOrDefault(l => l.Name.ToLower() == name);

                        if (loc == null)
                        {
                            await e.Channel.SendMessage($"Location {name} wasn't found or isn't nearby.");
                            return;
                        }

                        if (!loc.Enter(player))
                        {
                            await e.Channel.SendMessage($"Couldn't enter nearby location {name}.");
                            return;
                        }

                        await e.Channel.SendMessage($"**You have entered {player.Location.Name}.**\r\n```{player.Location}```");
                    });

                group.CreateCommand("loc")
                    .Description("Displays information about your current location.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.Channel.SendMessage($"**{player.Location}:**\r\n{player.Location.ToString()}");
                    });

                group.CreateCommand("loc enterany")
                    .MinPermissions((int) PermissionLevel.BotOwner)
                    .Description("Tries to enter your users location to the given location id.")
                    .Parameter("id")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        Location loc = Location.Get(uint.Parse(e.GetArg("id")));
                        if (loc.Enter(player))
                        {
                            await e.Channel.SendMessage($"You have entered location {player.Location.Name}");
                            return;
                        }
                        await e.Channel.SendMessage($"Couldn't enter locaton {loc.Name}");
                    });

                group.CreateCommand("loc set")
                    .MinPermissions((int) PermissionLevel.BotOwner)
                    .Description("Sets your users location to the given location id. Not recommended.")
                    .Parameter("id")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        Location loc = Location.Get(uint.Parse(e.GetArg("id")));

                        player.Location = loc;
                        await e.Channel.SendMessage($"You have entered location {loc.Name}");
                    });
            });

            manager.CreateCommands("cc", group =>
            {
                // only allow if player is in the char create state.
                group.AddCheck((cmd, usr, cnl) =>
                {
                    GamePlayer player = GameSesh.GetPlayer(usr);
                    return player != null && player.IsInCharacterCreation;
                });

                group.CreateCommand("name")
                    .Description("Sets the name of your character.")
                    .Parameter("name", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User.Id);

                        player.CreateState.Name = e.GetArg("name");
                        await e.Channel.SendMessage($"Name set to {player.CreateState.Name}");
                        await CheckForCanFinish(e.User);
                    });

                group.CreateCommand("gender")
                    .Description("Available genders: Male, Female")
                    .Parameter("gender")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);

                        player.CreateState.Gender = Utils.EnumParse<GamePlayer.GenderType>(e.GetArg("gender"));
                        await e.Channel.SendMessage($"Gender set to {player.CreateState.Gender}");
                        await CheckForCanFinish(e.User);
                    });

                group.CreateCommand("info")
                    .Description("Displays the current information you have set for your character.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.Channel.SendMessage(player.CreateState.ToString());
                    });

                group.CreateCommand("finish")
                    .AddCheck(
                        (cmd, usr, chnl) =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(usr);
                            return player != null && player.IsInCharacterCreation &&
                                   player.CreateState.CanFinishCreation;
                        })
                    // only allow this finish command for players who are in the char creation state exist and have the can finish creation state.
                    .Description("Finishes character creation.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);

                        player.FinishPlayerCreate();
                        await
                            e.Channel.SendMessage(
                                $"Player creation done.\r\nName {Format.Code(player.Name)} Gender: {Format.Code(player.Gender.ToString())}");
                    });
            });
        }

        private async Task CheckForCanFinish(User user)
        {
            if (user == null) return;

            GamePlayer player = GameSesh.GetPlayer(user.Id);

            if (player?.CreateState == null) return;
            if (!player.CreateState.CanFinishCreation) return;

            await
                user.SendPrivate(
                    $"Player data seems good.\r\n{player.CreateState}\r\n Use !cc finish to finish creating your character.");
        }

        public void OnDataLoad()
        {
            // find the user associated with the gameplayer.
            foreach (GamePlayer player in GameSesh.Players.Where(player => player.User == null))
                player.User = _client.GetUser(player.UserId);
        }
    }
}