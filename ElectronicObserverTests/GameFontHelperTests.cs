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

		[Fact]
		public void GenerateFontCss_BoldWeightSuffix_AppliesBoldToNormalAndPreservesCjkAndLocalizedCandidates()
		{
			// What: Boldウェイト付きフォント名指定時、通常ウェイト側にもBold候補が適用され、CJKフォールバックや日本語ボールド表記・PostScript名が含まれ、Bold Bold等の重複接尾辞が出ないこと
			string css = GameFontHelper.GenerateFontCss("Noto Sans JP Bold", null, null);

			Assert.NotNull(css);
			int boldRuleIdx = css.IndexOf("font-weight: bold;", StringComparison.Ordinal);
			Assert.True(boldRuleIdx > 0);
			string normalRule = css.Substring(0, boldRuleIdx);

			Assert.Contains("local(\"Noto Sans JP Bold\")", normalRule);
			Assert.Contains("local(\"NotoSansJP-Bold\")", normalRule);
			Assert.Contains("local(\"Noto Sans CJK JP Bold\")", normalRule);
			Assert.Contains("local(\"Noto Sans JP Bold\")", css.Substring(boldRuleIdx));
			Assert.DoesNotContain("Noto Sans JP Bold Regular", css);
			Assert.DoesNotContain("Noto Sans JP Bold Bold", css);

			string meiryoCss = GameFontHelper.GenerateFontCss("メイリオ Bold", null, null);
			Assert.NotNull(meiryoCss);
			int meiryoBoldIdx = meiryoCss.IndexOf("font-weight: bold;", StringComparison.Ordinal);
			Assert.True(meiryoBoldIdx > 0);
			Assert.Contains("local(\"メイリオ ボールド\")", meiryoCss.Substring(0, meiryoBoldIdx));
			Assert.Contains("local(\"メイリオ ボールド\")", meiryoCss.Substring(meiryoBoldIdx));
			Assert.DoesNotContain("メイリオ Bold Regular", meiryoCss);
			Assert.DoesNotContain("メイリオ Bold Bold", meiryoCss);
		}

		[Fact]
		public void GenerateFontCss_NamedWeightSuffix_PreservesWeightAndCjkFallbackWithoutRegularOrBoldSuffixDuplication()
		{
			// What: MediumやRegular等のウェイト名付きフォント指定時、CJKフォールバックへウェイトが引き継がれ、不正なRegular/Bold連結が起きないこと
			string jpCss = GameFontHelper.GenerateFontCss("Noto Sans JP Medium", null, null);

			Assert.NotNull(jpCss);
			Assert.Contains("local(\"Noto Sans JP Medium\")", jpCss);
			Assert.Contains("local(\"Noto Sans CJK JP Medium\")", jpCss);
			Assert.DoesNotContain("Noto Sans JP Medium Regular", jpCss);
			Assert.DoesNotContain("Noto Sans JP Medium Bold", jpCss);

			string regularCss = GameFontHelper.GenerateFontCss("Noto Sans JP Regular", null, null);
			Assert.NotNull(regularCss);
			Assert.Contains("local(\"Noto Sans JP Regular\")", regularCss);
			Assert.Contains("local(\"Noto Sans JP Bold\")", regularCss);
			Assert.DoesNotContain("Noto Sans JP Regular Regular", regularCss);

			string latinCss = GameFontHelper.GenerateFontCss(null, "Roboto Bold", null);
			Assert.NotNull(latinCss);
			int latinBoldIdx = latinCss.IndexOf("font-weight: bold;", StringComparison.Ordinal);
			Assert.True(latinBoldIdx > 0);
			Assert.Contains("local(\"Roboto Bold\")", latinCss.Substring(0, latinBoldIdx));
			Assert.Contains("local(\"Roboto-Bold\")", latinCss.Substring(0, latinBoldIdx));
			Assert.Contains("unicode-range: U+0020-007E, U+00A0-00FF;", latinCss);
			Assert.DoesNotContain("Roboto Bold Regular", latinCss);
			Assert.DoesNotContain("Roboto Bold Bold", latinCss);
		}

		[Fact]
		public void FormatAndResolveGameFontSpec_RoundTripsBoldSelection()
		{
			// What: FontDialogの太字選択が保存用フォント名へ反映され、再選択時にもベースファミリ名と太字フラグへ復元されること
			Assert.Equal("Noto Sans JP", GameFontHelper.FormatGameFontName("Noto Sans JP", false));
			Assert.Equal("Noto Sans JP Bold", GameFontHelper.FormatGameFontName("Noto Sans JP", true));
			Assert.Equal("Noto Sans JP Bold", GameFontHelper.FormatGameFontName("Noto Sans JP Bold", true));

			Func<string, bool> installed = name =>
				string.Equals(name, "Noto Sans JP", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(name, "Noto Sans JP Medium", StringComparison.OrdinalIgnoreCase);

			Assert.True(GameFontHelper.ResolveGameFontSpec("Noto Sans JP Bold", installed, out string fam1, out bool bold1));
			Assert.Equal("Noto Sans JP", fam1);
			Assert.True(bold1);

			Assert.True(GameFontHelper.ResolveGameFontSpec("メイリオ ボールド", n => n == "メイリオ", out string fam2, out bool bold2));
			Assert.Equal("メイリオ", fam2);
			Assert.True(bold2);

			Assert.True(GameFontHelper.ResolveGameFontSpec("Noto Sans JP Medium", installed, out string fam3, out bool bold3));
			Assert.Equal("Noto Sans JP Medium", fam3);
			Assert.False(bold3);

			Assert.False(GameFontHelper.ResolveGameFontSpec("Bold", installed, out _, out _));
		}
	}
}
