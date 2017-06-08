using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace CBT.Core.Internal
{
    internal class RestorePackage
    {
        public string Id { get; set; }
        public string Version { get; set; }
    }
    internal class PackageRestoreData
    {
        public string RestoreOutputAbsolutePath { get; set; }
        public IList<RestorePackage> PackageImportOrder { get; set; }
    }

    //class PackageRestoreDataConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(PackageRestoreData));
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
    //        JObject jo = JObject.Load(reader);
    //        return new PackageRestoreData
    //        {
    //            RestoreOutputAbsolutePath = (string)jo["RestoreOutputAbsolutePath"],
    //            PackageImportOrder = jo["PackageImportOrder"].Select(i => new PackageIdentity((string)i.SelectToken("Item1"), new NuGetVersion((string)i.SelectToken("Item2")))).ToList(),
    //            //PackageImportOrder = jo["PackageImportOrder"].Select(i => new PackageIdentity((string)i.SelectToken("Id"), GetNuGetVersion(i))).ToList(),
    //        };
    //    }

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        PackageRestoreData packageRestoreData = (PackageRestoreData)value;
    //        List<Tuple<string,string>> packageList = new List<Tuple<string, string>>();
    //        foreach (var pkg in packageRestoreData.PackageImportOrder)
    //        {
    //            packageList.Add(new Tuple<string, string>(pkg.Id, pkg.Version.ToString()));
    //        }
    //        JToken t = JToken.FromObject(value);
    //        t["PackageImportOrder"] = JsonConvert.SerializeObject(packageList);

    //        if (t.Type != JTokenType.Object)
    //        {
    //            t.WriteTo(writer);
    //        }
    //        else
    //        {
    //            JObject o = (JObject)t;
    //            o.WriteTo(writer);
    //        };
    //    }

    //    //private NuGetVersion GetNuGetVersion(JToken json)
    //    //{
    //    //    var foo = json.SelectToken("Version").ToString();
    //    //    //return new NuGetVersion("1.0.0");
    //    //    return JsonConvert.DeserializeObject<NuGetVersion>(json.SelectToken("Version").ToString(), new NuGetVersionConverter());
    //    //}
    //}

    //class NuGetVersionConverter : JsonConverter
    //{
    //    public override bool CanConvert(Type objectType)
    //    {
    //        return (objectType == typeof(NuGetVersion));
    //    }

    //    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    //    {
            
    //        JObject jo = JObject.Load(reader);
            
    //        if ((bool) jo["IsPrerelease"])
    //        {
                
    //        }
    //        //string nugetVersion = $"{}";
    //        return new NuGetVersion("0.0.0");
    //    }

    //    public override bool CanWrite => false;

    //    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
