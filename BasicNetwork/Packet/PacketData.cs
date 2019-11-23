using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BasicNetwork.Packet
{
    public class BasicData : Dictionary<string, object>
    {
        public static BasicData FromJson(string json)
            => JsonConvert.DeserializeObject<BasicData>(json);

        public T Get<T>(string Key, T def)
        {
            try
            {
                if (ContainsKey(Key))
                {
                    switch (this[Key])
                    {
                        case JArray ja: return ja.ToObject<T>();
                        case JObject jo: return jo.ToObject<T>();
                        default: return (T)this[Key];
                    }
                }

                return def;
            }
            catch
            {
                return def;
            }
        }

        public BasicData Set(string key, object data)
        {
            this[key] = data;
            return this;
        }
    }
}
