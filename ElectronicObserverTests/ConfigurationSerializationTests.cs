using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// 設定ファイルの (非) 互換性を固定するテスト。
	/// Configuration.xml に、いまの型が知らない要素が残っていても読み込みが失敗しないことを確認します
	/// (設定項目を削ったり作り直したりしても、ユーザーの設定が読めなくならないようにするための保険)。
	/// </summary>
	public class ConfigurationSerializationTests
	{
		private const string UnknownElementName = "SettingThatNoLongerExists";

		[Fact]
		public void UnknownElement_IsIgnored()
		{
			var config = new Configuration.ConfigurationData.ConfigFormExpeditionCheck
			{
				AlertOnMissionScreenOpened = true,
				SelectedFleetId = 3,
				TargetMissions = new[] { 0, 0, 2, 5, 38 },
			};

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData.ConfigFormExpeditionCheck));

			string xml;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, config);
				xml = Encoding.UTF8.GetString(stream.ToArray());
			}

			int end = xml.LastIndexOf("</", System.StringComparison.Ordinal);
			Assert.True(end > 0, "closing tag not found; XML was:\r\n" + xml);
			string modified = xml.Substring(0, end)
				+ $"<{UnknownElementName}>42</{UnknownElementName}>"
				+ xml.Substring(end);
			Assert.True(xml != modified, "unknown element could not be injected; XML was:\r\n" + xml);

			Configuration.ConfigurationData.ConfigFormExpeditionCheck restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(modified)))
			{
				restored = (Configuration.ConfigurationData.ConfigFormExpeditionCheck)serializer.ReadObject(stream);
			}

			Assert.True(restored.AlertOnMissionScreenOpened);
			Assert.Equal(3, restored.SelectedFleetId);
			Assert.Equal(new[] { 0, 0, 2, 5, 38 }, restored.TargetMissions);
		}

		[Fact]
		public void ConfigConnection_RoundTrip_PreservesProperties()
		{
			// What: Verify ConfigConnection serializes and deserializes cleanly without data loss.
			var config = new Configuration.ConfigurationData.ConfigConnection
			{
				Port = 12345,
				SaveReceivedData = true,
				SaveDataPath = @"D:\EO_TestLogs",
				SaveRequest = true,
				SaveResponse = false,
				SaveOtherFile = true,
				ApplyVersion = true,
				RegisterAsSystemProxy = true,
				UseSystemProxy = false,
				UseUpstreamProxy = true,
				UpstreamProxyPort = 8080,
				UpstreamProxyAddress = "127.0.0.1"
			};

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData.ConfigConnection));
			string xml;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, config);
				xml = Encoding.UTF8.GetString(stream.ToArray());
			}

			Configuration.ConfigurationData.ConfigConnection restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
			{
				restored = (Configuration.ConfigurationData.ConfigConnection)serializer.ReadObject(stream);
			}

			Assert.NotNull(restored);
			Assert.Equal(config.Port, restored.Port);
			Assert.Equal(config.SaveReceivedData, restored.SaveReceivedData);
			Assert.Equal(config.SaveDataPath, restored.SaveDataPath);
			Assert.Equal(config.SaveRequest, restored.SaveRequest);
			Assert.Equal(config.SaveResponse, restored.SaveResponse);
			Assert.Equal(config.SaveOtherFile, restored.SaveOtherFile);
			Assert.Equal(config.ApplyVersion, restored.ApplyVersion);
			Assert.Equal(config.RegisterAsSystemProxy, restored.RegisterAsSystemProxy);
			Assert.Equal(config.UseSystemProxy, restored.UseSystemProxy);
			Assert.Equal(config.UseUpstreamProxy, restored.UseUpstreamProxy);
			Assert.Equal(config.UpstreamProxyPort, restored.UpstreamProxyPort);
			Assert.Equal(config.UpstreamProxyAddress, restored.UpstreamProxyAddress);
		}

		[Fact]
		public void ConfigFormFleet_RoundTrip_PreservesProperties()
		{
			// What: Verify ConfigFormFleet serializes and deserializes with custom display configurations.
			var config = new Configuration.ConfigurationData.ConfigFormFleet
			{
				ShowAircraft = false,
				SearchingAbilityMethod = 2,
				ShowNextExp = false,
				IsScrollable = true,
				FixShipNameWidth = true,
				ShortenHPBar = true,
				ShowConditionIcon = false
			};

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData.ConfigFormFleet));
			string xml;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, config);
				xml = Encoding.UTF8.GetString(stream.ToArray());
			}

			Configuration.ConfigurationData.ConfigFormFleet restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
			{
				restored = (Configuration.ConfigurationData.ConfigFormFleet)serializer.ReadObject(stream);
			}

			Assert.NotNull(restored);
			Assert.Equal(config.ShowAircraft, restored.ShowAircraft);
			Assert.Equal(config.SearchingAbilityMethod, restored.SearchingAbilityMethod);
			Assert.Equal(config.ShowNextExp, restored.ShowNextExp);
			Assert.Equal(config.IsScrollable, restored.IsScrollable);
			Assert.Equal(config.FixShipNameWidth, restored.FixShipNameWidth);
			Assert.Equal(config.ShortenHPBar, restored.ShortenHPBar);
			Assert.Equal(config.ShowConditionIcon, restored.ShowConditionIcon);
		}

		[Fact]
		public void ConfigUI_RoundTrip_PreservesProperties()
		{
			// What: Verify ConfigUI roundtrips theme and appearance flags without losing state.
			var config = new Configuration.ConfigurationData.ConfigUI
			{
				BarColorMorphing = false,
				IsLayoutFixed = true
			};

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData.ConfigUI));
			string xml;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, config);
				xml = Encoding.UTF8.GetString(stream.ToArray());
			}

			Configuration.ConfigurationData.ConfigUI restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
			{
				restored = (Configuration.ConfigurationData.ConfigUI)serializer.ReadObject(stream);
			}

			Assert.NotNull(restored);
			Assert.Equal(config.BarColorMorphing, restored.BarColorMorphing);
			Assert.Equal(config.IsLayoutFixed, restored.IsLayoutFixed);
		}

		[Fact]
		public void ConfigurationData_WholeRoundTrip_PreservesCustomValues()
		{
			// What: Verify full ConfigurationData tree roundtrips while preserving child section configs.
			var configData = new Configuration.ConfigurationData();
			configData.Initialize();
			configData.Connection.Port = 54321;
			configData.Connection.SaveReceivedData = true;
			configData.FormFleet.ShowAircraft = false;
			configData.UI.BarColorMorphing = false;

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData));
			string xml;
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, configData);
				xml = Encoding.UTF8.GetString(stream.ToArray());
			}

			Configuration.ConfigurationData restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(xml)))
			{
				restored = (Configuration.ConfigurationData)serializer.ReadObject(stream);
			}

			Assert.NotNull(restored);
			Assert.Equal(54321, restored.Connection.Port);
			Assert.True(restored.Connection.SaveReceivedData);
			Assert.False(restored.FormFleet.ShowAircraft);
			Assert.False(restored.UI.BarColorMorphing);
		}

		[Fact]
		public void ConfigurationData_MissingElements_FillsDefaultsSafely()
		{
			// What: Verify deserializing minimal or element-omitted XML activates OnDeserializing/Initialize()
			//       and populates all null/missing sections with standard default values.
			const string minimalXml = @"<Configuration xmlns=""http://schemas.datacontract.org/2004/07/ElectronicObserver.Utility"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">
	<Connection>
		<Port>49999</Port>
	</Connection>
</Configuration>";

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData));
			Configuration.ConfigurationData restored;
			using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(minimalXml)))
			{
				restored = (Configuration.ConfigurationData)serializer.ReadObject(stream);
			}

			Assert.NotNull(restored);
			// Specified in XML
			Assert.Equal(49999, restored.Connection.Port);
			// Omitted in XML under Connection -> should be default
			Assert.False(restored.Connection.SaveReceivedData);
			// Completely omitted sections -> should be initialized by Initialize() rather than remaining null
			Assert.NotNull(restored.FormFleet);
			Assert.True(restored.FormFleet.ShowAircraft);
			Assert.NotNull(restored.UI);
			Assert.NotNull(restored.UI.MainFont);
			Assert.NotNull(restored.FormQuest);
		}
	}
}
