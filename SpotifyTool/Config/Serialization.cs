using ProtoBuf;
using ProtoBuf.Meta;
using SpotifyAPI.Web;
using SpotifyTool.SpotifyObjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace SpotifyTool.Config
{
    public static class Serialization
    {
        private readonly static Dictionary<string, object> Cache = new Dictionary<string, object>();

        public static void Init()
        {
            //RuntimeTypeModel.Default.InferTagFromNameDefault = true;
            //RuntimeTypeModel.Default.AutoAddMissingTypes = true;
            //RuntimeTypeModel.Default.AutoAddProtoContractTypesOnly = false;
            //RuntimeTypeModel.Default.AutoCompile = true;
            ////var t = System.Reflection.Assembly.GetAssembly(typeof(FullTrack)).DefinedTypes.ToList();
            //var spotifyTypes = System.Reflection.Assembly.GetAssembly(typeof(FullTrack)).DefinedTypes
            //    .Where(dt => dt.FullName.StartsWith("SpotifyAPI.Web") && dt.UnderlyingSystemType != null).ToList();
            //var ownTypes = System.Reflection.Assembly.GetAssembly(typeof(Serialization)).DefinedTypes
            //    .Where(dt => dt.FullName.StartsWith("SpotifyTool") && dt.UnderlyingSystemType != null).ToList();
            //var allTypes = spotifyTypes.Concat(ownTypes).ToList();
            //foreach (var typeInfo in allTypes)
            //{
            //    RuntimeTypeModel.Default.Add(typeInfo.UnderlyingSystemType, true);
            //}
        }

        public static void SerializeBinary(object obj, string fileName)
        {
            Cache[fileName] = obj;
            //using (var s = File.Open(fileName, FileMode.Create))
            //{
            //    //BinaryFormatter b = new BinaryFormatter();
            //    RuntimeTypeModel.Default.Serialize(s, obj);
            //}
        }
        public static T DeserializeBinary<T>(string fileName)
        {
            return (T)Cache[fileName];
            //using (var s = File.Open(fileName, FileMode.Open))
            //{
            //    //BinaryFormatter b = new BinaryFormatter();
            //    return (T)RuntimeTypeModel.Default.Deserialize<T>(s);
            //}
        }
    }
}
