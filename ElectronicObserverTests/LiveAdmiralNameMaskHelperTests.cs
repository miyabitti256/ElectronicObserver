using BrowserLib;
using ElectronicObserver.Utility;
using Xunit;

namespace ElectronicObserverTests
{
	public class LiveAdmiralNameMaskHelperTests
	{
		[Fact]
		public void EscapeJsString_HandlesNull()
		{
			string result = LiveAdmiralNameMaskHelper.EscapeJsString(null);

			Assert.Equal("null", result);
		}

		[Fact]
		public void EscapeJsString_EscapesSpecialCharactersAndHtmlTags()
		{
			string input = "提督\"'\\</script>\r\n\t";
			string escaped = LiveAdmiralNameMaskHelper.EscapeJsString(input);

			Assert.StartsWith("\"", escaped);
			Assert.EndsWith("\"", escaped);
			Assert.Contains("\\\"", escaped);
			Assert.Contains("\\\\", escaped);
			Assert.Contains("\\x3c/script>", escaped);
			Assert.DoesNotContain("</script>", escaped);
			Assert.Contains("\\r\\n\\t", escaped);
		}

		[Theory]
		[InlineData(true, "吹雪提督", "司令官")]
		[InlineData(false, "大和", "提督")]
		public void GenerateHookScript_ContainsExpectedConfigurationAndHooks(bool enabled, string target, string mask)
		{
			string script = LiveAdmiralNameMaskHelper.GenerateHookScript(enabled, target, mask);

			Assert.Contains($"enabled: {(enabled ? "true" : "false")}", script);
			Assert.Contains($"targetName: \"{target}\"", script);
			Assert.Contains($"maskText: \"{mask}\"", script);
			Assert.Contains("CanvasRenderingContext2D.prototype.fillText", script);
			Assert.Contains("CanvasRenderingContext2D.prototype.strokeText", script);
			Assert.Contains("CanvasRenderingContext2D.prototype.measureText", script);
			Assert.Contains("PIXI.Text.prototype", script);
			Assert.Contains("PIXI.Text.prototype._renderWebGL", script);
			Assert.Contains("window.__setAdmiralMask", script);
		}

		[Fact]
		public void GenerateHookScript_FallsBackToDefaultMaskText_WhenEmpty()
		{
			string script = LiveAdmiralNameMaskHelper.GenerateHookScript(true, "テスト提督", "");

			Assert.Contains("maskText: \"提督\"", script);
		}

		[Fact]
		public void GenerateUpdateScript_ProducesValidCallWithSelfInitialization()
		{
			string script = LiveAdmiralNameMaskHelper.GenerateUpdateScript(true, "吹雪", "司令官");

			Assert.Contains("if (typeof window.__setAdmiralMask === 'undefined')", script);
			Assert.Contains("window.__setAdmiralMask(true, \"吹雪\", \"司令官\");", script);
		}

		[Fact]
		public void InjectIntoHtml_InsertsScriptAfterPixiMinJs()
		{
			string html = "<html><head><script src=\"https://cdnjs.cloudflare.com/ajax/libs/pixi.js/4.8.8/pixi.min.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, targetName: "提督A", maskText: "司令官");

			Assert.Contains("pixi.min.js\"></script><script>", modified);
			Assert.Contains("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", modified);
			Assert.Contains("window.__eoAdmiralMask", modified);
			Assert.Contains("司令官", modified);
		}

		[Fact]
		public void InjectIntoHtml_InsertsScriptAfterStandardPixiJs()
		{
			string html = "<html><head><script src=\"/kcs2/js/pixi.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, targetName: "提督A", maskText: "司令官");

			Assert.Contains("/pixi.js\"></script><script>", modified);
			Assert.Contains("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", modified);
			Assert.Contains("window.__eoAdmiralMask", modified);
			Assert.Contains("司令官", modified);
		}

		[Fact]
		public void InjectIntoHtml_OmitsPreserveDrawingBuffer_WhenDisabled()
		{
			string html = "<html><head><script src=\"https://cdnjs.cloudflare.com/ajax/libs/pixi.js/4.8.8/pixi.min.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: false, maskEnabled: true, targetName: "提督A", maskText: "提督");

			Assert.DoesNotContain("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", modified);
			Assert.Contains("window.__eoAdmiralMask", modified);
		}

		[Fact]
		public void InjectIntoHtml_FallsBackToBeforeHead_WhenPixiTagNotFound()
		{
			string html = "<html><head></head><body>No Pixi Here</body></html>";

			string result = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, targetName: "提督", maskText: "提督");

			Assert.Contains("<script>", result);
			Assert.Contains("</head>", result);
		}

		[Fact]
		public void ConfigFormBrowser_HasCorrectDefaultValues()
		{
			var config = new Configuration.ConfigurationData.ConfigFormBrowser();

			Assert.False(config.LiveAdmiralNameMask);
			Assert.Equal("提督", config.LiveAdmiralNameMaskText);
			Assert.Equal("", config.LiveAdmiralNameCustomTarget);
		}

		[Fact]
		public void AdmiralData_AdmiralName_ReturnsEmptyString_WhenNotInitialized()
		{
			var admiral = new ElectronicObserver.Data.AdmiralData();

			Assert.False(admiral.IsAvailable);
			Assert.Equal("", admiral.AdmiralName);
		}
	}
}
