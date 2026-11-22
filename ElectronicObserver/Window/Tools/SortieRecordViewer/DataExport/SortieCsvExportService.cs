#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ElectronicObserver.Database.KancolleApi;
using ElectronicObserver.Database.Sortie;
using ElectronicObserver.Window.Tools.SortieRecordViewer.Sortie;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.DataExport
{
	public enum SortieCsvExportType
	{
		Shelling,
		Night,
		Torpedo,
		AirBattle,
		AirBase,
		AirBaseDefense,
		BattleRank,
		RedShelling,
		RedNight,
		RedTorpedo,
		RedAirBattle,
		SmokeScreen,
	}

	public static class SortieCsvExportService
	{
		/// <summary>
		/// 指定された出撃データ群から CSV 文字列を生成します。
		/// </summary>
		public static string ExportCsv(SortieCsvExportType exportType, IEnumerable<(SortieRecord Record, IEnumerable<ApiFile> ApiFiles)> sorties)
		{
			if (sorties == null) throw new ArgumentNullException(nameof(sorties));

			var sb = new StringBuilder();

			// ヘッダ行の出力
			sb.AppendLine(GetHeader(exportType));

			foreach (var (record, apiFiles) in sorties)
			{
				if (record == null) continue;

				var detail = SortieNodeEmulator.EmulateSortie(record, apiFiles ?? Enumerable.Empty<ApiFile>());
				string area = $"{record.World}-{record.Map}";

				foreach (var node in detail.Nodes)
				{
					AppendNodeRows(sb, exportType, record.Id, area, node);
				}
			}

			return sb.ToString();
		}

		private static string GetHeader(SortieCsvExportType type)
		{
			return type switch
			{
				SortieCsvExportType.Shelling => "SortieId,Map,Cell,CellDisplay,Phase,Attacker,Defender,AttackerIndex,DefenderIndex,Damage,IsCritical,AttackType",
				SortieCsvExportType.Night => "SortieId,Map,Cell,CellDisplay,Attacker,Defender,AttackerIndex,DefenderIndex,Damage,IsCritical,AttackType",
				SortieCsvExportType.Torpedo => "SortieId,Map,Cell,CellDisplay,Phase,Attacker,Defender,Damage",
				SortieCsvExportType.AirBattle => "SortieId,Map,Cell,CellDisplay,AirControlState,FriendlyAircraftLost,FriendlyDamageTotal,EnemyDamageTotal",
				SortieCsvExportType.AirBase => "SortieId,Map,Cell,CellDisplay,PhaseName,EnemyDamageTotal",
				SortieCsvExportType.AirBaseDefense => "SortieId,Map,Cell,CellDisplay,Info,DamageState",
				SortieCsvExportType.BattleRank => "SortieId,Map,Cell,CellDisplay,WinRank,BaseExp,AdmiralExp,MvpShip,DropShip",
				SortieCsvExportType.RedShelling => "SortieId,Map,Cell,CellDisplay,Phase,Attacker,Defender,Damage,IsCritical,AttackType",
				SortieCsvExportType.RedNight => "SortieId,Map,Cell,CellDisplay,Attacker,Defender,Damage,IsCritical,AttackType",
				SortieCsvExportType.RedTorpedo => "SortieId,Map,Cell,CellDisplay,Phase,Defender,Damage",
				SortieCsvExportType.RedAirBattle => "SortieId,Map,Cell,CellDisplay,Defender,Damage",
				SortieCsvExportType.SmokeScreen => "SortieId,Map,Cell,CellDisplay,SmokeType,PlayerFormation,EnemyFormation,EngagementForm",
				_ => "SortieId,Map,Cell,Info",
			};
		}

		private static void AppendNodeRows(StringBuilder sb, SortieCsvExportType type, int sortieId, string area, SortieNodeInfo node)
		{
			switch (type)
			{
				case SortieCsvExportType.Shelling:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("砲撃戦")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerIndex >= 1 && a.AttackerIndex <= 6))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(phase.PhaseName),
								Escape(atk.AttackerName),
								Escape(atk.DefenderName),
								atk.AttackerIndex,
								atk.DefenderIndex,
								atk.Damage,
								atk.IsCritical,
								Escape(atk.AttackType)));
						}
					}
					break;

				case SortieCsvExportType.Night:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("夜戦")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerIndex >= 1 && a.AttackerIndex <= 6))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(atk.AttackerName),
								Escape(atk.DefenderName),
								atk.AttackerIndex,
								atk.DefenderIndex,
								atk.Damage,
								atk.IsCritical,
								Escape(atk.AttackType)));
						}
					}
					break;

				case SortieCsvExportType.Torpedo:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("雷撃")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerName.Contains("味方")))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(phase.PhaseName),
								Escape(atk.AttackerName),
								Escape(atk.DefenderName),
								atk.Damage));
						}
					}
					break;

				case SortieCsvExportType.AirBattle:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("航空戦") || p.PhaseName.Contains("空襲")))
					{
						int friendlyDam = phase.Attacks.Where(a => a.AttackerName.Contains("敵")).Sum(a => a.Damage);
						int enemyDam = phase.Attacks.Where(a => a.AttackerName.Contains("味方")).Sum(a => a.Damage);

						sb.AppendLine(string.Join(",",
							sortieId,
							Escape(area),
							node.CellId,
							Escape(node.CellDisplay),
							node.AirControlState,
							phase.FriendlyAircraftLost,
							friendlyDam,
							enemyDam));
					}
					break;

				case SortieCsvExportType.AirBase:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("基地")))
					{
						int enemyDam = phase.Attacks.Sum(a => a.Damage);
						sb.AppendLine(string.Join(",",
							sortieId,
							Escape(area),
							node.CellId,
							Escape(node.CellDisplay),
							Escape(phase.PhaseName),
							enemyDam));
					}
					break;

				case SortieCsvExportType.AirBaseDefense:
					if (node.NodeTypeDescription.Contains("空襲"))
					{
						sb.AppendLine(string.Join(",",
							sortieId,
							Escape(area),
							node.CellId,
							Escape(node.CellDisplay),
							Escape(node.NodeTypeDescription),
							Escape(node.NonBattleInfo)));
					}
					break;

				case SortieCsvExportType.BattleRank:
					if (node.IsBattleNode)
					{
						sb.AppendLine(string.Join(",",
							sortieId,
							Escape(area),
							node.CellId,
							Escape(node.CellDisplay),
							Escape(node.WinRank),
							node.BaseExp,
							node.AdmiralExp,
							Escape(node.MvpShipName),
							Escape(node.DropShipName)));
					}
					break;

				case SortieCsvExportType.RedShelling:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("砲撃戦")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerIndex >= 7 && a.AttackerIndex <= 12))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(phase.PhaseName),
								Escape(atk.AttackerName),
								Escape(atk.DefenderName),
								atk.Damage,
								atk.IsCritical,
								Escape(atk.AttackType)));
						}
					}
					break;

				case SortieCsvExportType.RedNight:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("夜戦")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerIndex >= 7 && a.AttackerIndex <= 12))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(atk.AttackerName),
								Escape(atk.DefenderName),
								atk.Damage,
								atk.IsCritical,
								Escape(atk.AttackType)));
						}
					}
					break;

				case SortieCsvExportType.RedTorpedo:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("雷撃")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerName.Contains("敵")))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(phase.PhaseName),
								Escape(atk.DefenderName),
								atk.Damage));
						}
					}
					break;

				case SortieCsvExportType.RedAirBattle:
					foreach (var phase in node.Phases.Where(p => p.PhaseName.Contains("航空戦") || p.PhaseName.Contains("空襲")))
					{
						foreach (var atk in phase.Attacks.Where(a => a.AttackerName.Contains("敵")))
						{
							sb.AppendLine(string.Join(",",
								sortieId,
								Escape(area),
								node.CellId,
								Escape(node.CellDisplay),
								Escape(atk.DefenderName),
								atk.Damage));
						}
					}
					break;

				case SortieCsvExportType.SmokeScreen:
					if (node.IsBattleNode)
					{
						sb.AppendLine(string.Join(",",
							sortieId,
							Escape(area),
							node.CellId,
							Escape(node.CellDisplay),
							node.SmokeType,
							node.PlayerFormation,
							node.EnemyFormation,
							node.EngagementForm));
					}
					break;
			}
		}

		private static string Escape(string? s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			string str = s!;
			if (str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"))
			{
				return "\"" + str.Replace("\"", "\"\"") + "\"";
			}
			return str;
		}
	}
}
