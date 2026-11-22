using System.Data.SQLite;

namespace ElectronicObserver.Database
{
	/// <summary>
	/// SortieRecordDatabase is an alias / wrapper of ElectronicObserverDbContext.
	/// </summary>
	public class SortieRecordDatabase : ElectronicObserverDbContext
	{
		public SortieRecordDatabase(bool inMemory = false) : base(inMemory)
		{
		}

		public SortieRecordDatabase(string dbPathOrConnectionString) : base(dbPathOrConnectionString)
		{
		}

		public SortieRecordDatabase(SQLiteConnection connection, bool leaveOpen = false) : base(connection, leaveOpen)
		{
		}
	}
}
