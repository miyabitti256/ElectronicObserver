using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class ConsumableItemData
	{
		[JsonPropertyName("Id")]
		public int Id { get; set; }

		[JsonIgnore]
		public int ItemId { get => Id; set => Id = value; }

		[JsonPropertyName("Count")]
		public int Count { get; set; }
	}
}
