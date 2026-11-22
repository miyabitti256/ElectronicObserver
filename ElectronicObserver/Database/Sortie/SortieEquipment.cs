using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class SortieEquipment
	{
		[JsonPropertyName("Id")]
		public int Id { get; set; }

		[JsonPropertyName("Level")]
		public int Level { get; set; }

		[JsonPropertyName("AircraftLevel")]
		public int AircraftLevel { get; set; }
	}
}
