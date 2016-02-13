using Discord;
using Discord.Commands;
using Discord.Modules;

namespace Stormbot.Bot.Core.DynPerm
{
    public class DynamicPermissionModule : IModule
    {
        private DiscordClient _client;
        private DynamicPermissionService _dynPerms;

        public void Install(ModuleManager manager)
        {
            _client = manager.Client;
            _dynPerms = _client.DynPerms();

            manager.CreateDynCommands("dynperm", PermissionLevel.ServerAdmin, group =>
            {
                group.CreateCommand("set")
                    .Description(
                        "Sets the dynamic permissions for this server. Use the dynperm help command for more info.")
                    .Parameter("perms", ParameterType.Unparsed)
                    .Do(async e =>
                    {
#if DEBUG_DEV
                        DynamicPerms perms = _dynPerms.TryAddOrUpdate(e.Server.Id, e.GetArg("perms"));
                        if (perms == null)
                        {
                            await e.Channel.SendMessage("Failed parsing Dynamic Permissions. Make sure your JSON is valid.");
                            return;
                        }
                        await e.Channel.SendMessage($"Parsed Dynamic Permissions:\r\n```" +
                                                    $"- Role Rules: {perms.RolePerms.Count}" +
                                                    $"- User Rules: {perms.UserPerms.Count}```");
#else
                        await e.Channel.SendMessage("This feature is currently being stress tested and is unavailable.");
#endif
                    });

                group.CreateCommand("help")
                    .Description("help")
                    .Do(async e =>
                    {
                        await
                            e.Channel.SendMessage(
                                "\r\n**Dynamic Permissions** allow users to choose what permissions they want to give to users and roles.\r\n" +
                                "The rules are encoded in a JSON format, which you can easily validate.(http://pro.jsonlint.com/)\r\n" +
                                "The base format: https://ghostbin.com/paste/rqqqm\r\n" +
                                "You can get user ids from `\"ued list\"` and role ids from `\"role list\"` commands.\r\n" +
                                "\"Roles\" and \"Users\" are both arrays, meaning you can put multiple elements inside of them.\r\n" +
                                "Here are some example rules: https://ghostbin.com/paste/ye267\r\n" +
                                $"Since I am completely horrible at explaining concepts, please refer to these resources if you are still confused:\r\n" +
                                $"- https://en.wikipedia.org/wiki/JSON#Data_types.2C_syntax_and_example\r\n" +
                                $"- http://www.w3schools.com/json/json_syntax.asp");
                    });
            });
        }
    }
}
