using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SpotifyTool.Config
{
    public static class Serialization
    {
        private readonly static Dictionary<string, object> ReadonlyCache = new Dictionary<string, object>();

        public static Task SerializeJson(object obj, string fileName, bool isMutable = true)
        {
            if (!isMutable)
            {
                ReadonlyCache[fileName] = obj;
            }
            string json = JsonConvert.SerializeObject(obj);
            return File.WriteAllTextAsync(fileName, json);
        }
        public async static Task<T> DeserializeJson<T>(string fileName, bool isMutable = true)
        {
            object outValue;
            if (!isMutable && ReadonlyCache.TryGetValue(fileName, out outValue))
            {
                return (T)outValue;
            }
            string json = await File.ReadAllTextAsync(fileName);
            var outValueCasted = JsonConvert.DeserializeObject<T>(json);
            if (!isMutable)
            {
                ReadonlyCache[fileName] = outValueCasted;
            }
            return outValueCasted;
        }
    }
}
