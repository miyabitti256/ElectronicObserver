using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class SortieCostModel
	{
		public static SortieCostModel Zero => new SortieCostModel();

		[JsonPropertyName("Fuel")]
		public int Fuel { get; set; }

		[JsonPropertyName("Ammo")]
		public int Ammo { get; set; }

		[JsonPropertyName("Steel")]
		public int Steel { get; set; }

		[JsonPropertyName("Bauxite")]
		public int Bauxite { get; set; }

		public static SortieCostModel operator +(SortieCostModel a, SortieCostModel b) => new SortieCostModel
		{
			Fuel = (a?.Fuel ?? 0) + (b?.Fuel ?? 0),
			Ammo = (a?.Ammo ?? 0) + (b?.Ammo ?? 0),
			Steel = (a?.Steel ?? 0) + (b?.Steel ?? 0),
			Bauxite = (a?.Bauxite ?? 0) + (b?.Bauxite ?? 0),
		};

		public static SortieCostModel operator -(SortieCostModel a, SortieCostModel b) => new SortieCostModel
		{
			Fuel = (a?.Fuel ?? 0) - (b?.Fuel ?? 0),
			Ammo = (a?.Ammo ?? 0) - (b?.Ammo ?? 0),
			Steel = (a?.Steel ?? 0) - (b?.Steel ?? 0),
			Bauxite = (a?.Bauxite ?? 0) - (b?.Bauxite ?? 0),
		};
	}
}
