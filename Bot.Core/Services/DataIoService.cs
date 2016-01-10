// Copyright (c) 2015 Justas Dabrila (justasdabrila@gmail.com)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Discord;
using Discord.Modules;
using Newtonsoft.Json;
using Stormbot.Helpers;

namespace Stormbot.Bot.Core.Services
{
    public class DataIoService : IService
    {
        private class SerializationData
        {
            public IModule Module { get; }
            public FieldInfo Field { get; }
            public string SaveDir { get; }

            public SerializationData(IModule module, FieldInfo field, string saveDir)
            {
                Module = module;
                Field = field;
                SaveDir = saveDir;
            }
        }

        private DiscordClient _client;

        private const string DataDir = Constants.DataFolderDir + @"data\";

        public void Install(DiscordClient client)
        {
            _client = client;
        }

        private IEnumerable<SerializationData> GetFields<T>() where T : Attribute
        {
            return from module in _client.Modules().Modules.Select(m => m.Instance)
                let type = module.GetType()
                where type.GetCustomAttribute<DataModuleAttribute>() != null
                let fields = type.GetRuntimeFields().Where(f => f.GetCustomAttribute<T>() != null)
                from serializeField in fields
                select new SerializationData(
                    module,
                    serializeField,
                    $"{DataDir}{type.Name}_{serializeField.Name}.json");
        }

        public void Load()
        {
            foreach (SerializationData data in GetFields<DataLoadAttribute>())
            {
                try
                {
                    if (!File.Exists(data.SaveDir)) continue;

                    Logger.FormattedWrite(GetType().Name, $"Loading field {data.Field.Name}", ConsoleColor.DarkBlue);
                    string jsondata = File.ReadAllText(data.SaveDir);

                    data.Field.SetValue(
                        data.Module,
                        JsonConvert.DeserializeObject(jsondata, data.Field.FieldType));
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite(
                        GetType().Name,
                        $"Failed loading data for field {data.Field.Name}. Exception: {ex}",
                        ConsoleColor.Red);
                }
            }
        }

        public void Save()
        {
            foreach (SerializationData data in GetFields<DataLoadAttribute>())
            {
                try
                {
                    Logger.FormattedWrite(GetType().Name, $"Saving field {data.Field.Name}", ConsoleColor.DarkBlue);
                    File.WriteAllText(data.SaveDir,
                        JsonConvert.SerializeObject(data.Field.GetValue(data.Module)));
                }
                catch (Exception ex)
                {
                    Logger.FormattedWrite(GetType().Name,
                        $"Failed saving data for field {data.Field.Name}. Exception: {ex}",
                        ConsoleColor.Red);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DataModuleAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public abstract class BaseDataAttribute : Attribute
    {
    }

    public class DataLoadAttribute : BaseDataAttribute
    {
    }

    public class DataSaveAttribute : BaseDataAttribute
    {
    }
}