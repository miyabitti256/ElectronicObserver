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
	}
}
