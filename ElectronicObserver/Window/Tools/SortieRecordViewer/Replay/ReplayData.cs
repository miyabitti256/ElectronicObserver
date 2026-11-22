#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElectronicObserver.Window.Tools.SortieRecordViewer.Replay
{
	public class ReplayData
	{
		[JsonPropertyName("id")]
		public int Id { get; set; } = 1;

		[JsonPropertyName("now_maphp")]
		public int NowMaphp { get; set; }

		[JsonPropertyName("max_maphp")]
		public int MaxMaphp { get; set; }

		[JsonPropertyName("defeat_count")]
		public int DefeatCount { get; set; }

		[JsonPropertyName("world")]
		public int World { get; set; }

		[JsonPropertyName("mapnum")]
		public int Mapnum { get; set; }

		[JsonPropertyName("fleetnum")]
		public int Fleetnum { get; set; }

		[JsonPropertyName("combined")]
		public int Combined { get; set; }

		[JsonPropertyName("fleet1")]
		public List<ReplayShip>? Fleet1 { get; set; }

		[JsonPropertyName("fleet2")]
		public List<ReplayShip>? Fleet2 { get; set; }

		[JsonPropertyName("fleet3")]
		public List<ReplayShip>? Fleet3 { get; set; }

		[JsonPropertyName("fleet4")]
		public List<ReplayShip>? Fleet4 { get; set; }

		[JsonPropertyName("support1")]
		public int Support1 { get; set; }

		[JsonPropertyName("support2")]
		public int Support2 { get; set; }

		[JsonPropertyName("lbas")]
		public List<ReplayAirBase>? AirBases { get; set; }

		[JsonPropertyName("time")]
		public long Time { get; set; }

		[JsonPropertyName("battles")]
		public List<ReplayBattle> Battles { get; set; } = new List<ReplayBattle>();
	}

	public class ReplayShip
	{
		[JsonPropertyName("mst_id")]
		public int ShipId { get; set; }

		[JsonPropertyName("level")]
		public int Level { get; set; }

		[JsonPropertyName("kyouka")]
		public List<int> Kyouka { get; set; } = new List<int>();

		[JsonPropertyName("morale")]
		public int Morale { get; set; }

		[JsonPropertyName("equip")]
		public List<int> Equip { get; set; } = new List<int>();

		[JsonPropertyName("stars")]
		public List<int> Stars { get; set; } = new List<int>();

		[JsonPropertyName("ace")]
		public List<int> Ace { get; set; } = new List<int>();
	}

	public class ReplayAirBase
	{
		[JsonPropertyName("rid")]
		public int Rid { get; set; }

		[JsonPropertyName("range")]
		public ReplayRange Range { get; set; } = new ReplayRange();

		[JsonPropertyName("action")]
		public int Action { get; set; }

		[JsonPropertyName("planes")]
		public List<ReplayAirBaseSquadron> Planes { get; set; } = new List<ReplayAirBaseSquadron>();
	}

	public class ReplayRange
	{
		[JsonPropertyName("api_base")]
		public int ApiBase { get; set; }

		[JsonPropertyName("api_bonus")]
		public int ApiBonus { get; set; }
	}

	public class ReplayAirBaseSquadron
	{
		[JsonPropertyName("mst_id")]
		public int EquipmentId { get; set; }

		[JsonPropertyName("count")]
		public int Count { get; set; }

		[JsonPropertyName("stars")]
		public int Stars { get; set; }

		[JsonPropertyName("ace")]
		public int Ace { get; set; }

		[JsonPropertyName("state")]
		public int State { get; set; }

		[JsonPropertyName("morale")]
		public int Morale { get; set; }
	}

	public class ReplayBattle
	{
		[JsonPropertyName("sortie_id")]
		public int SortieId { get; set; }

		[JsonPropertyName("node")]
		public int Node { get; set; }

		[JsonPropertyName("data")]
		public object? FirstBattle { get; set; }

		[JsonPropertyName("yasen")]
		public object? SecondBattle { get; set; }

		[JsonPropertyName("rating")]
		public string? Rating { get; set; }

		[JsonPropertyName("drop")]
		public int Drop { get; set; }

		[JsonPropertyName("time")]
		public double Time { get; set; }

		[JsonPropertyName("baseEXP")]
		public int BaseExp { get; set; }

		[JsonPropertyName("hqEXP")]
		public int HqExp { get; set; }

		[JsonPropertyName("mvp")]
		public List<int> Mvp { get; set; } = new List<int>();

		[JsonPropertyName("id")]
		public int Id { get; set; }
	}
}
