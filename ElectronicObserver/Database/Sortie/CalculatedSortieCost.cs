#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class CalculatedSortieCost
	{
		[JsonPropertyName("SortieFleetSupplyCost")]
		public SortieCostModel? SortieFleetSupplyCost { get; set; }

		[JsonPropertyName("SortieFleetRepairCost")]
		public SortieCostModel? SortieFleetRepairCost { get; set; }

		[JsonPropertyName("SortieFleetJetCost2")]
		public SortieCostModel? SortieFleetJetCost { get; set; }

		[JsonPropertyName("NodeSupportSupplyCost")]
		public SortieCostModel? NodeSupportSupplyCost { get; set; }

		[JsonPropertyName("BossSupportSupplyCost")]
		public SortieCostModel? BossSupportSupplyCost { get; set; }

		[JsonPropertyName("TotalAirBaseSortieCost")]
		public SortieCostModel? TotalAirBaseSortieCost { get; set; }

		[JsonPropertyName("TotalAirBaseSupplyCost")]
		public SortieCostModel? TotalAirBaseSupplyCost { get; set; }

		[JsonPropertyName("AirBaseJetCost2")]
		public SortieCostModel? AirBaseJetCost { get; set; }

		[JsonPropertyName("DamageStateCounts")]
		public Dictionary<string, int>? DamageStateCounts { get; set; }

		[JsonPropertyName("ConsumedItems")]
		public List<ConsumableItemData>? ConsumedItems { get; set; }
	}
}
