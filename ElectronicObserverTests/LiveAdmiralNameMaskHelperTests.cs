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
			// null 入力時に "null" 文字列が返されることを保証する
			string result = LiveAdmiralNameMaskHelper.EscapeJsString(null);

			Assert.Equal("null", result);
		}

		[Fact]
		public void EscapeJsString_EscapesSpecialCharactersAndHtmlTags()
		{
			// エスケープ対象文字および HTML script 終了タグが安全にエスケープされることを保証する
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

		[Fact]
		public void GenerateHookScript_AppendsEscapedMaskTextAsArgument()
		{
			// 特殊文字を含むマスク文字列がエスケープされて即時呼び出し引数になることを保証する
			string script = LiveAdmiralNameMaskHelper.GenerateHookScript("提督\"<special>");

			Assert.EndsWith("(\"提督\\\"\\x3cspecial>\");", script);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		public void GenerateHookScript_FallsBackToDefaultMaskText_WhenNullOrEmpty(string maskText)
		{
			// マスク文字列が null または空文字列の場合にデフォルトの「提督」が引数となることを保証する
			string script = LiveAdmiralNameMaskHelper.GenerateHookScript(maskText);

			Assert.EndsWith("(\"提督\");", script);
		}

		[Fact]
		public void InjectIntoHtml_InsertsScriptImmediatelyBeforeMainJsTag()
		{
			// 実際の index.php 同様に axios -> pixi -> main.js の並びにおいて main.js 直前にスクリプトが挿入されることを保証する
			string html = "<!DOCTYPE html><html><head><script src=\"js/axios.min.js\"></script><script src=\"js/pixi.min.js\"></script><script src=\"js/main.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, maskText: "司令官");

			Assert.Contains("</script><script>PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", modified);
			Assert.Contains("(\"司令官\");</script><script src=\"js/main.js\">", modified);
		}

		[Fact]
		public void InjectIntoHtml_InsertsScriptAfterPixiTag_WhenMainJsNotFound()
		{
			// main.js タグが存在しない場合、pixi.min.js タグの直後にスクリプトが挿入されることを保証する
			string html = "<html><head><script src=\"https://cdnjs.cloudflare.com/ajax/libs/pixi.js/4.8.8/pixi.min.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, maskText: "提督");

			Assert.Contains("pixi.min.js\"></script><script>", modified);
			Assert.Contains("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", modified);
			Assert.Contains("(\"提督\");</script></head>", modified);
		}

		[Fact]
		public void InjectIntoHtml_OmitsMaskHook_WhenMaskDisabled_ButIncludesPreserveDrawingBuffer()
		{
			// マスク無効時、フックスクリプトは挿入されず preserveDrawingBuffer のみが挿入されることを保証する
			string html = "<html><head><script src=\"js/main.js\"></script></head><body></body></html>";

			string modified = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: false, maskText: "提督");

			Assert.Equal("<html><head><script>PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;</script><script src=\"js/main.js\"></script></head><body></body></html>", modified);
		}

		[Fact]
		public void InjectIntoHtml_ReturnsUnchangedHtml_WhenBothOptionsDisabled()
		{
			// preserveDrawingBuffer もマスクも無効な場合、HTML が無改変で返されることを保証する
			string html = "<html><head><script src=\"js/main.js\"></script></head><body></body></html>";

			string result = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: false, maskEnabled: false, maskText: "提督");

			Assert.Equal(html, result);
		}

		[Fact]
		public void InjectIntoHtml_FallsBackToBeforeClosingHeadTag_WhenNeitherMainNorPixiTagExists()
		{
			// main.js も pixi.js も存在しない場合、</head> タグの直前にスクリプトが挿入されることを保証する
			string html = "<html><head><title>Test</title></head><body></body></html>";

			string result = LiveAdmiralNameMaskHelper.InjectIntoHtml(html, preserveDrawingBuffer: true, maskEnabled: true, maskText: "提督");

			Assert.Contains("<script>PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;", result);
			Assert.EndsWith("(\"提督\");</script></head><body></body></html>", result);
		}

		[Fact]
		public void ConfigFormBrowser_HasCorrectDefaultValues()
		{
			// ConfigFormBrowser の提督名マスク初期設定値が仕様通りであることを保証する
			var config = new Configuration.ConfigurationData.ConfigFormBrowser();

			Assert.False(config.LiveAdmiralNameMask);
			Assert.Equal("提督", config.LiveAdmiralNameMaskText);
		}

		[Fact]
		public void AdmiralData_AdmiralName_ReturnsEmptyString_WhenNotInitialized()
		{
			// 未初期化状態の AdmiralData において AdmiralName が空文字列であることを保証する
			var admiral = new ElectronicObserver.Data.AdmiralData();

			Assert.False(admiral.IsAvailable);
			Assert.Equal("", admiral.AdmiralName);
		}
	}
}
