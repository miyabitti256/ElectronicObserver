#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using ElectronicObserver.Data;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport
{
	public static class SortieExternalLinkService
	{
		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = false,
		};

		/// <summary>
		/// デッキビルダー互換の JSON 文字列を生成します。
		/// </summary>
		public static string GenerateDeckBuilderJson(SortieRecord record)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));

			int hqlv = 120;
			try
			{
				if (KCDatabase.Instance?.Admiral?.IsAvailable == true)
				{
					hqlv = KCDatabase.Instance.Admiral.Level;
				}
			}
			catch
			{
				// Why not throw: Fallback to 120 if admiral master data is not loaded.
				hqlv = 120;
			}

			var dict = new Dictionary<string, object>
			{
				["version"] = 4,
				["hqlv"] = hqlv,
			};

			if (record.FleetData?.Fleets != null)
			{
				int fleetIndex = 1;
				foreach (var fleet in record.FleetData.Fleets)
				{
					if (fleet?.Ships != null && fleet.Ships.Count > 0)
					{
						var fleetDict = new Dictionary<string, object>();
						int shipIndex = 1;
						foreach (var ship in fleet.Ships)
						{
							if (ship == null) continue;

							var shipDict = new Dictionary<string, object>
							{
								["id"] = ship.Id,
								["lv"] = ship.Level,
								["luck"] = ship.LuckTotal,
							};

							var itemsDict = new Dictionary<string, object>();
							int eqIndex = 1;
							if (ship.EquipmentSlots != null)
							{
								foreach (var slot in ship.EquipmentSlots)
								{
									if (slot?.Equipment != null && slot.Equipment.Id > 0)
									{
										itemsDict[$"i{eqIndex}"] = new Dictionary<string, object>
										{
											["id"] = slot.Equipment.Id,
											["rf"] = slot.Equipment.Level,
											["mas"] = slot.Equipment.AircraftLevel,
										};
									}
									eqIndex++;
								}
							}

							if (ship.ExpansionSlot?.Equipment != null && ship.ExpansionSlot.Equipment.Id > 0)
							{
								itemsDict["ix"] = new Dictionary<string, object>
								{
									["id"] = ship.ExpansionSlot.Equipment.Id,
									["rf"] = ship.ExpansionSlot.Equipment.Level,
									["mas"] = ship.ExpansionSlot.Equipment.AircraftLevel,
								};
							}

							shipDict["items"] = itemsDict;
							fleetDict[$"s{shipIndex}"] = shipDict;
							shipIndex++;
						}

						dict[$"f{fleetIndex}"] = fleetDict;
					}
					fleetIndex++;
				}
			}

			if (record.FleetData?.AirBases != null)
			{
				int airBaseIndex = 1;
				foreach (var airBase in record.FleetData.AirBases.Where(b => b.MapAreaId == record.World))
				{
					var airBaseDict = new Dictionary<string, object>();
					var itemsDict = new Dictionary<string, object>();
					int sqIndex = 1;

					if (airBase.Squadrons != null)
					{
						foreach (var squadron in airBase.Squadrons)
						{
							if (squadron?.EquipmentSlot?.Equipment != null && squadron.EquipmentSlot.Equipment.Id > 0)
							{
								itemsDict[$"i{sqIndex}"] = new Dictionary<string, object>
								{
									["id"] = squadron.EquipmentSlot.Equipment.Id,
									["rf"] = squadron.EquipmentSlot.Equipment.Level,
									["mas"] = squadron.EquipmentSlot.Equipment.AircraftLevel,
								};
							}
							sqIndex++;
						}
					}

					airBaseDict["items"] = itemsDict;
					airBaseDict["mode"] = (int)airBase.ActionKind;
					dict[$"a{airBaseIndex}"] = airBaseDict;
					airBaseIndex++;
				}
			}

			return JsonSerializer.Serialize(dict, JsonOptions);
		}

		/// <summary>
		/// 制空権シミュレータ (kc-web) のインポート URL を生成します。
		/// </summary>
		public static string GenerateAirControlSimulatorUrl(SortieRecord record)
		{
			string deckJson = GenerateDeckBuilderJson(record);
			return "https://noro6.github.io/kc-web#import:" + deckJson;
		}

		/// <summary>
		/// 作戦室 (Jervis) のインポート URL を生成します。
		/// </summary>
		public static string GenerateOperationRoomUrl(SortieRecord record)
		{
			string deckJson = GenerateDeckBuilderJson(record);
			return "https://jervis.vercel.app?predeck=" + Uri.EscapeDataString(deckJson);
		}

		/// <summary>
		/// 出撃レコードと API ファイル群をエクスポート用 JSON にシリアライズします。
		/// </summary>
		public static string ExportSortieRecordJson(SortieRecord record, IEnumerable<ApiFile> apiFiles)
		{
			if (record == null) throw new ArgumentNullException(nameof(record));

			var container = new SortieExportPackage
			{
				Record = record,
				ApiFiles = apiFiles?.Select(f => new ApiFileExportDto
				{
					ApiFileType = (int)f.ApiFileType,
					Name = f.Name,
					Content = f.ContentString,
					TimeStamp = f.TimeStamp,
					Version = f.Version,
				}).ToList() ?? new List<ApiFileExportDto>(),
			};

			return JsonSerializer.Serialize(container, JsonOptions);
		}

		/// <summary>
		/// エクスポート JSON から出撃レコードと API ファイル群を復元します。
		/// </summary>
		public static (SortieRecord Record, List<ApiFile> ApiFiles) ImportSortieRecordJson(string json)
		{
			if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("JSON is null or empty.", nameof(json));

			var container = JsonSerializer.Deserialize<SortieExportPackage>(json, JsonOptions);
			if (container?.Record == null)
			{
				throw new InvalidOperationException("Failed to deserialize SortieRecord from json.");
			}

			var apiFiles = new List<ApiFile>();
			if (container.ApiFiles != null)
			{
				foreach (var dto in container.ApiFiles)
				{
					var file = new ApiFile
					{
						ApiFileType = (ApiFileType)dto.ApiFileType,
						Name = dto.Name,
						ContentString = dto.Content ?? "",
						TimeStamp = dto.TimeStamp,
						Version = dto.Version,
						SortieRecordId = container.Record.Id,
					};
					apiFiles.Add(file);
				}
			}

			return (container.Record, apiFiles);
		}

		private class SortieExportPackage
		{
			[JsonPropertyName("record")]
			public SortieRecord? Record { get; set; }

			[JsonPropertyName("api_files")]
			public List<ApiFileExportDto>? ApiFiles { get; set; }
		}

		private class ApiFileExportDto
		{
			[JsonPropertyName("type")]
			public int ApiFileType { get; set; }

			[JsonPropertyName("name")]
			public string? Name { get; set; }

			[JsonPropertyName("content")]
			public string? Content { get; set; }

			[JsonPropertyName("timestamp")]
			public DateTime TimeStamp { get; set; }

			[JsonPropertyName("version")]
			public int Version { get; set; }
		}
	}
}
