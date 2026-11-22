#nullable enable

using System;

using System.Collections.Generic;

using System.Data;

using System.Data.SQLite;

using System.IO;

using System.Linq;

using System.Text;

using System.Text.Json;

using System.Threading.Tasks;

using ElectronicObserver.Database.KancolleApi;

using ElectronicObserver.Database.Sortie;



namespace ElectronicObserver.Database

{

	public class ElectronicObserverDbContext : IDisposable

	{

		public static string DefaultDbDirectory => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Record");

		public static string DefaultDbPath => Path.Combine(DefaultDbDirectory, "ElectronicObserver.sqlite");



		private readonly SQLiteConnection _connection;

		private readonly bool _ownsConnection;

		private readonly bool _isTempFile;

		private readonly string? _dbFilePath;



		private bool _isInitialized;

		private bool _disposed;



		public SortieRecordSet Sorties { get; }

		public ApiFileSet ApiFiles { get; }



		public SQLiteConnection Connection => _connection;



		public ElectronicObserverDbContext(bool inMemory = false)

		{

			if (inMemory)

			{

				_isTempFile = true;

				_dbFilePath = Path.Combine(Path.GetTempPath(), $"EO_Test_{Guid.NewGuid():N}.sqlite");

				string connStr = $"Data Source={_dbFilePath};Version=3;";

				_connection = new SQLiteConnection(connStr);

				_ownsConnection = true;

			}

			else

			{

				_dbFilePath = DefaultDbPath;

				string dir = Path.GetDirectoryName(_dbFilePath);

				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))

				{

					Directory.CreateDirectory(dir);

				}

				string connStr = $"Data Source={_dbFilePath};Version=3;";

				_connection = new SQLiteConnection(connStr);

				_ownsConnection = true;

			}



			_connection.Open();

			ConfigurePragmas();

			InitializeDatabase();



			Sorties = new SortieRecordSet(this);

			ApiFiles = new ApiFileSet(this);

		}



		public ElectronicObserverDbContext(string dbPathOrConnectionString)

		{

			if (string.IsNullOrWhiteSpace(dbPathOrConnectionString))

			{

				throw new ArgumentException("Database path or connection string cannot be empty", nameof(dbPathOrConnectionString));

			}



			string connStr;

			if (dbPathOrConnectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))

			{

				connStr = dbPathOrConnectionString;

			}

			else

			{

				_dbFilePath = dbPathOrConnectionString;

				string dir = Path.GetDirectoryName(_dbFilePath);

				if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))

				{

					Directory.CreateDirectory(dir);

				}

				connStr = $"Data Source={_dbFilePath};Version=3;";

			}



			_connection = new SQLiteConnection(connStr);

			_ownsConnection = true;

			_connection.Open();

			ConfigurePragmas();

			InitializeDatabase();



			Sorties = new SortieRecordSet(this);

			ApiFiles = new ApiFileSet(this);

		}



		public ElectronicObserverDbContext(SQLiteConnection connection, bool leaveOpen = false)

		{

			_connection = connection ?? throw new ArgumentNullException(nameof(connection));

			_ownsConnection = !leaveOpen;



			if (_connection.State != ConnectionState.Open)

			{

				_connection.Open();

			}



			ConfigurePragmas();

			InitializeDatabase();



			Sorties = new SortieRecordSet(this);

			ApiFiles = new ApiFileSet(this);

		}



		private void ConfigurePragmas()

		{

			using (var cmd = _connection.CreateCommand())

			{

				cmd.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL;";

				cmd.ExecuteNonQuery();

			}

		}



		public void InitializeDatabase()

		{

			if (_isInitialized) return;



			using (var cmd = _connection.CreateCommand())

			{

				cmd.CommandText = @"

CREATE TABLE IF NOT EXISTS Sorties (

    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    Version INTEGER NOT NULL,

    World INTEGER NOT NULL,

    Map INTEGER NOT NULL,

    FleetData TEXT NOT NULL,

    FleetAfterSortieData TEXT NULL,

    MapData TEXT NOT NULL,

    CalculatedSortieCost TEXT NULL

);



CREATE TABLE IF NOT EXISTS ApiFiles (

    Id INTEGER PRIMARY KEY AUTOINCREMENT,

    ApiFileType INTEGER NOT NULL,

    Name TEXT NOT NULL,

    Content BLOB NOT NULL,

    TimeStamp TEXT NOT NULL,

    Version INTEGER NOT NULL,

    SortieRecordId INTEGER NULL,

    FOREIGN KEY(SortieRecordId) REFERENCES Sorties(Id) ON DELETE SET NULL

);



CREATE INDEX IF NOT EXISTS IX_ApiFiles_SortieRecordId ON ApiFiles (SortieRecordId);

CREATE INDEX IF NOT EXISTS IX_ApiFiles_Name ON ApiFiles (Name);

";

				cmd.ExecuteNonQuery();

			}



			_isInitialized = true;

		}



		public int SaveChanges()

		{

			int affected = 0;

			using (var transaction = _connection.BeginTransaction())

			{

				affected += Sorties.CommitPending(transaction);

				affected += ApiFiles.CommitPending(transaction);

				transaction.Commit();

			}

			return affected;

		}



		public Task<int> SaveChangesAsync()

		{

			return Task.FromResult(SaveChanges());

		}



		public static byte[] CompressContent(string text)

		{

			if (string.IsNullOrEmpty(text))

			{

				return new byte[0];

			}



			byte[] input = Encoding.UTF8.GetBytes(text);

			return BrotliSharpLib.Brotli.CompressBuffer(input, 0, input.Length);

		}



		public static string DecompressContent(byte[] blob)

		{

			if (blob == null || blob.Length == 0)

			{

				return string.Empty;

			}



			byte[] decompressed = BrotliSharpLib.Brotli.DecompressBuffer(blob, 0, blob.Length);

			return Encoding.UTF8.GetString(decompressed);

		}



		public void Dispose()

		{

			if (_disposed) return;

			_disposed = true;



			if (_ownsConnection)

			{

				_connection?.Close();

				_connection?.Dispose();

			}



			if (_isTempFile && !string.IsNullOrEmpty(_dbFilePath) && File.Exists(_dbFilePath))

			{

				try

				{

					File.Delete(_dbFilePath);

				}

				catch

				{

					// ignored

				}

			}

		}

	}



	public class SortieRecordSet

	{

		private readonly ElectronicObserverDbContext _context;

		private readonly List<SortieRecord> _added = new List<SortieRecord>();

		private readonly List<SortieRecord> _updated = new List<SortieRecord>();



		public SortieRecordSet(ElectronicObserverDbContext context)

		{

			_context = context;

		}



		public void Add(SortieRecord record)

		{

			if (record != null && !_added.Contains(record))

			{

				_added.Add(record);

			}

		}



		public Task AddAsync(SortieRecord record)

		{

			Add(record);

			return Task.CompletedTask;

		}



		public void Update(SortieRecord record)

		{

			if (record != null && !_updated.Contains(record) && !_added.Contains(record))

			{

				_updated.Add(record);

			}

		}



		public Task UpdateAsync(SortieRecord record)

		{

			Update(record);

			return Task.CompletedTask;

		}



		internal int CommitPending(SQLiteTransaction transaction)

		{

			int affected = 0;

			var conn = _context.Connection;



			foreach (var record in _added)

			{

				using (var cmd = conn.CreateCommand())

				{

					cmd.Transaction = transaction;

					cmd.CommandText = @"

INSERT INTO Sorties (Version, World, Map, FleetData, FleetAfterSortieData, MapData, CalculatedSortieCost)

VALUES (@Version, @World, @Map, @FleetData, @FleetAfterSortieData, @MapData, @CalculatedSortieCost);

SELECT last_insert_rowid();";



					cmd.Parameters.AddWithValue("@Version", record.Version);

					cmd.Parameters.AddWithValue("@World", record.World);

					cmd.Parameters.AddWithValue("@Map", record.Map);

					cmd.Parameters.AddWithValue("@FleetData", JsonSerializer.Serialize(record.FleetData ?? new SortieFleetData()));

					cmd.Parameters.AddWithValue("@FleetAfterSortieData", record.FleetAfterSortieData != null ? JsonSerializer.Serialize(record.FleetAfterSortieData) : (object)DBNull.Value);

					cmd.Parameters.AddWithValue("@MapData", JsonSerializer.Serialize(record.MapData ?? new SortieMapData()));

					cmd.Parameters.AddWithValue("@CalculatedSortieCost", record.CalculatedSortieCost != null ? JsonSerializer.Serialize(record.CalculatedSortieCost) : (object)DBNull.Value);



					object result = cmd.ExecuteScalar();

					record.Id = Convert.ToInt32(result);

					affected++;

				}



				if (record.ApiFiles != null && record.ApiFiles.Count > 0)

				{

					foreach (var file in record.ApiFiles)

					{

						file.SortieRecordId = record.Id;

						_context.ApiFiles.Add(file);

					}

				}

			}

			_added.Clear();



			foreach (var record in _updated)

			{

				using (var cmd = conn.CreateCommand())

				{

					cmd.Transaction = transaction;

					cmd.CommandText = @"

UPDATE Sorties

SET Version = @Version,

    World = @World,

    Map = @Map,

    FleetData = @FleetData,

    FleetAfterSortieData = @FleetAfterSortieData,

    MapData = @MapData,

    CalculatedSortieCost = @CalculatedSortieCost

WHERE Id = @Id;";



					cmd.Parameters.AddWithValue("@Id", record.Id);

					cmd.Parameters.AddWithValue("@Version", record.Version);

					cmd.Parameters.AddWithValue("@World", record.World);

					cmd.Parameters.AddWithValue("@Map", record.Map);

					cmd.Parameters.AddWithValue("@FleetData", JsonSerializer.Serialize(record.FleetData ?? new SortieFleetData()));

					cmd.Parameters.AddWithValue("@FleetAfterSortieData", record.FleetAfterSortieData != null ? JsonSerializer.Serialize(record.FleetAfterSortieData) : (object)DBNull.Value);

					cmd.Parameters.AddWithValue("@MapData", JsonSerializer.Serialize(record.MapData ?? new SortieMapData()));

					cmd.Parameters.AddWithValue("@CalculatedSortieCost", record.CalculatedSortieCost != null ? JsonSerializer.Serialize(record.CalculatedSortieCost) : (object)DBNull.Value);



					affected += cmd.ExecuteNonQuery();

				}

			}

			_updated.Clear();



			return affected;

		}



		public SortieRecord? Find(int id, bool includeApiFiles = true)

		{

			var conn = _context.Connection;

			SortieRecord? record = null;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = "SELECT * FROM Sorties WHERE Id = @Id LIMIT 1;";

				cmd.Parameters.AddWithValue("@Id", id);



				using (var reader = cmd.ExecuteReader())

				{

					if (reader.Read())

					{

						record = MapSortieRecord(reader);

					}

				}

			}



			if (record != null && includeApiFiles)

			{

				record.ApiFiles = _context.ApiFiles.GetBySortieId(record.Id);

			}



			return record;

		}



		public Task<SortieRecord?> FindAsync(int id, bool includeApiFiles = true)

		{

			return Task.FromResult(Find(id, includeApiFiles));

		}



		public SortieRecord? FirstOrDefault(Func<SortieRecord, bool> predicate, bool includeApiFiles = true)

		{

			var all = ToList(includeApiFiles);

			return all.FirstOrDefault(predicate);

		}



		public Task<SortieRecord?> FirstOrDefaultAsync(Func<SortieRecord, bool> predicate, bool includeApiFiles = true)

		{

			return Task.FromResult(FirstOrDefault(predicate, includeApiFiles));

		}



		public List<SortieRecord> ToList(bool includeApiFiles = false)

		{

			var list = new List<SortieRecord>();

			var conn = _context.Connection;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = "SELECT * FROM Sorties ORDER BY Id;";

				using (var reader = cmd.ExecuteReader())

				{

					while (reader.Read())

					{

						list.Add(MapSortieRecord(reader));

					}

				}

			}



			if (includeApiFiles)

			{

				foreach (var record in list)

				{

					record.ApiFiles = _context.ApiFiles.GetBySortieId(record.Id);

				}

			}



			return list;

		}



		public Task<List<SortieRecord>> ToListAsync(bool includeApiFiles = false)

		{

			return Task.FromResult(ToList(includeApiFiles));

		}



		private static SortieRecord MapSortieRecord(SQLiteDataReader reader)

		{

			var record = new SortieRecord

			{

				Id = Convert.ToInt32(reader["Id"]),

				Version = Convert.ToInt32(reader["Version"]),

				World = Convert.ToInt32(reader["World"]),

				Map = Convert.ToInt32(reader["Map"]),

			};



			string? fleetJson = reader["FleetData"] as string;

			if (!string.IsNullOrEmpty(fleetJson))

			{

				record.FleetData = JsonSerializer.Deserialize<SortieFleetData>(fleetJson!) ?? new SortieFleetData();

			}



			if (reader["FleetAfterSortieData"] != DBNull.Value)

			{

				string? fleetAfterJson = reader["FleetAfterSortieData"] as string;

				if (!string.IsNullOrEmpty(fleetAfterJson))

				{

					record.FleetAfterSortieData = JsonSerializer.Deserialize<SortieFleetData>(fleetAfterJson!);

				}

			}



			string? mapJson = reader["MapData"] as string;

			if (!string.IsNullOrEmpty(mapJson))

			{

				record.MapData = JsonSerializer.Deserialize<SortieMapData>(mapJson!) ?? new SortieMapData();

			}



			if (reader["CalculatedSortieCost"] != DBNull.Value)

			{

				string? costJson = reader["CalculatedSortieCost"] as string;

				if (!string.IsNullOrEmpty(costJson))

				{

					record.CalculatedSortieCost = JsonSerializer.Deserialize<CalculatedSortieCost>(costJson!);

				}

			}



			return record;

		}

	}



	public class ApiFileSet

	{

		private readonly ElectronicObserverDbContext _context;

		private readonly List<ApiFile> _added = new List<ApiFile>();



		public ApiFileSet(ElectronicObserverDbContext context)

		{

			_context = context;

		}



		public void Add(ApiFile file)

		{

			if (file != null && !_added.Contains(file))

			{

				_added.Add(file);

			}

		}



		public Task AddAsync(ApiFile file)

		{

			Add(file);

			return Task.CompletedTask;

		}



		public void AddRange(IEnumerable<ApiFile> files)

		{

			if (files != null)

			{

				foreach (var file in files)

				{

					Add(file);

				}

			}

		}



		public Task AddRangeAsync(IEnumerable<ApiFile> files)

		{

			AddRange(files);

			return Task.CompletedTask;

		}



		internal int CommitPending(SQLiteTransaction transaction)

		{

			int affected = 0;

			var conn = _context.Connection;



			foreach (var file in _added)

			{

				using (var cmd = conn.CreateCommand())

				{

					cmd.Transaction = transaction;

					cmd.CommandText = @"

INSERT INTO ApiFiles (ApiFileType, Name, Content, TimeStamp, Version, SortieRecordId)

VALUES (@ApiFileType, @Name, @Content, @TimeStamp, @Version, @SortieRecordId);

SELECT last_insert_rowid();";



					cmd.Parameters.AddWithValue("@ApiFileType", (int)file.ApiFileType);

					cmd.Parameters.AddWithValue("@Name", file.Name ?? "");

					cmd.Parameters.Add("@Content", DbType.Binary).Value = ElectronicObserverDbContext.CompressContent(file.Content);

					cmd.Parameters.AddWithValue("@TimeStamp", file.TimeStamp.ToString("o"));

					cmd.Parameters.AddWithValue("@Version", file.Version);

					cmd.Parameters.AddWithValue("@SortieRecordId", file.SortieRecordId.HasValue ? (object)file.SortieRecordId.Value : DBNull.Value);



					object result = cmd.ExecuteScalar();

					file.Id = Convert.ToInt32(result);

					affected++;

				}

			}

			_added.Clear();



			return affected;

		}



		public List<ApiFile> GetBySortieId(int sortieId)

		{

			var list = new List<ApiFile>();

			var conn = _context.Connection;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = "SELECT * FROM ApiFiles WHERE SortieRecordId = @SortieRecordId ORDER BY Id;";

				cmd.Parameters.AddWithValue("@SortieRecordId", sortieId);



				using (var reader = cmd.ExecuteReader())

				{

					while (reader.Read())

					{

						list.Add(MapApiFile(reader));

					}

				}

			}



			return list;

		}



		public Task<List<ApiFile>> GetBySortieIdAsync(int sortieId)

		{

			return Task.FromResult(GetBySortieId(sortieId));

		}



		public List<ApiFile> GetExpeditionApis()

		{

			var list = new List<ApiFile>();

			var conn = _context.Connection;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = @"

SELECT * FROM ApiFiles

WHERE Name IN (

    'api_start2/getData',

    'api_port/port',

    'api_get_member/ship2',

    'api_get_member/slot_item',

    'api_req_mission/start',

    'api_req_mission/result',

    'api_req_mission/return_instruction'

)

ORDER BY Id;";



				using (var reader = cmd.ExecuteReader())

				{

					while (reader.Read())

					{

						list.Add(MapApiFile(reader));

					}

				}

			}



			return list;

		}



		public Task<List<ApiFile>> GetExpeditionApisAsync()

		{

			return Task.FromResult(GetExpeditionApis());

		}



		public List<ApiFile> ToList()

		{

			var list = new List<ApiFile>();

			var conn = _context.Connection;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = "SELECT * FROM ApiFiles ORDER BY Id;";

				using (var reader = cmd.ExecuteReader())

				{

					while (reader.Read())

					{

						list.Add(MapApiFile(reader));

					}

				}

			}



			return list;

		}



		public Task<List<ApiFile>> ToListAsync()

		{

			return Task.FromResult(ToList());

		}



		public List<ApiFile> GetByName(string name)

		{

			var list = new List<ApiFile>();

			var conn = _context.Connection;



			using (var cmd = conn.CreateCommand())

			{

				cmd.CommandText = "SELECT * FROM ApiFiles WHERE Name = @Name ORDER BY Id;";

				cmd.Parameters.AddWithValue("@Name", name ?? "");



				using (var reader = cmd.ExecuteReader())

				{

					while (reader.Read())

					{

						list.Add(MapApiFile(reader));

					}

				}

			}



			return list;

		}



		public Task<List<ApiFile>> GetByNameAsync(string name)

		{

			return Task.FromResult(GetByName(name));

		}



		private static ApiFile MapApiFile(SQLiteDataReader reader)

		{

			var file = new ApiFile

			{

				Id = Convert.ToInt32(reader["Id"]),

				ApiFileType = (ApiFileType)Convert.ToInt32(reader["ApiFileType"]),

				Name = reader["Name"] as string ?? "",

				Version = Convert.ToInt32(reader["Version"]),

			};



			if (reader["TimeStamp"] != DBNull.Value)

			{

				string? timeStr = reader["TimeStamp"] as string;

				if (!string.IsNullOrEmpty(timeStr) && DateTime.TryParse(timeStr, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt))

				{

					file.TimeStamp = dt;

				}

				else

				{

					DateTime.TryParse(timeStr, out dt);

					file.TimeStamp = dt;

				}

			}



			if (reader["SortieRecordId"] != DBNull.Value)

			{

				file.SortieRecordId = Convert.ToInt32(reader["SortieRecordId"]);

			}



			byte[]? blob = reader["Content"] as byte[];

			file.Content = ElectronicObserverDbContext.DecompressContent(blob ?? new byte[0]);



			return file;

		}

	}

}

