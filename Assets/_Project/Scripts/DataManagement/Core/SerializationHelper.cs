using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace StepUpTableTennis.DataManagement.Core.Utils
{
    public static class SerializationHelper
    {
        private static readonly JsonSerializerSettings Settings = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };

        public static string Serialize<T>(T data)
        {
            try
            {
                return JsonConvert.SerializeObject(data, Settings);
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to serialize {typeof(T).Name}: {e.Message}");
                throw;
            }
        }

        public static T Deserialize<T>(string json)
        {
            try
            {
                var result = JsonConvert.DeserializeObject<T>(json, Settings);
                if (result == null)
                    throw new InvalidOperationException($"Deserialization of {typeof(T).Name} resulted in null");
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to deserialize {typeof(T).Name}: {e.Message}");
                throw;
            }
        }

        public static async Task SaveToFileAsync<T>(T data, string filePath)
        {
            var json = Serialize(data);
            await File.WriteAllTextAsync(filePath, json, Encoding.UTF8);
        }

        public static async Task<T> LoadFromFileAsync<T>(string filePath)
        {
            var json = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
            return Deserialize<T>(json);
        }
    }
}