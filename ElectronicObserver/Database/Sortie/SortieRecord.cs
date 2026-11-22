#nullable enable
using System.Collections.Generic;
using ElectronicObserver.Database.KancolleApi;

namespace ElectronicObserver.Database.Sortie
{
	public class SortieRecord
	{
		public int Id { get; set; }
		public int Version { get; set; } = 1;
		public int World { get; set; }
		public int Map { get; set; }
		public List<ApiFile> ApiFiles { get; set; } = new List<ApiFile>();
		public SortieFleetData FleetData { get; set; } = new SortieFleetData();
		public SortieFleetData? FleetAfterSortieData { get; set; }
		public SortieMapData MapData { get; set; } = new SortieMapData();
		public CalculatedSortieCost? CalculatedSortieCost { get; set; }
	}
}
