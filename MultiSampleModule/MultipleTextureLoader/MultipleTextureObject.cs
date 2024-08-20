using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using UnityEngine;

namespace WIGU.Modules.Texture
{
    public class MultipleTextureObjectConverter : JsonConverter<MultipleTextureObject>
    {
        public override MultipleTextureObject ReadJson(JsonReader reader, Type objectType, MultipleTextureObject existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jobject = JObject.Load(reader);
            return new MultipleTextureObject() { Path = (string)jobject["path"] };
        }

        public override void WriteJson(JsonWriter writer, MultipleTextureObject value, JsonSerializer serializer)
        {
            new JObject
            {
                {
                    "path",
                    value.Path
                }
            }.WriteTo(writer, Array.Empty<JsonConverter>());
        }
    }

    public class MultipleTextureObject : JsonObject
    {
        public string Path { get; set; }
    }
}
