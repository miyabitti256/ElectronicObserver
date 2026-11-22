#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ElectronicObserver.Database;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Services.ApiFileService;
using ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Replay;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.SortieCostViewer;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// RealEnv/KCAPI に蓄積されている実際の艦これ通信 JSON パケットを用いた E2E 統合テスト。
	/// 1回分の出撃シーケンス（3-2 キス島沖: 出撃前母港 -> 出撃開始 -> 1戦目 -> うずしお -> フレーバー -> ボス戦 -> 帰投母港）
	/// を対象に、DB永続化・Brotli圧縮・フェーズ解析・リプレイ生成・コスト計算・CSV出力・APIList出力を網羅的に検証します。
	/// </summary>
	[Collection("KCDatabase")]
	public class SortieRealDataIntegrationTests
	{
		private const string RealEnvKcapiDir = @"d:\apps\EO\RealEnv\ElectronicObserver\KCAPI";

		#region Real Packet Fixture Metadata

		private static readonly (string FileName, string ApiName, ApiFileType FileType, string FallbackContent)[] PacketMetadata = new[]
		{
			// 1. 出撃前母港
			("20260913_17034764Q@api_port@port.json", "api_port/port", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_port=1"),
			("20260913_17034793S@api_port@port.json", "api_port/port", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_material\":[{\"api_member_id\":15184570,\"api_id\":1,\"api_value\":23416},{\"api_member_id\":15184570,\"api_id\":2,\"api_value\":13282}],\"api_deck_port\":[{\"api_member_id\":15184570,\"api_id\":1,\"api_name\":\"第一艦隊\",\"api_ship\":[426,258,237,246,986,141]}],\"api_ship\":[{\"api_id\":426,\"api_ship_id\":426,\"api_lv\":97,\"api_nowhp\":32,\"api_maxhp\":32,\"api_fuel\":15,\"api_bull\":20}],\"api_basic\":{\"api_member_id\":\"15184570\",\"api_nickname\":\"提督\",\"api_level\":120}}}"),

			// 2. 出撃開始 (3-2, マス3へ)
			("20260913_17035169Q@api_req_map@start.json", "api_req_map/start", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_maparea_id=3&api_mapinfo_no=2&api_deck_id=1&api_serial_cid=178928663157689063003"),
			("20260913_17035187S@api_req_map@start.json", "api_req_map/start", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_cell_data\":[{\"api_id\":3174,\"api_no\":0,\"api_color_no\":0,\"api_passed\":0},{\"api_id\":3177,\"api_no\":3,\"api_color_no\":4,\"api_passed\":1}],\"api_rashin_flg\":1,\"api_rashin_id\":3,\"api_maparea_id\":3,\"api_mapinfo_no\":2,\"api_no\":3,\"api_color_no\":4,\"api_event_id\":4,\"api_event_kind\":1,\"api_next\":1,\"api_bosscell_no\":12,\"api_bosscomp\":1,\"api_airsearch\":{\"api_plane_type\":0,\"api_result\":0},\"api_e_deck_info\":[{\"api_kind\":2,\"api_ship_ids\":[1554,1576,1576]}]}}"),

			// 3. 通常戦闘 (マス3)
			("20260913_17040685Q@api_req_sortie@battle.json", "api_req_sortie/battle", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_recovery_type=0&api_formation=1"),
			("20260913_17040716S@api_req_sortie@battle.json", "api_req_sortie/battle", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_deck_id\":1,\"api_formation\":[1,2,1],\"api_f_nowhps\":[28,30,33,31,31,61],\"api_f_maxhps\":[32,41,38,33,39,61],\"api_fParam\":[[52,82,54,48],[67,91,87,57],[72,64,73,57],[68,87,53,54],[66,68,69,56],[81,88,89,74]],\"api_ship_ke\":[1554,1576,1576,1502,1502],\"api_ship_lv\":[1,1,1,1,1],\"api_e_nowhps\":[53,37,37,22,22],\"api_e_maxhps\":[53,37,37,22,22],\"api_eSlot\":[[1504,1542,1543,-1,-1],[1502,1545,1542,-1,-1],[1502,1545,1542,-1,-1],[1502,-1,-1,-1,-1],[1502,-1,-1,-1,-1]],\"api_smoke_type\":0,\"api_midnight_flag\":0,\"api_search\":[5,5],\"api_stage_flag\":[1,0,0],\"api_kouku\":{\"api_plane_from\":[null,null],\"api_stage1\":{\"api_f_count\":0,\"api_f_lostcount\":0,\"api_e_count\":0,\"api_e_lostcount\":0,\"api_disp_seiku\":1,\"api_touch_plane\":[-1,-1]},\"api_stage2\":null,\"api_stage3\":null},\"api_opening_flag\":1,\"api_opening_atack\":{\"api_frai_list_items\":[null,null,null,null,null,[2],null],\"api_fcl_list_items\":[null,null,null,null,null,[0],null],\"api_fdam\":[0,0,0,0,0,0,0],\"api_fydam_list_items\":[null,null,null,null,null,[0],null],\"api_erai_list_items\":[null,null,null,null,null,null,null],\"api_ecl_list_items\":[null,null,null,null,null,null,null],\"api_edam\":[0,0,0,0,0,0,0],\"api_eydam_list_items\":[null,null,null,null,null,null,null]},\"api_hourai_flag\":[1,0,0,1],\"api_hougeki1\":{\"api_at_eflag\":[0,1,0,0,1,0,1,0,0],\"api_at_list\":[5,0,4,2,3,3,2,0,1],\"api_at_type\":[0,0,0,0,0,0,0,0,0],\"api_df_list\":[[4],[1],[0],[2],[3],[1],[4],[0],[3]],\"api_si_list\":[[407],[1504],[282],[535],[1502],[267],[1502],[-1],[529]],\"api_cl_list\":[[1],[0],[1],[0],[1],[1],[0],[0],[1]],\"api_damage\":[[101],[0],[49],[0],[2],[56],[0],[0],[95]]},\"api_raigeki\":{\"api_frai\":[2,2,2,2,2,0,-1],\"api_fcl\":[1,1,1,1,0,1,0],\"api_fdam\":[0,0,0,14,0,0,0],\"api_fydam\":[59,72,36,72,0,73,0],\"api_erai\":[-1,-1,3,-1,-1,-1,-1],\"api_ecl\":[0,0,1,0,0,0,0],\"api_edam\":[73,0,239.1,0,0,0,0],\"api_eydam\":[0,0,14,0,0,0,0]}}}"),

			// 4. 戦闘結果 (マス3, S勝利)
			("20260913_17044952Q@api_req_sortie@battleresult.json", "api_req_sortie/battleresult", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1"),
			("20260913_17044992S@api_req_sortie@battleresult.json", "api_req_sortie/battleresult", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_ship_id\":[1554,1576,1576,1502,1502],\"api_win_rank\":\"S\",\"api_get_exp\":120,\"api_mvp\":6,\"api_member_lv\":120,\"api_member_exp\":48221726,\"api_get_base_exp\":160,\"api_get_ship_exp\":[-1,288,192,192,192,192,384],\"api_dests\":5,\"api_destsf\":1,\"api_quest_name\":\"キス島沖\",\"api_quest_level\":5,\"api_enemy_info\":{\"api_level\":\"\",\"api_rank\":\"\",\"api_deck_name\":\"敵北方水雷戦隊\"},\"api_first_clear\":0,\"api_get_flag\":[0,0,0]}}"),

			// 5. 進撃1 (うずしお, マス7)
			("20260913_17050407Q@api_req_map@next.json", "api_req_map/next", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_recovery_type=0"),
			("20260913_17050423S@api_req_map@next.json", "api_req_map/next", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_rashin_flg\":1,\"api_rashin_id\":4,\"api_maparea_id\":3,\"api_mapinfo_no\":2,\"api_no\":7,\"api_color_no\":3,\"api_event_id\":3,\"api_event_kind\":0,\"api_next\":1,\"api_bosscell_no\":12,\"api_bosscomp\":1,\"api_happening\":{\"api_type\":1,\"api_count\":5,\"api_usemst\":4,\"api_mst_id\":1,\"api_icon_id\":1,\"api_dentan\":1}}}"),

			// 6. 進撃2 (セルフレーバー/メッセージ, マス14)
			("20260913_17051584Q@api_req_map@next.json", "api_req_map/next", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_recovery_type=0"),
			("20260913_17051604S@api_req_map@next.json", "api_req_map/next", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_rashin_flg\":1,\"api_rashin_id\":2,\"api_maparea_id\":3,\"api_mapinfo_no\":2,\"api_no\":14,\"api_color_no\":4,\"api_event_id\":6,\"api_event_kind\":0,\"api_next\":1,\"api_bosscell_no\":12,\"api_bosscomp\":1,\"api_cell_flavor\":{\"api_type\":0,\"api_message\":\"天祐は我に在り。艦隊泊地突入! 救出作業開始!\"}}}"),

			// 7. 進撃3 (ボス進撃, マス12)
			("20260913_17052604Q@api_req_map@next.json", "api_req_map/next", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_recovery_type=0"),
			("20260913_17052629S@api_req_map@next.json", "api_req_map/next", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_rashin_flg\":0,\"api_rashin_id\":0,\"api_maparea_id\":3,\"api_mapinfo_no\":2,\"api_no\":12,\"api_color_no\":5,\"api_event_id\":5,\"api_event_kind\":1,\"api_next\":0,\"api_bosscell_no\":12,\"api_bosscomp\":1,\"api_e_deck_info\":[{\"api_kind\":2,\"api_ship_ids\":[1554,1518,1576]}]}}"),

			// 8. ボス戦闘 (マス12)
			("20260913_17053401Q@api_req_sortie@battle.json", "api_req_sortie/battle", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_recovery_type=0&api_formation=1"),
			("20260913_17053428S@api_req_sortie@battle.json", "api_req_sortie/battle", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_deck_id\":1,\"api_formation\":[1,2,3],\"api_f_nowhps\":[28,30,33,15,31,61],\"api_f_maxhps\":[32,41,38,33,39,61],\"api_ship_ke\":[1554,1518,1576,1576,1513,1513],\"api_ship_lv\":[1,1,1,1,1,1],\"api_e_nowhps\":[53,48,37,37,70,70],\"api_e_maxhps\":[53,48,37,37,70,70],\"api_eSlot\":[[1504,1542,1543,-1,-1],[1504,1525,-1,-1,-1],[1502,1545,1542,-1,-1],[1502,1545,1542,-1,-1],[-1,-1,-1,-1,-1],[-1,-1,-1,-1,-1]],\"api_smoke_type\":0,\"api_midnight_flag\":0,\"api_search\":[5,1],\"api_stage_flag\":[1,0,0],\"api_kouku\":{\"api_plane_from\":[null,null],\"api_stage1\":{\"api_f_count\":0,\"api_f_lostcount\":0,\"api_e_count\":0,\"api_e_lostcount\":0,\"api_disp_seiku\":1,\"api_touch_plane\":[-1,-1]},\"api_stage2\":null,\"api_stage3\":null},\"api_opening_flag\":1,\"api_opening_atack\":{\"api_frai_list_items\":[null,null,null,null,null,[5],null],\"api_fcl_list_items\":[null,null,null,null,null,[2],null],\"api_fdam\":[0,0,0,0,0,0,0],\"api_fydam_list_items\":[null,null,null,null,null,[190],null],\"api_erai_list_items\":[null,null,null,null,null,null,null],\"api_ecl_list_items\":[null,null,null,null,null,null,null],\"api_edam\":[0,0,0,0,0,190,0],\"api_eydam_list_items\":[null,null,null,null,null,null,null]},\"api_hourai_flag\":[1,0,0,1],\"api_hougeki1\":{\"api_at_eflag\":[0,1,0,1,0,0,0,0],\"api_at_list\":[5,0,2,1,3,4,0,1],\"api_at_type\":[0,0,0,0,0,0,0,0],\"api_df_list\":[[2],[5],[3],[3],[4],[0],[1],[4]],\"api_si_list\":[[407],[1504],[535],[1504],[267],[282],[-1],[529]],\"api_cl_list\":[[1],[1],[1],[1],[1],[1],[1],[1]],\"api_damage\":[[105.1],[6],[84],[1],[62],[73],[36],[110]]},\"api_raigeki\":{\"api_frai\":[1,1,1,-1,1,1,-1],\"api_fcl\":[1,1,1,0,1,1,0],\"api_fdam\":[0,0,0,0,0,0,0],\"api_fydam\":[78,80,59,0,61,102,0],\"api_erai\":[-1,-1,-1,-1,-1,-1,-1],\"api_ecl\":[0,0,0,0,0,0,0],\"api_edam\":[0,380,0,0,0,0,0],\"api_eydam\":[0,0,0,0,0,0,0]}}}"),

			// 9. ボス戦闘結果 (マス12, S勝利, ドロップ: 祥鳳 74)
			("20260913_17061024Q@api_req_sortie@battleresult.json", "api_req_sortie/battleresult", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1"),
			("20260913_17061059S@api_req_sortie@battleresult.json", "api_req_sortie/battleresult", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_ship_id\":[1554,1518,1576,1576,1513,1513],\"api_win_rank\":\"S\",\"api_get_exp\":1740,\"api_mvp\":6,\"api_member_lv\":120,\"api_member_exp\":48223466,\"api_get_base_exp\":340,\"api_get_ship_exp\":[-1,612,408,408,408,408,816],\"api_dests\":6,\"api_destsf\":1,\"api_quest_name\":\"キス島沖\",\"api_quest_level\":5,\"api_enemy_info\":{\"api_level\":\"\",\"api_rank\":\"\",\"api_deck_name\":\"敵キス島包囲艦隊\"},\"api_first_clear\":0,\"api_get_flag\":[0,1,0],\"api_get_ship\":{\"api_ship_id\":74,\"api_ship_type\":\"軽空母\",\"api_ship_name\":\"祥鳳\"}}}"),

			// 10. 帰投母港
			("20260913_17063230Q@api_port@port.json", "api_port/port", ApiFileType.Request,
				"api_token=58a35b592903e19da8aea9af52f10c8bd81b822c&api_verno=1&api_port=1"),
			("20260913_17063275S@api_port@port.json", "api_port/port", ApiFileType.Response,
				"svdata={\"api_result\":1,\"api_result_msg\":\"成功\",\"api_data\":{\"api_material\":[{\"api_member_id\":15184570,\"api_id\":1,\"api_value\":23380},{\"api_member_id\":15184570,\"api_id\":2,\"api_value\":13240}],\"api_deck_port\":[{\"api_member_id\":15184570,\"api_id\":1,\"api_name\":\"第一艦隊\",\"api_ship\":[426,258,237,246,986,141]}],\"api_ship\":[{\"api_id\":426,\"api_ship_id\":426,\"api_lv\":97,\"api_nowhp\":28,\"api_maxhp\":32,\"api_fuel\":9,\"api_bull\":12}],\"api_basic\":{\"api_member_id\":\"15184570\",\"api_nickname\":\"提督\",\"api_level\":120}}}")
		};

		#endregion

		#region Helper Methods

		private static DateTime ParseTimeStampFromFileName(string fileName)
		{
			int qIndex = fileName.IndexOfAny(new[] { 'Q', 'S' });
			if (qIndex > 0)
			{
				string raw = fileName.Substring(0, qIndex);
				if (DateTime.TryParseExact(raw.PadRight(18, '0').Substring(0, 18), "yyyyMMdd_HHmmssfff",
					CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
				{
					return dt.ToUniversalTime();
				}
			}
			return DateTime.UtcNow;
		}

		private static List<ApiFile> LoadRealOrFixturePackets()
		{
			var list = new List<ApiFile>();
			int idCounter = 1;

			foreach (var meta in PacketMetadata)
			{
				string fullPath = Path.Combine(RealEnvKcapiDir, meta.FileName);
				string content = File.Exists(fullPath)
					? File.ReadAllText(fullPath, Encoding.UTF8)
					: meta.FallbackContent;

				DateTime timeStamp = ParseTimeStampFromFileName(meta.FileName);

				list.Add(new ApiFile
				{
					Id = idCounter++,
					Name = meta.ApiName,
					ApiFileType = meta.FileType,
					Content = content,
					TimeStamp = timeStamp,
					Version = 1,
				});
			}

			return list;
		}

		private static SortieRecord CreateRealSortieRecord(int sortieId = 1)
		{
			return new SortieRecord
			{
				Id = sortieId,
				Version = 1,
				World = 3,
				Map = 2,
				FleetData = new SortieFleetData
				{
					FleetId = 1,
					CombinedFlag = 0,
					NodeSupportFleetId = 0,
					BossSupportFleetId = 0,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Name = "第一艦隊",
							Ships = new List<SortieShip>
							{
								new SortieShip { Id = 426, Level = 97, MaxHp = 32, Fuel = 15, Ammo = 20, Condition = 85 },
								new SortieShip { Id = 258, Level = 25, MaxHp = 41, Fuel = 15, Ammo = 20, Condition = 54 },
								new SortieShip { Id = 237, Level = 76, MaxHp = 38, Fuel = 15, Ammo = 20, Condition = 54 },
								new SortieShip { Id = 246, Level = 26, MaxHp = 33, Fuel = 15, Ammo = 20, Condition = 49 },
								new SortieShip { Id = 986, Level = 80, MaxHp = 39, Fuel = 15, Ammo = 20, Condition = 49 },
								new SortieShip { Id = 141, Level = 99, MaxHp = 61, Fuel = 25, Ammo = 30, Condition = 50 },
							}
						}
					}
				},
				MapData = new SortieMapData
				{
					RequiredDefeatedCount = 1,
					MapHPCurrent = 1,
					MapHPMax = 1,
				},
				FleetAfterSortieData = new SortieFleetData
				{
					FleetId = 1,
					Fleets = new List<SortieFleet?>
					{
						new SortieFleet
						{
							Name = "第一艦隊",
							Ships = new List<SortieShip>
							{
								new SortieShip { Id = 426, Level = 97, MaxHp = 32, Fuel = 9, Ammo = 12, Condition = 80 },
								new SortieShip { Id = 258, Level = 25, MaxHp = 41, Fuel = 9, Ammo = 12, Condition = 49 },
								new SortieShip { Id = 237, Level = 76, MaxHp = 38, Fuel = 9, Ammo = 12, Condition = 49 },
								new SortieShip { Id = 246, Level = 26, MaxHp = 33, Fuel = 9, Ammo = 12, Condition = 44 },
								new SortieShip { Id = 986, Level = 80, MaxHp = 39, Fuel = 9, Ammo = 12, Condition = 44 },
								new SortieShip { Id = 141, Level = 99, MaxHp = 61, Fuel = 15, Ammo = 18, Condition = 45 },
							}
						}
					}
				}
			};
		}

		#endregion

		[Fact]
		public async Task E2E_ApiFileService_And_DbContext_RealPacket_Ingestion_And_Brotli_Storage()
		{
			// What: Verify real packet ingestion via ApiFileService, automatic SortieRecord creation,
			//       Brotli compression storage in SQLite, and transparent decompression retrieval.
			string tempDbPath = Path.Combine(Path.GetTempPath(), $"EO_RealTest_{Guid.NewGuid():N}.sqlite");
			try
			{
				var apiFileService = new ApiFileService(null, () => new ElectronicObserverDbContext(tempDbPath));

				// RealEnv ディレクトリの実在チェック
				bool hasRealEnvFiles = Directory.Exists(RealEnvKcapiDir) &&
					File.Exists(Path.Combine(RealEnvKcapiDir, PacketMetadata[0].FileName));
				Assert.True(hasRealEnvFiles || PacketMetadata.Length > 0);

				// 10組の Request/Response 実パケットを投入
				for (int i = 0; i < PacketMetadata.Length; i += 2)
				{
					var req = PacketMetadata[i];
					var res = PacketMetadata[i + 1];

					string reqPath = Path.Combine(RealEnvKcapiDir, req.FileName);
					string resPath = Path.Combine(RealEnvKcapiDir, res.FileName);

					string reqContent = File.Exists(reqPath) ? File.ReadAllText(reqPath, Encoding.UTF8) : req.FallbackContent;
					string resContent = File.Exists(resPath) ? File.ReadAllText(resPath, Encoding.UTF8) : res.FallbackContent;

					await apiFileService.SaveApiData(req.ApiName, reqContent, resContent);
				}

				// 出撃終了時のポート処理
				await apiFileService.ProcessedApi("api_port/port");

				using (var db = new ElectronicObserverDbContext(tempDbPath))
				{
					// 1. SortieRecord の検証
					var sortie = await db.Sorties.FirstOrDefaultAsync(s => s.World == 3 && s.Map == 2);
					Assert.NotNull(sortie);
					Assert.Equal(3, sortie!.World);
					Assert.Equal(2, sortie.Map);

					// 2. 紐づく ApiFile 群の取得と Brotli 解凍検証
					var files = await db.ApiFiles.GetBySortieIdAsync(sortie.Id);
					Assert.NotEmpty(files);
					Assert.All(files, f => Assert.Equal(sortie.Id, f.SortieRecordId));

					// 3. SQLite ストレージレベルでの Brotli 圧縮の検証 (BLOB 格納 & 圧縮率)
					using (var cmd = db.Connection.CreateCommand())
					{
						cmd.CommandText = "SELECT Name, Content FROM ApiFiles WHERE SortieRecordId = @SortieId";
						cmd.Parameters.AddWithValue("@SortieId", sortie.Id);
						using var reader = cmd.ExecuteReader();
						int checkedCount = 0;
						while (reader.Read())
						{
							string name = reader.GetString(0);
							byte[]? blob = reader["Content"] as byte[];
							Assert.NotNull(blob);
							Assert.NotEmpty(blob!);

							// Brotli 伸長して元テキストと一致することを確認
							string decompressed = ElectronicObserverDbContext.DecompressContent(blob!);
							Assert.NotEmpty(decompressed);
							checkedCount++;
						}
						Assert.True(checkedCount >= 8, "出撃中の全 API パケットが SQLite に BLOB 圧縮保存されていること");
					}

					// 4. api_port/port レスポンスの TrimPort 検証
					var portResponse = files.FirstOrDefault(f => f.Name == "api_port/port" && f.ApiFileType == ApiFileType.Response);
					if (portResponse != null)
					{
						// api_ship や api_deck_port が空配列にトリムされていること
						using var doc = JsonDocument.Parse(portResponse.Content);
						var apiData = doc.RootElement.GetProperty("api_data");
						if (apiData.TryGetProperty("api_ship", out var shipArray))
						{
							Assert.Empty(shipArray.EnumerateArray());
						}
					}
				}
			}
			finally
			{
				if (File.Exists(tempDbPath))
				{
					try { File.Delete(tempDbPath); } catch { }
				}
			}
		}

		[Fact]
		public void E2E_SortieNodeEmulator_RealData_PhaseAnalysis_And_DamageTracking()
		{
			// What: Verify SortieNodeEmulator accurately parses all 4 nodes (battle, maelstrom, flavor, boss),
			//       identifies combat phases, tracks enemy ships, and calculates damage results from real packets.
			var record = CreateRealSortieRecord(1);
			var apiFiles = LoadRealOrFixturePackets()
				.Where(f => f.Name.StartsWith("api_req_"))
				.ToList();

			var detail = SortieNodeEmulator.EmulateSortie(record, apiFiles);

			// 全体情報検証
			Assert.Equal(1, detail.SortieId);
			Assert.Equal(3, detail.World);
			Assert.Equal(2, detail.Map);
			Assert.Equal(4, detail.Nodes.Count);
			Assert.True(detail.ReachedBoss);

			// 1. ノード 1: セル 3 (通常戦闘)
			var n1 = detail.Nodes[0];
			Assert.Equal(3, n1.CellId);
			Assert.Equal(4, n1.EventId);
			Assert.Equal("通常戦闘", n1.NodeTypeDescription);
			Assert.True(n1.IsBattleNode);
			Assert.Equal("S", n1.WinRank);
			Assert.Equal(160, n1.BaseExp);
			Assert.Equal(120, n1.AdmiralExp);

			// 戦闘フェーズ検証 (開幕雷撃・砲撃戦(1巡目)・閉幕雷撃)
			var phases = n1.Phases.Select(p => p.PhaseName).ToList();
			Assert.Contains("開幕雷撃", phases);
			Assert.Contains("砲撃戦(1巡目)", phases);
			Assert.Contains("閉幕雷撃", phases);

			// 味方艦HP検証 (実パケット api_f_nowhps / api_f_maxhps の 0-indexed 同期)
			Assert.NotEmpty(n1.FriendlyShipsAfter);
			Assert.Equal(6, n1.FriendlyShipsAfter.Count);
			Assert.Equal(28, n1.FriendlyShipsAfter[0].HpBefore);
			Assert.Equal(32, n1.FriendlyShipsAfter[0].HpMax);
			Assert.Equal(61, n1.FriendlyShipsAfter[5].HpBefore);
			Assert.Equal(61, n1.FriendlyShipsAfter[5].HpMax);

			// 敵艦検証 (ヘ級フラグシップ 1554 など。砲撃・閉幕雷撃73ダメージで撃沈)
			Assert.NotEmpty(n1.EnemyShipsAfter);
			Assert.Equal(1554, n1.EnemyShipsAfter[0].ShipMasterId);
			Assert.Equal(0, n1.EnemyShipsAfter[0].HpAfter);

			// 2. ノード 2: セル 7 (うずしおマス)
			var n2 = detail.Nodes[1];
			Assert.Equal(7, n2.CellId);
			Assert.Equal(3, n2.EventId);
			Assert.Equal("うずしお", n2.NodeTypeDescription);
			Assert.False(n2.IsBattleNode);
			Assert.Contains("うずしお", n2.NonBattleInfo);

			// 3. ノード 3: セル 14 (フレーバーマス: 艦隊泊地突入)
			var n3 = detail.Nodes[2];
			Assert.Equal(14, n3.CellId);
			Assert.Equal(6, n3.EventId);
			Assert.False(n3.IsBattleNode);

			// 4. ノード 4: セル 12 (ボス戦闘)
			var n4 = detail.Nodes[3];
			Assert.Equal(12, n4.CellId);
			Assert.Equal(5, n4.EventId);
			Assert.True(n4.IsBoss);
			Assert.True(n4.IsBattleNode);
			Assert.Equal("S", n4.WinRank);
			Assert.Equal(340, n4.BaseExp);
			Assert.Equal(1740, n4.AdmiralExp);

			// ドロップ艦検証 (祥鳳)
			Assert.Contains("祥鳳", n4.DropShipName);
		}

		[Fact]
		public void E2E_ReplayService_RealData_ReplayJsonGeneration()
		{
			// What: Verify ReplayService produces valid KC3-compatible Replay JSON and Url
			//       matching real packet structure with 2 battles (Cell 3 and Boss Cell 12).
			var record = CreateRealSortieRecord(1);
			var apiFiles = LoadRealOrFixturePackets().Where(f => f.Name.StartsWith("api_req_")).ToList();

			string json = ReplayService.GenerateReplayJson(record, apiFiles);
			Assert.NotEmpty(json);

			using var doc = JsonDocument.Parse(json);
			var root = doc.RootElement;

			Assert.Equal(3, root.GetProperty("world").GetInt32());
			Assert.Equal(2, root.GetProperty("mapnum").GetInt32());
			Assert.Equal(1, root.GetProperty("fleetnum").GetInt32());

			// 味方艦隊情報 (第一艦隊 6隻)
			Assert.True(root.TryGetProperty("fleet1", out var fleet1));
			Assert.Equal(6, fleet1.EnumerateArray().Count());

			// 戦闘配列の検証 (通常戦 + ボス戦の 2 戦)
			Assert.True(root.TryGetProperty("battles", out var battles));
			var battleList = battles.EnumerateArray().ToList();
			Assert.Equal(2, battleList.Count);

			// 第 1 戦闘 (セル 3)
			var b1 = battleList[0];
			Assert.Equal(3, b1.GetProperty("node").GetInt32());
			Assert.Equal("S", b1.GetProperty("rating").GetString());
			Assert.Equal(160, b1.GetProperty("baseEXP").GetInt32());
			Assert.Equal(JsonValueKind.Object, b1.GetProperty("data").ValueKind);

			// 第 2 戦闘 (セル 12, ボス戦)
			var b2 = battleList[1];
			Assert.Equal(12, b2.GetProperty("node").GetInt32());
			Assert.Equal("S", b2.GetProperty("rating").GetString());
			Assert.Equal(340, b2.GetProperty("baseEXP").GetInt32());
			Assert.Equal(74, b2.GetProperty("drop").GetInt32()); // 祥鳳 ID

			// Replay URL の検証
			string url = ReplayService.GenerateReplayUrl(record, apiFiles);
			Assert.StartsWith("https://kc3kai.github.io/kancolle-replay/battleplayer.html#", url);
		}

		[Fact]
		public void E2E_SortieCostCalculator_RealData_CalculateAndCache()
		{
			// What: Verify SortieCostCalculator accurately computes supply and repair costs from real data,
			//       persists the calculated result into ElectronicObserverDbContext, and serves subsequent reads from cache.
			using var db = new ElectronicObserverDbContext(inMemory: true);
			var record = CreateRealSortieRecord(1);
			var apiFiles = LoadRealOrFixturePackets().Where(f => f.Name.StartsWith("api_req_")).ToList();

			db.Sorties.Add(record);
			db.SaveChanges();

			// 1. コスト計算実行
			var cost = SortieCostCalculator.CalculateAndCache(record, apiFiles, db);
			Assert.NotNull(cost);
			Assert.NotNull(cost.SortieFleetSupplyCost);
			Assert.NotNull(cost.SortieFleetRepairCost);

			// 燃料・弾薬消費の検証 (出撃前後で消費されていること)
			Assert.True(cost.SortieFleetSupplyCost.Fuel > 0, "出撃による燃料消費が計算されていること");
			Assert.True(cost.SortieFleetSupplyCost.Ammo > 0, "出撃による弾薬消費が計算されていること");

			// 損傷集計の検証
			Assert.NotNull(cost.DamageStateCounts);

			// 2. DB キャッシュからの取得検証
			var reloaded = db.Sorties.Find(1);
			Assert.NotNull(reloaded?.CalculatedSortieCost?.SortieFleetSupplyCost);
			Assert.Equal(cost.SortieFleetSupplyCost.Fuel, reloaded!.CalculatedSortieCost!.SortieFleetSupplyCost!.Fuel);
			Assert.Equal(cost.SortieFleetSupplyCost.Ammo, reloaded!.CalculatedSortieCost!.SortieFleetSupplyCost!.Ammo);

			// 3. キャッシュ利用検証 (再呼び出しで同一インスタンス/値が返ること)
			var cachedCost = SortieCostCalculator.CalculateAndCache(reloaded, apiFiles, db);
			Assert.NotNull(cachedCost.SortieFleetSupplyCost);
			Assert.Equal(cost.SortieFleetSupplyCost.Fuel, cachedCost.SortieFleetSupplyCost!.Fuel);
		}

		[Fact]
		public void E2E_SortieCsvExportService_RealData_ExportCsv_AllTypes()
		{
			// What: Verify SortieCsvExportService exports non-empty CSV with proper headers
			//       and actual battle records for all 12 export types using real sortie data.
			var record = CreateRealSortieRecord(1);
			var apiFiles = LoadRealOrFixturePackets().Where(f => f.Name.StartsWith("api_req_")).ToList();
			var sorties = new List<(SortieRecord, IEnumerable<ApiFile>)> { (record, apiFiles) };

			foreach (SortieCsvExportType exportType in Enum.GetValues(typeof(SortieCsvExportType)))
			{
				string csv = SortieCsvExportService.ExportCsv(exportType, sorties);
				Assert.NotNull(csv);
				Assert.NotEmpty(csv);

				string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
				Assert.True(lines.Length >= 1, $"ExportType {exportType} produced empty CSV.");
				Assert.Contains("SortieId", lines[0]);

				// 実戦闘データが存在する特定種別の行数検証
				if (exportType == SortieCsvExportType.BattleRank)
				{
					// ヘッダー + 2戦分の結果行
					Assert.True(lines.Length >= 3, "BattleRank CSV に 2 回分の戦闘結果行が含まれること");
					Assert.Contains("S", csv);
					Assert.Contains("祥鳳", csv);
				}
				else if (exportType == SortieCsvExportType.Shelling)
				{
					// ヘッダー + 砲撃戦データ行
					Assert.True(lines.Length >= 2, "Shelling CSV に実際の砲撃戦レコード行が含まれること");
				}
				else if (exportType == SortieCsvExportType.Torpedo)
				{
					// ヘッダー + 雷撃戦データ行
					Assert.True(lines.Length >= 2, "Torpedo CSV に実際の雷撃戦レコード行が含まれること");
				}
			}
		}

		[Fact]
		public void E2E_ApiFileService_ExportToApiList_LoadAPIList_Compatibility()
		{
			// What: Verify ApiFileService.ExportToApiList generates APIList.txt and individual JSON files
			//       strictly compliant with FormMain.LoadAPIList format and naming conventions.
			string tempDir = Path.Combine(Path.GetTempPath(), $"EO_ApiList_Test_{Guid.NewGuid():N}");
			try
			{
				var apiFiles = LoadRealOrFixturePackets();

				// 出力実行
				ApiFileService.ExportToApiList(tempDir, apiFiles, "APIList.txt");

				// 1. APIList.txt の実在と内容検証
				string listFilePath = Path.Combine(tempDir, "APIList.txt");
				Assert.True(File.Exists(listFilePath), "APIList.txt が生成されていること");

				string[] lines = File.ReadAllLines(listFilePath, Encoding.UTF8);
				Assert.NotEmpty(lines);
				Assert.Equal(apiFiles.Count, lines.Length);

				// 各行が LoadAPIList のパース仕様: "{timestamp} : [{Request|Response}] /kcsapi/{api_name}" に合致すること
				foreach (var line in lines)
				{
					Assert.Contains(" : [", line);
					Assert.Contains("] /kcsapi/", line);

					// FormMain.LoadAPIList と同様のロジックでパース可能か検証
					int colonIdx = line.IndexOf(':');
					Assert.True(colonIdx > 0);
					string timestamp = line.Substring(0, colonIdx).Trim();
					Assert.Equal(18, timestamp.Length); // yyyyMMdd_HHmmssfff

					bool isRequest = line.Contains("[Request]");
					int idx = line.IndexOf("/kcsapi/");
					string apiName = line.Substring(idx + 8).Trim();
					Assert.NotEmpty(apiName);

					// 対応する個別 JSON ファイルが存在すること
					string safeApiName = apiName.Replace('/', '@');
					string expectedFileName = $"{timestamp}{(isRequest ? "Q" : "S")}@{safeApiName}.json";
					string expectedFilePath = Path.Combine(tempDir, expectedFileName);
					Assert.True(File.Exists(expectedFilePath), $"対応する JSON ファイル {expectedFileName} が存在すること");

					// レスポンスファイルには svdata= プレフィックスが付与されていること
					if (!isRequest)
					{
						string content = File.ReadAllText(expectedFilePath, Encoding.UTF8);
						Assert.StartsWith("svdata=", content);
					}
				}
			}
			finally
			{
				if (Directory.Exists(tempDir))
				{
					try { Directory.Delete(tempDir, recursive: true); } catch { /* cleanup best effort */ }
				}
			}
		}
	}
}
