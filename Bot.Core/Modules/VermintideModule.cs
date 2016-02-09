using System.Collections.Generic;
using Discord.Commands;
using Discord.Modules;
using StrmyCore;

namespace Stormbot.Bot.Core.Modules
{
    public class VermintideModule : IModule
    {
        private readonly List<string> _difficulties = new List<string>
        {
            "Easy",
            "Normal",
            "Hard",
            "Nightmare",
            "Cataclysm"
        };

        private readonly List<string> _missions = new List<string>
        {
            "Man the Ramparts",
            "The Horn of Magnus",
            "Smuggler's Run",
            "The White Rat",
            "Engines of War",
            "Black Powder",
            "Waterfront",
            "The Enemy Below",
            "Garden of Morr",
            "Well Watch",
            "Supply and Demand",
            "The Wizard's Tower",
            "Wheat and Chaff"
        };

        public void Install(ModuleManager manager)
        {
            manager.CreateCommands("verm", group =>
            {
                group.CreateCommand("roulette")
                    .Description(
                        "Picks a random mission and a random difficulty. (Can be overriden with an optional param.)")
                    .Parameter("difficulty", ParameterType.Optional)
                    .Do(async e =>
                    {
                        string diff = e.GetArg("difficulty");

                        if (string.IsNullOrEmpty(diff))
                            diff = _difficulties.PickRandom();

                        await e.Channel.SendMessage($"{_missions.PickRandom()} on {diff}");
                    });
            });
        }
    }
}
