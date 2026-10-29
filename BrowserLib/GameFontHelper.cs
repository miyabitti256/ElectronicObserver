using System;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserLib
{
	/// <summary>
	/// ゲーム画面（ブラウザ）上のフォント（font_j）カスタマイズ・混植用 CSS 生成および注入ヘルパーです。
	/// </summary>
	public static class GameFontHelper
	{
		public const string UnicodeRangeLatin = "U+0020-007E, U+00A0-00FF";

		/// <summary>
		/// フォント名から CSS 構文を破壊する危険な文字をサニタイズします。
		/// </summary>
		public static string SanitizeFontName(string fontName)
		{
			if (string.IsNullOrWhiteSpace(fontName))
			{
				return string.Empty;
			}

			// Why not allow multi-line: 改行によるCSSインジェクションや構文崩壊を防止するため改行以降を切り捨てる
			int newlineIdx = fontName.IndexOfAny(new[] { '\r', '\n' });
			if (newlineIdx >= 0)
			{
				fontName = fontName.Substring(0, newlineIdx);
			}

			// Why not allow arbitrary symbols: クォートや波括弧、セミコロン、コロン等の構文制御文字を防止するため
			return Regex.Replace(fontName, @"[^\w\s\-\.\u3000-\u30FF\u4E00-\u9FFF\uFF00-\uFFEF]", "").Trim();
		}

		/// <summary>
		/// 日本語フォント、欧文フォント、カスタムCSSからゲーム画面注入用の CSS 文字列を生成します。
		/// </summary>
		public static string GenerateFontCss(string japaneseFont, string latinFont, string customCss)
		{
			var sb = new StringBuilder();
			string cleanJp = SanitizeFontName(japaneseFont);
			string cleanLatin = SanitizeFontName(latinFont);

			// 1. ベース日本語フォント定義（先に定義することで欧文に該当しない文字のフォールバックとする）
			if (!string.IsNullOrEmpty(cleanJp))
			{
				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				sb.AppendLine($"\tsrc: local(\"{cleanJp}\"), local(\"{cleanJp} Regular\"), local(\"{cleanJp} CJK JP\");");
				sb.AppendLine("}");

				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				sb.AppendLine("\tfont-weight: bold;");
				sb.AppendLine($"\tsrc: local(\"{cleanJp} Bold\"), local(\"{cleanJp} Medium\"), local(\"{cleanJp}\");");
				sb.AppendLine("}");
			}

			// 2. 欧文フォント定義（後に定義することで unicode-range 内の英数字を最優先で上書きする）
			// Why not define Latin font before Japanese: CSSの@font-faceは後から定義されたルールが同一ファミリー内で優先マッチするため
			if (!string.IsNullOrEmpty(cleanLatin))
			{
				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				sb.AppendLine($"\tsrc: local(\"{cleanLatin}\"), local(\"{cleanLatin} Regular\"), local(\"{cleanLatin}-Regular\");");
				sb.AppendLine($"\tunicode-range: {UnicodeRangeLatin};");
				sb.AppendLine("}");

				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				sb.AppendLine("\tfont-weight: bold;");
				sb.AppendLine($"\tsrc: local(\"{cleanLatin} Bold\"), local(\"{cleanLatin}-Bold\");");
				sb.AppendLine($"\tunicode-range: {UnicodeRangeLatin};");
				sb.AppendLine("}");
			}

			// 3. 上級者向けカスタムCSS（末尾に追加し、任意の個別指定や追加スタイルを適用可能にする）
			if (!string.IsNullOrWhiteSpace(customCss))
			{
				if (sb.Length > 0)
				{
					sb.AppendLine();
				}
				sb.Append(customCss.Trim());
			}

			return sb.Length > 0 ? sb.ToString() : null;
		}

		/// <summary>
		/// HTML 内にカスタムフォント CSS を &lt;style&gt; タグとして挿入します。
		/// </summary>
		public static string InjectCssIntoHtml(string html, string css)
		{
			if (string.IsNullOrEmpty(html) || string.IsNullOrWhiteSpace(css))
			{
				return html;
			}

			string styleTag = $"<style id=\"eo-custom-game-font\">\r\n{css}\r\n</style>\r\n";

			// Why not inject before fonts.css: 公式の fonts.css より後に読み込ませることで font_j の定義を確実に上書きするため
			int headCloseIdx = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
			if (headCloseIdx >= 0)
			{
				return html.Insert(headCloseIdx, styleTag);
			}

			int bodyOpenIdx = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
			if (bodyOpenIdx >= 0)
			{
				return html.Insert(bodyOpenIdx, styleTag);
			}

			return styleTag + html;
		}
	}
}
