using System;

namespace ElectronicObserver.Database.KancolleApi
{
	public class ApiFile
	{
		public int Id { get; set; }
		public ApiFileType ApiFileType { get; set; }
		public string Name { get; set; } = "";
		public string Content { get; set; } = "";
		public string ContentString { get => Content; set => Content = value; }
		public DateTime TimeStamp { get; set; }
		public int Version { get; set; }
		public int? SortieRecordId { get; set; }

		public override string ToString() => $"{Name} {ApiFileType}";
	}
}
