using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class SpecialEffectItem
	{
		[JsonPropertyName("ApiKind")]
		public int ApiKind { get; set; }

		[JsonPropertyName("Firepower")]
		public int Firepower { get; set; }

		[JsonPropertyName("Torpedo")]
		public int Torpedo { get; set; }

		[JsonPropertyName("Armor")]
		public int Armor { get; set; }

		[JsonPropertyName("Evasion")]
		public int Evasion { get; set; }
	}
}
