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

        public void Install(ModuleManager manager)
        {
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
                group.AddCheck((cmd, usr, chl) => GameSesh.PlayerExists(usr.Id)); // check if user exists

                group.CreateCommand("info")
                    .Description("Displays information about your character.")
                    .Do(async e =>
                    {
                        GamePlayer player = GameSesh.GetPlayer(e.User);

                        StringBuilder builder =
                            new StringBuilder($"{Format.Bold($"{e.User.Name}'s character info:")}\r\n");

                        builder.AppendLine(player.CreateState?.ToString() ?? player.ToString());

                        await e.Channel.SendMessage(builder.ToString());
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
            });

            manager.CreateCommands("cc", group =>
            {
                // if player or the create state is null dont allow execution.
                group.AddCheck((cmd, usr, cnl) =>
                {
                    GamePlayer player = GameSesh.GetPlayer(usr);
                    return player?.CreateState != null;
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

                group.CreateCommand("finish")
                    .AddCheck(
                        (cmd, usr, chnl) =>
                            //if player or create state is null or we cant finish creation - dont execute.
                        {
                            GamePlayer player = GameSesh.GetPlayer(usr);
                            return player?.CreateState != null && player.CreateState.CanFinishCreation;
                        })
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
            // ignored
        }
    }
}