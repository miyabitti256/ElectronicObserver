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
		public void GenerateFontCss_GeneratesLatinOnlyRules()
		{
			// What: 欧文フォントのみ指定した場合、通常・太字のfont_jとunicode-rangeが含まれること
			string css = GameFontHelper.GenerateFontCss(null, "Roboto", null);

			Assert.NotNull(css);
			Assert.Contains("font-family: \"font_j\";", css);
			Assert.Contains("local(\"Roboto\")", css);
			Assert.Contains("font-weight: bold;", css);
			Assert.Contains("local(\"Roboto Bold\")", css);
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
			// CSSの@font-faceは後から定義されたものが同一ファミリー内で優先マッチするため、和文が先、欧文が後
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
		public void InjectCssIntoHtml_InjectsBeforeClosingHeadTag()
		{
			// What: </head>の直前に<style id="eo-custom-game-font">が挿入されること
			string html = "<html><head><title>Test</title></head><body>Content</body></html>";
			string css = "@font-face { font-family: \"font_j\"; }";

			string modified = GameFontHelper.InjectCssIntoHtml(html, css);

			Assert.Contains("<style id=\"eo-custom-game-font\">", modified);
			Assert.Contains(css, modified);
			Assert.True(modified.IndexOf("<style id=\"eo-custom-game-font\">", StringComparison.Ordinal) < modified.IndexOf("</head>", StringComparison.Ordinal));
		}

		[Fact]
		public void InjectCssIntoHtml_FallbackToBeforeBodyWhenNoHead()
		{
			// What: </head>が存在しない場合、<body>タグの直前に挿入されること
			string html = "<html><body>Content</body></html>";
			string css = "@font-face { font-family: \"font_j\"; }";

			string modified = GameFontHelper.InjectCssIntoHtml(html, css);

			Assert.Contains("<style id=\"eo-custom-game-font\">", modified);
			Assert.True(modified.IndexOf("<style id=\"eo-custom-game-font\">", StringComparison.Ordinal) < modified.IndexOf("<body", StringComparison.Ordinal));
		}

		[Fact]
		public void InjectCssIntoHtml_ReturnsOriginalHtmlWhenCssOrHtmlEmpty()
		{
			// What: htmlまたはcssがnullまたは空文字の場合、元のhtmlを変更せずそのまま返すこと
			string html = "<html><body>Content</body></html>";

			Assert.Equal(html, GameFontHelper.InjectCssIntoHtml(html, null));
			Assert.Equal(html, GameFontHelper.InjectCssIntoHtml(html, ""));
			Assert.Null(GameFontHelper.InjectCssIntoHtml(null, "css"));
		}
	}
}
