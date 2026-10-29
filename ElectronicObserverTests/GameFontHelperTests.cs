using System;
using BrowserLib;
using Xunit;

namespace ElectronicObserverTests
{
	public class GameFontHelperTests
	{
		[Fact]
		public void SanitizeFontName_RemovesDangerousCharacters()
		{
			// What: クォートや波括弧、改行等の危険文字がサニタイズされること
			Assert.Equal("", GameFontHelper.SanitizeFontName(null));
			Assert.Equal("", GameFontHelper.SanitizeFontName("   "));
			Assert.Equal("Noto Sans JP", GameFontHelper.SanitizeFontName("Noto Sans JP"));
			Assert.Equal("Noto Sans JP", GameFontHelper.SanitizeFontName("Noto \"Sans\" JP;"));
			Assert.Equal("Roboto", GameFontHelper.SanitizeFontName("Roboto\r\n{color:red}"));
		}

		[Fact]
		public void GenerateFontCss_ReturnsNullWhenAllParametersEmpty()
		{
			// What: すべての指定が空の場合、nullを返すこと
			string css = GameFontHelper.GenerateFontCss(null, null, null);
			Assert.Null(css);

			css = GameFontHelper.GenerateFontCss("", "   ", "");
			Assert.Null(css);
		}

		[Fact]
		public void GenerateFontCss_GeneratesJapaneseOnlyRules()
		{
			// What: 和文フォントのみ指定した場合、通常・太字のfont_jが定義されunicode-rangeは含まれないこと
			string css = GameFontHelper.GenerateFontCss("Noto Sans JP", null, null);

			Assert.NotNull(css);
			Assert.Contains("font-family: \"font_j\";", css);
			Assert.Contains("local(\"Noto Sans JP\")", css);
			Assert.Contains("font-weight: bold;", css);
			Assert.Contains("local(\"Noto Sans JP Bold\")", css);
			Assert.DoesNotContain("unicode-range", css);
		}

		[Fact]
		public void GenerateFontCss_NotoSansJp_UsesValidCjkFallbackWithoutDuplicateJp()
		{
			// What: Noto Sans JP指定時に不正な「Noto Sans JP CJK JP」が生成されず、正しい「Noto Sans CJK JP」フォールバックが生成されること
			string css = GameFontHelper.GenerateFontCss("Noto Sans JP", null, null);

			Assert.NotNull(css);
			Assert.Contains("local(\"Noto Sans JP\")", css);
			Assert.Contains("local(\"Noto Sans JP Regular\")", css);
			Assert.Contains("local(\"Noto Sans CJK JP\")", css);
			Assert.DoesNotContain("Noto Sans JP CJK JP", css);
		}

		[Fact]
		public void GenerateFontCss_NotoSansCjkJp_DoesNotDuplicateCjkPrefix()
		{
			// What: 最初から「Noto Sans CJK JP」が指定された場合に「CJK CJK JP」のような重複が起きないこと
			string css = GameFontHelper.GenerateFontCss("Noto Sans CJK JP", null, null);

			Assert.NotNull(css);
			Assert.Contains("local(\"Noto Sans CJK JP\")", css);
			Assert.DoesNotContain("CJK CJK", css);
			Assert.DoesNotContain("JP CJK JP", css);
		}

		[Fact]
		public void GenerateFontCss_NonNotoJapaneseFont_DoesNotAppendBogusCjkJp()
		{
			// What: Noto以外の和文フォント指定時に無関係な「CJK JP」サフィックスが付加されないこと
			string css = GameFontHelper.GenerateFontCss("Meiryo", null, null);

			Assert.NotNull(css);
			Assert.Contains("local(\"Meiryo\")", css);
			Assert.Contains("local(\"Meiryo Regular\")", css);
			Assert.DoesNotContain("CJK JP", css);
		}

		[Fact]
		public void GenerateFontCss_GeneratesLatinOnlyRules()
		{
			// What: 欧文フォントのみ指定した場合、通常・太字のfont_j（単一ウェイト向けフォールバック含む）とunicode-rangeが含まれること
			string css = GameFontHelper.GenerateFontCss(null, "Roboto", null);

			Assert.NotNull(css);
			Assert.Contains("font-family: \"font_j\";", css);
			Assert.Contains("local(\"Roboto\")", css);
			Assert.Contains("font-weight: bold;", css);
			Assert.Contains("local(\"Roboto Bold\"), local(\"Roboto-Bold\"), local(\"Roboto\")", css);
			Assert.Contains("unicode-range: U+0020-007E, U+00A0-00FF;", css);
		}

		[Fact]
		public void GenerateFontCss_MixesFontsWithCorrectEvaluationOrder()
		{
			// What: 和文・欧文の混植時、和文が先・欧文が後に定義され、欧文にunicode-rangeが付与されること
			string css = GameFontHelper.GenerateFontCss("Noto Sans JP", "Roboto", null);

			Assert.NotNull(css);

			int jpIndex = css.IndexOf("Noto Sans JP", StringComparison.Ordinal);
			int latinIndex = css.IndexOf("Roboto", StringComparison.Ordinal);

			Assert.True(jpIndex >= 0);
			Assert.True(latinIndex >= 0);
			Assert.True(jpIndex < latinIndex);

			Assert.Contains("unicode-range: U+0020-007E, U+00A0-00FF;", css);
		}

		[Fact]
		public void GenerateFontCss_AppendsCustomCss()
		{
			// What: カスタムCSSが指定された場合、自動生成定義の末尾に付加されること
			string customCss = "/* custom test */\n.CanvasArea { opacity: 0.9; }";
			string css = GameFontHelper.GenerateFontCss("BIZ UDPGothic", "Segoe UI", customCss);

			Assert.NotNull(css);
			Assert.Contains("BIZ UDPGothic", css);
			Assert.Contains("Segoe UI", css);
			Assert.EndsWith(customCss, css);
		}

		[Fact]
		public void GenerateFontCss_CustomCssOnly()
		{
			// What: カスタムCSSのみ指定された場合、そのカスタムCSSがそのまま返ること
			string customCss = "@font-face { font-family: \"font_j\"; src: local(\"Meiryo\"); }";
			string css = GameFontHelper.GenerateFontCss(null, null, customCss);

			Assert.Equal(customCss, css);
		}

		[Fact]
		public void GenerateInjectionScript_EscapesCssAndPreventsScriptTagBreakout()
		{
			// What: 動的注入用JSが生成され、カスタムCSS内に</script>が含まれていても安全にエスケープされること
			string css = "@font-face { font-family: \"font_j\"; } /* </script> */";
			string script = GameFontHelper.GenerateInjectionScript(css);

			Assert.Contains("eo-custom-game-font", script);
			Assert.Contains("font_j", script);
			Assert.DoesNotContain("</script>", script);
			Assert.Contains("\\x3c/script>", script);
		}

		[Theory]
		[InlineData(null)]
		[InlineData("")]
		[InlineData("   ")]
		public void GenerateInjectionScript_PassesNullWhenCssIsEmpty(string emptyCss)
		{
			// What: CSSが空またはnullの場合、既存style削除のために引数nullで呼び出すスクリプトが生成されること
			string script = GameFontHelper.GenerateInjectionScript(emptyCss);

			Assert.EndsWith("(null);", script);
		}

		[Fact]
		public void InjectIntoHtml_InsertsDynamicScriptImmediatelyBeforeMainJs()
		{
			// What: index.php内にmain.jsが存在する場合、main.js実行直前に動的DOM注入<script>が挿入されること
			string html = "<!DOCTYPE html><html><head></head><body><script src=\"js/pixi.min.js\"></script><script src=\"js/main.js\"></script></body></html>";
			string css = GameFontHelper.GenerateFontCss("Noto Sans JP", "Roboto", null);

			string modified = GameFontHelper.InjectIntoHtml(html, css);

			int scriptIdx = modified.IndexOf("<script>", StringComparison.Ordinal);
			int mainIdx = modified.IndexOf("<script src=\"js/main.js\">", StringComparison.Ordinal);
			Assert.True(scriptIdx >= 0);
			Assert.True(scriptIdx < mainIdx);
			Assert.Contains("eo-custom-game-font", modified);
		}

		[Fact]
		public void InjectIntoHtml_FallbacksToClosingHeadOrOpeningBody()
		{
			// What: main.jsやpixi.jsが存在しない場合、</head>または<body>の直前にスクリプトが挿入されること
			string css = "@font-face { font-family: \"font_j\"; }";

			string withHead = "<html><head><title>Test</title></head><body>Content</body></html>";
			string modHead = GameFontHelper.InjectIntoHtml(withHead, css);
			Assert.True(modHead.IndexOf("<script>", StringComparison.Ordinal) < modHead.IndexOf("</head>", StringComparison.Ordinal));

			string withoutHead = "<html><body>Content</body></html>";
			string modBody = GameFontHelper.InjectIntoHtml(withoutHead, css);
			Assert.True(modBody.IndexOf("<script>", StringComparison.Ordinal) < modBody.IndexOf("<body", StringComparison.Ordinal));
		}

		[Fact]
		public void InjectIntoHtml_ReturnsOriginalHtmlWhenCssOrHtmlEmpty()
		{
			// What: htmlまたはcssがnullまたは空白文字の場合、元のhtmlを変更せずそのまま返すこと
			string html = "<html><body>Content</body></html>";

			Assert.Equal(html, GameFontHelper.InjectIntoHtml(html, null));
			Assert.Equal(html, GameFontHelper.InjectIntoHtml(html, ""));
			Assert.Equal(html, GameFontHelper.InjectIntoHtml(html, "   "));
			Assert.Null(GameFontHelper.InjectIntoHtml(null, "css"));
		}
	}
}
