using System.Linq;
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

            // commands, which require the caller to have an in game user.
            // bug : http://puu.sh/mzUz3/309cc7df23.png
            // asked volt about this, hopefuly a solution comes soon.
            // if not, I should not chain groups like that, only make groups from the manager object.
            manager.CreateCommands("", ingameG =>
            {
                ingameG.AddCheck((cmd, usr, chl) =>
                {
                    GamePlayer player = GameSesh.GetPlayer(usr);
                    if (player == null) return false;
                    return !player.IsInCharacterCreation;
                }); // only allow these commnads for users who have a player and the player did the character creation.

                ingameG.CreateGroup("char", charG =>
                {
                    charG.CreateCommand("info")
                        .Description("Displays information about your character.")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            await e.Channel.SafeSendMessage(player.ToString());
                        });
                });

                ingameG.CreateGroup("inv", invG =>
                {
                    invG.CreateCommand("add")
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
                                await
                                    e.Channel.SafeSendMessage(
                                        "Couldn't fully add all the requested items to your inventory.");
                                return;
                            }
                            await e.Channel.SafeSendMessage($"Added {add.ItemDef.Name} x{add.Amount} to your inventory.");
                        });

                    invG.CreateCommand("show")
                        .Description("Shows you your inventory.")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            await e.User.SendPrivate($"{Format.Bold("Your inventory:")}\r\n`{player.Inventory}`");
                        });
                });

                ingameG.CreateGroup("bank", bankG =>
                {
                    bankG.AddCheck((cmd, usr, chnl) =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(usr);
                        return player.Location.Objects.Contains(LocObject.Bank);
                    }); // only allow these commands to players who are in a location that contains a bank locobject.

                    bankG.CreateCommand("show")
                        .Parameter("page", ParameterType.Optional)
                        .Description(
                            "Displays the items in the given page from your bank. If not page argument was passed, instead we will show how many pages are available.")
                        .Do(async e =>
                        {
                            // check for page argunement;
                            int page;
                            GamePlayer player = GameSesh.GetPlayer(e.User);

                            if (int.TryParse(e.GetArg("page"), out page))
                            {
                                page--; // convert to zero based.
                                await
                                    e.Channel.SafeSendMessage(
                                        $"**{e.User}'s bank page {page + 1}:**\r\n`{player.Bank.GetPageData(page)}`");
                            }
                            else
                            {
                                await e.Channel.SafeSendMessage($"Available bank pages: {Bank.Pages}");
                            }
                        });

                    bankG.CreateCommand("take")
                        .AddCheck((cmd, usr, chnl) =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(usr);
                            return player.Inventory.HasSpace;
                        })
                        .Description("Takes an item out of your bank and puts it into your inventory")
                        .Parameter("bank index")
                        .Do(async e =>
                        {
                            int bankIndex = int.Parse(e.GetArg("bank index")) - 1;

                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            Item takeItem = player.Bank.TakeItem(bankIndex);

                            if (takeItem == null)
                            {
                                await e.Channel.SafeSendMessage("You cannot take nothing from your bank!");
                                return;
                            }
                            if (player.Inventory.AddItem(takeItem))
                            {
                                await
                                    e.Channel.SafeSendMessage(
                                        $"You took `{takeItem.ItemDef.Name}` from your bank and put it in your inventory.");
                                return;
                            }
                            await e.Channel.SafeSendMessage($"Failed takinng `{takeItem.ItemDef.Name}` from your bank.");
                        });

                    bankG.CreateCommand("deposit") // todo : optional amount argument
                        .AddCheck((comd, usr, chnl) =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(usr);
                            return player.Bank.HasSpace;
                        })
                        .Parameter("inventory index")
                        .Parameter("bank index", ParameterType.Optional)
                        .Description(
                            "Displays an item at the given index from your inventory to your bank. If an optional bank index paramater is passed, we will try to deposit an item at that given index.")
                        .Do(async e =>
                        {
                            int inventoryIndex = int.Parse(e.GetArg("inventory index")) - 1; // convert to zero based.
                            int bankIndex;

                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            Item depositItem = player.Inventory.TakeItem(inventoryIndex);

                            if (depositItem == null) // check if we are trying to depost an existing item.
                            {
                                await e.Channel.SafeSendMessage("You cannot deposit nothing into your bank!");
                                return;
                            }
                            if (!player.Bank.CanHoldItem(depositItem)) // check if the bank can hold the item.
                            {
                                await
                                    e.Channel.SafeSendMessage(
                                        $"Your bank doesn't have enough space to hold `{depositItem.ItemDef.Name}`");
                                return;
                            }

                            // all looks good, proceed to depositiong.

                            //check for optional index paramater
                            if (int.TryParse(e.GetArg("inventory index"), out bankIndex))
                            {
                                bankIndex--; // convert to zero based.
                                if (player.Bank.AddItemIndex(depositItem, bankIndex))
                                    goto deposit;
                                else
                                    goto failed;
                            }

                            // no set index, add to the first empty spot.
                            if (player.Bank.AddItem(depositItem))
                                goto deposit;
                            else
                                goto failed;

                            //as close to local functions as we can get right now, i suppose.
                            deposit:
                            await
                                e.Channel.SafeSendMessage($"You deposit `{depositItem.ItemDef.Name}` into your bank.");
                            return;

                            failed:
                            await
                                e.Channel.SafeSendMessage(
                                    $"Couldn't fully deposit `{depositItem.ItemDef.Name}` to your bank.");
                            return;
                        });
                });

                ingameG.CreateGroup("loc", locG =>
                {
                    locG.CreateCommand("nearby")
                        .Alias("near")
                        .Description("Lists locations that you enter from your current location.")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            await e.Channel.SafeSendMessage($"**Nearby locations:**\r\n{player.Location.ToStringNearby()}");
                        });

                    locG.CreateCommand("interact")
                        .Alias("use")
                        .Alias("int")
                        .Description("Interact with a object in your current location")
                        .Parameter("name", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            ;
                            string objName = e.GetArg("name").ToLower();
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            LocObject obj = player.Location.Objects.FirstOrDefault(o => o.Name.ToLower() == objName);

                            if (obj == null)
                            {
                                await
                                    e.Channel.SafeSendMessage(
                                        $"Could not find object `{objName}` in current location (`{player.Location.Name}`)");
                                return;
                            }
                            await e.Channel.SafeSendMessage($"You interact with `{objName}`");
                            await obj.OnInteract(player);
                        });

                    locG.CreateCommand("enter")
                        .Alias("goto")
                        .Description("Enters a nearby location, found by it's name.")
                        .Parameter("name", ParameterType.Unparsed)
                        .Do(async e =>
                        {
                            string name = e.GetArg("name").ToLower();
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            Location loc =
                                player.Location.NearbyLocations.All.FirstOrDefault(l => l.Name.ToLower() == name);

                            if (loc == null)
                            {
                                await e.Channel.SafeSendMessage($"Location {name} wasn't found or isn't nearby.");
                                return;
                            }

                            if (!loc.Enter(player))
                            {
                                await e.Channel.SafeSendMessage($"Couldn't enter nearby location {name}.");
                                return;
                            }

                            await
                                e.Channel.SafeSendMessage(
                                    $"**You have entered {player.Location.Name}.**\r\n```{player.Location}```");
                        });

                    locG.CreateCommand("")
                        .Description("Displays information about your current location.")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            await e.Channel.SafeSendMessage($"**{player.Location}:**\r\n{player.Location.ToString()}");
                        });

                    locG.CreateCommand("enterany")
                        .MinPermissions((int) PermissionLevel.BotOwner)
                        .Description("Tries to enter your users location to the given location id.")
                        .Parameter("id")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            Location loc = Location.Get(uint.Parse(e.GetArg("id")));
                            if (loc.Enter(player))
                            {
                                await e.Channel.SafeSendMessage($"You have entered location {player.Location.Name}");
                                return;
                            }
                            await e.Channel.SafeSendMessage($"Couldn't enter locaton {loc.Name}");
                        });

                    locG.CreateCommand("set")
                        .MinPermissions((int) PermissionLevel.BotOwner)
                        .Description("Sets your users location to the given location id. Not recommended.")
                        .Parameter("id")
                        .Do(async e =>
                        {
                            GamePlayer player = GameSesh.GetPlayer(e.User);
                            Location loc = Location.Get(uint.Parse(e.GetArg("id")));

                            player.Location = loc;
                            await e.Channel.SafeSendMessage($"You have entered location {loc.Name}");
                        });
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
                        await e.Channel.SafeSendMessage($"Name set to {player.CreateState.Name}");
                        await CheckForCanFinish(e.User);
                    });

                group.CreateCommand("gender")
                    .Description("Available genders: Male, Female")
                    .Parameter("gender")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);

                        player.CreateState.Gender = Utils.EnumParse<GamePlayer.GenderType>(e.GetArg("gender"));
                        await e.Channel.SafeSendMessage($"Gender set to {player.CreateState.Gender}");
                        await CheckForCanFinish(e.User);
                    });

                group.CreateCommand("info")
                    .Description("Displays the current information you have set for your character.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);
                        await e.Channel.SafeSendMessage(player.CreateState.ToString());
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
                            e.Channel.SafeSendMessage(
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
            foreach (GamePlayer player in GameSesh.Players.Values.Where(player => player.User == null))
                player.User = _client.GetUser(player.UserId);
        }
    }
}