using System.IO;
using System.Runtime.Serialization;
using ElectronicObserver.Data;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	/// <summary>
	/// Guards the default values that the (de)serializer relies on: when an element
	/// is missing from Configuration.xml, DataContractSerializer keeps the value set
	/// by the constructor, so these defaults are effectively the upgrade behaviour.
	/// </summary>
	public class ConfigurationDefaultsTests
	{
		[Fact]
		public void ExpeditionCheck_HasExpectedDefaults()
		{
			var config = new Configuration.ConfigurationData.ConfigFormExpeditionCheck();

			Assert.True(config.AlertOnMissionScreenOpened);
			Assert.False(config.AlertSupplyDepleted);
			Assert.Equal(2, config.SelectedFleetId);
			Assert.Equal(0, config.SelectedTabIndex);
			Assert.Equal(new[] { 0, 0, 2, 5, 38 }, config.TargetMissions);
		}

		[Fact]
		public void Life_ChecksForUpdatesByDefault()
		{
			var config = new Configuration.ConfigurationData.ConfigLife();

			Assert.True(config.CheckUpdateInformation);
		}

		[Fact]
		public void ExpeditionCheck_DefaultsMatchSelectionHelper()
		{
			var config = new Configuration.ConfigurationData.ConfigFormExpeditionCheck();

			Assert.Equal(ExpeditionSelection.DefaultTargetMissions, config.TargetMissions);
			Assert.Equal(ExpeditionSelection.MinFleetId, config.SelectedFleetId);
		}

		[Fact]
		public void ExpeditionCheck_RoundTripsThroughDataContractSerializer()
		{
			var config = new Configuration.ConfigurationData.ConfigFormExpeditionCheck
			{
				AlertOnMissionScreenOpened = false,
				AlertSupplyDepleted = true,
				SelectedFleetId = 4,
				SelectedTabIndex = 1,
				TargetMissions = new[] { 0, 0, 7, 0, 123 },
			};

			var serializer = new DataContractSerializer(typeof(Configuration.ConfigurationData.ConfigFormExpeditionCheck));

			using var stream = new MemoryStream();
			serializer.WriteObject(stream, config);
			stream.Position = 0;
			var restored = (Configuration.ConfigurationData.ConfigFormExpeditionCheck)serializer.ReadObject(stream);

			Assert.False(restored.AlertOnMissionScreenOpened);
			Assert.True(restored.AlertSupplyDepleted);
			Assert.Equal(4, restored.SelectedFleetId);
			Assert.Equal(1, restored.SelectedTabIndex);
			Assert.Equal(new[] { 0, 0, 7, 0, 123 }, restored.TargetMissions);
		}
	}
}
