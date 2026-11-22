#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace ElectronicObserver.Data
{
	public class DestinationDataService
	{
		private static readonly Lazy<DestinationDataService> _instance = new Lazy<DestinationDataService>(() => new DestinationDataService());
		public static DestinationDataService Instance => _instance.Value;

		private readonly Dictionary<int, (string PreviousCellDisplay, string CellDisplay)> _destinations = new Dictionary<int, (string, string)>();

		public bool IsLoaded => _destinations.Count > 0;

		public void Initialize(string? filePath = null)
		{
			try
			{
				string path = filePath ?? FindDestinationFile();
				if (string.IsNullOrEmpty(path) || !File.Exists(path))
				{
					return;
				}

				string jsonText = File.ReadAllText(path);
				using (var doc = JsonDocument.Parse(jsonText))
				{
					_destinations.Clear();

					foreach (JsonProperty property in doc.RootElement.EnumerateObject())
					{
						if (property.NameEquals("version")) continue;

						string name = property.Name;
						int dashIdx = name.IndexOf('-');
						if (dashIdx == -1) continue;

						int spaceIdx = name.IndexOf(' ');
						string worldStr = spaceIdx != -1 ? name.Substring(spaceIdx + 1, dashIdx - spaceIdx - 1) : name.Substring(0, dashIdx);
						string mapStr = name.Substring(dashIdx + 1);

						if (!int.TryParse(worldStr, out int world) || !int.TryParse(mapStr, out int map))
							continue;

						foreach (JsonProperty stepProp in property.Value.EnumerateObject())
						{
							if (!int.TryParse(stepProp.Name, out int cellId))
								continue;

							if (stepProp.Value.ValueKind == JsonValueKind.Array && stepProp.Value.GetArrayLength() >= 2)
							{
								string prev = stepProp.Value[0].GetString() ?? "";
								string curr = stepProp.Value[1].GetString() ?? "";
								int hash = GetHashCode(world, map, cellId);
								_destinations[hash] = (prev, curr);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Utility.Logger.Add(2, "destination.jsonの読み込みに失敗しました: " + ex.Message);
			}
		}

		private static string FindDestinationFile()
		{
			string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "destination.json");
			if (File.Exists(localPath)) return localPath;

			string relPath = Path.Combine("Data", "destination.json");
			if (File.Exists(relPath)) return relPath;

			string appData = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"ElectronicObserver", "DataAndTranslations", "Data", "destination.json");
			if (File.Exists(appData)) return appData;

			return "";
		}

		public string CellDisplay(int mapAreaId, int mapInfoId, int cellId)
		{
			int hash = GetHashCode(mapAreaId, mapInfoId, cellId);
			if (_destinations.TryGetValue(hash, out var val))
			{
				return val.CellDisplay;
			}
			return cellId.ToString();
		}

		public string CellDisplayWithId(int mapAreaId, int mapInfoId, int cellId)
		{
			int hash = GetHashCode(mapAreaId, mapInfoId, cellId);
			if (_destinations.TryGetValue(hash, out var val))
			{
				return string.Format("{0} ({1})", val.CellDisplay, cellId);
			}
			return cellId.ToString();
		}

		public string PreviousCellDisplay(int mapAreaId, int mapInfoId, int cellId)
		{
			int hash = GetHashCode(mapAreaId, mapInfoId, cellId);
			if (_destinations.TryGetValue(hash, out var val))
			{
				return val.PreviousCellDisplay;
			}
			return cellId.ToString();
		}

		private static int GetHashCode(int mapAreaId, int mapInfoId, int cellId)
		{
			int hash = mapAreaId;
			hash = hash * 397 ^ mapInfoId;
			hash = hash * 397 ^ cellId;
			return hash;
		}
	}
}
