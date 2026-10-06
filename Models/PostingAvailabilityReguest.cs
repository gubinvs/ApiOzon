using System.Text.Json.Serialization;
using ApiOzon.Core;

namespace ApiOzon.Models
{
    public class PostingAvailabilityReguest
    {
        [JsonPropertyName("request_id")]
        public int RequestId {get; set;}

        [JsonPropertyName("cutoff_at")]
        public string CutoffAt {get; set;} = string.Empty;

        [JsonPropertyName("declared_value")]
        public DeclaredValue? DeclaredValue {get; set;}

        [JsonPropertyName("dimensions")]
        public Dimensions? Dimensions {get; set;}
    }
}