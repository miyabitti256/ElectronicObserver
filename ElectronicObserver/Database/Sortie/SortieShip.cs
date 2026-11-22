#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ElectronicObserver.Database.Sortie
{
	public class SortieShip
	{
		[JsonPropertyName("Id")]
		public int Id { get; set; }

		[JsonPropertyName("DropId")]
		public int? DropId { get; set; }

		[JsonPropertyName("Level")]
		public int Level { get; set; }

		[JsonPropertyName("Condition")]
		public int Condition { get; set; }

		[JsonPropertyName("Kyouka")]
		public List<int> Kyouka { get; set; } = new List<int>();

		[JsonPropertyName("Fuel")]
		public int Fuel { get; set; }

		[JsonPropertyName("Ammo")]
		public int Ammo { get; set; }

		[JsonPropertyName("Hp")]
		public int? Hp { get; set; }

		[JsonIgnore]
		public int MaxHp { get => Hp ?? 0; set => Hp = value; }

		[JsonPropertyName("Armor")]
		public int? Armor { get; set; }

		[JsonPropertyName("Evasion")]
		public int? Evasion { get; set; }

		[JsonPropertyName("Aircraft")]
		public List<int>? Aircraft { get; set; }

		[JsonPropertyName("Range")]
		public int Range { get; set; }

		[JsonPropertyName("Speed")]
		public int Speed { get; set; }

		[JsonPropertyName("Firepower")]
		public int? Firepower { get; set; }

		[JsonPropertyName("Torpedo")]
		public int? Torpedo { get; set; }

		[JsonPropertyName("Aa")]
		public int? Aa { get; set; }

		[JsonPropertyName("Asw")]
		public int? Asw { get; set; }

		[JsonPropertyName("Search")]
		public int? Search { get; set; }

		[JsonPropertyName("Luck")]
		public int? Luck { get; set; }

		[JsonIgnore]
		public int LuckTotal { get => Luck ?? 0; set => Luck = value; }

		[JsonPropertyName("EquipmentSlots")]
		public List<SortieEquipmentSlot> EquipmentSlots { get; set; } = new List<SortieEquipmentSlot>();

		[JsonPropertyName("ExpansionSlot")]
		public SortieEquipmentSlot? ExpansionSlot { get; set; }

		[JsonPropertyName("SpecialEffectItems")]
		public List<SpecialEffectItem>? SpecialEffectItems { get; set; }
	}
}
