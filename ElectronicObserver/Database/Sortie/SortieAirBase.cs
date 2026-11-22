using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class SortieAirBase
	{
		[JsonPropertyName("Name")]
		public string Name { get; set; } = "";

		[JsonPropertyName("MapAreaId")]
		public int MapAreaId { get; set; }

		[JsonPropertyName("AirCorpsId")]
		public int AirCorpsId { get; set; }

		[JsonIgnore]
		public int AirBaseId { get => AirCorpsId; set => AirCorpsId = value; }

		[JsonPropertyName("ActionKind")]
		public int ActionKind { get; set; }

		[JsonPropertyName("BaseDistance")]
		public int BaseDistance { get; set; }

		[JsonPropertyName("BonusDistance")]
		public int BonusDistance { get; set; }

		[JsonPropertyName("Squadrons")]
		public List<SortieAirBaseSquadron> Squadrons { get; set; } = new List<SortieAirBaseSquadron>();
	}
}
