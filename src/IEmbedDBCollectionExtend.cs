using System.Text.Json;

namespace EmbedDB;

internal interface IEmbedDBCollectionExtend
{
    string Name { get; }
    void SerializeData(Utf8JsonWriter writer);
    void DeserializeData(JsonElement element);
}
