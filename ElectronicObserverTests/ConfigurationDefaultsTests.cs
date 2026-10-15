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
		}
	}
}
