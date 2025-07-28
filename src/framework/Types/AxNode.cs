using Newtonsoft.Json;

namespace framework.Types;

public class AxNode
{
    [JsonProperty("nodeId")]
    public string NodeId { get; set; }

    [JsonProperty("role")]
    public AxValue Role { get; set; }

    [JsonProperty("name")]
    public AxValue Name { get; set; }

    [JsonProperty("value")]
    public AxValue Value { get; set; } // Optional: For inputs, sliders, etc.

    [JsonProperty("description")]
    public AxValue Description { get; set; } // Optional: aria-describedby, tooltips

    [JsonProperty("ignored")]
    public bool? Ignored { get; set; } // Optional: true if node is excluded

    [JsonProperty("childIds")]
    public List<string> ChildIds { get; set; } = new List<string>();

    // Resolved children after building tree
    [JsonIgnore]
    public List<AxNode> Children { get; set; } = new List<AxNode>();
}

public class AxValue
{
    [JsonProperty("value")]
    public string Value { get; set; }
}