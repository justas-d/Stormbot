using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Discord;
using Discord.Modules;
using Newtonsoft.Json;
using StrmyCore;

namespace Stormbot.Bot.Core.Services
{
    internal interface IDataObject
    {
        void OnDataLoad();
    }

    public class DataIoService : IService
    {
        private class SerializationData
        {
            public IDataObject Object { get; }
            public FieldInfo Field { get; }
            public string SaveDir { get; }

            public SerializationData(IDataObject obj, FieldInfo field, string saveDir)
            {
                Object = obj;
                Field = field;
                SaveDir = saveDir;
            }
        }

        private const string DataDir = Constants.DataFolderDir + @"data\";
        private DiscordClient _client;

        void IService.Install(DiscordClient client)
        {
            _client = client;
        }

        private IEnumerable<SerializationData> GetAllFields<T>() where T : Attribute
        {
            foreach (IService service in _client.Services)
            {
                if (service is IDataObject)
                {
                    foreach (SerializationData elem in GetFields<T>(service as IDataObject))
                        yield return elem;
                }
            }
            foreach (ModuleManager moduleManager in _client.GetService<ModuleService>().Modules)
            {
                IDataObject module = moduleManager.Instance as IDataObject;
                if (module != null)
                {
                    foreach (SerializationData elem in GetFields<T>(module))
                        yield return elem;
                }
            }
        }

        private IEnumerable<SerializationData> GetFields<T>(IDataObject data) where T : Attribute
        {
            Type type = data.GetType();
            foreach (FieldInfo field in type.GetRuntimeFields())
            {
                if (field.GetCustomAttribute<T>() != null)
                    yield return new SerializationData(data, field, $"{DataDir}{type.Name}_{field.Name}.json");
            }
        }

        public void Load()
        {
            foreach (SerializationData data in GetAllFields<DataLoadAttribute>())
            {
                try
                {
                    if (!File.Exists(data.SaveDir))
                    {
                        data.Object.OnDataLoad();
                        continue;
                    }

                    Logger.FormattedWrite(GetType().Name, $"Loading field {data.Field.Name}", ConsoleColor.DarkBlue);
                    string jsondata = File.ReadAllText(data.SaveDir);

                    data.Field.SetValue(
                        data.Object,
                        JsonConvert.DeserializeObject(jsondata, data.Field.FieldType));

                    data.Object.OnDataLoad();
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
            foreach (SerializationData data in GetAllFields<DataLoadAttribute>())
            {
                try
                {
                    Logger.FormattedWrite(GetType().Name, $"Saving field {data.Field.Name}", ConsoleColor.DarkBlue);
                    File.WriteAllText(data.SaveDir,
                        JsonConvert.SerializeObject(data.Field.GetValue(data.Object)));
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

    [AttributeUsage(AttributeTargets.Field)]
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