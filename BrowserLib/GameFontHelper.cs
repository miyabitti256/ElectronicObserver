using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserLib
{
	/// <summary>
	/// ゲーム画面（ブラウザ）上のフォント（font_j）カスタマイズ・混植用 CSS 生成および動的注入ヘルパーです。
	/// </summary>
	public static class GameFontHelper
	{
		public const string UnicodeRangeLatin = "U+0020-007E, U+00A0-00FF";

		private static readonly string InjectorScriptTemplate = LoadInjectorScriptTemplate();

		private static string LoadInjectorScriptTemplate()
		{
			var assembly = typeof(GameFontHelper).Assembly;
			using (var stream = assembly.GetManifestResourceStream("BrowserLib.GameFontInjector.js"))
			{
				if (stream == null)
				{
					throw new InvalidOperationException("Embedded resource 'BrowserLib.GameFontInjector.js' not found.");
				}
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
		}

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

		private static string GetNotoCjkJpFallbackName(string cleanJp)
		{
			// Why not append " CJK JP" unconditionally: "Noto Sans JP" が "Noto Sans JP CJK JP" になり、非Notoフォントにも不正な候補が付くのを防ぐため
			if (cleanJp.StartsWith("Noto ", StringComparison.OrdinalIgnoreCase) &&
				cleanJp.EndsWith(" JP", StringComparison.OrdinalIgnoreCase) &&
				!cleanJp.EndsWith(" CJK JP", StringComparison.OrdinalIgnoreCase))
			{
				return cleanJp.Substring(0, cleanJp.Length - 3) + " CJK JP";
			}
			return null;
		}

		/// <summary>
		/// 日本語フォント、欧文フォント、カスタムCSSからゲーム画面注入用の CSS 文字列を生成します。
		/// </summary>
		public static string GenerateFontCss(string japaneseFont, string latinFont, string customCss)
		{
			var sb = new StringBuilder();
			string cleanJp = SanitizeFontName(japaneseFont);
			string cleanLatin = SanitizeFontName(latinFont);

			if (!string.IsNullOrEmpty(cleanJp))
			{
				string cjkJp = GetNotoCjkJpFallbackName(cleanJp);

				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				if (cjkJp != null)
				{
					sb.AppendLine($"\tsrc: local(\"{cleanJp}\"), local(\"{cleanJp} Regular\"), local(\"{cjkJp}\"), local(\"{cjkJp} Regular\");");
				}
				else
				{
					sb.AppendLine($"\tsrc: local(\"{cleanJp}\"), local(\"{cleanJp} Regular\"), local(\"{cleanJp}-Regular\");");
				}
				sb.AppendLine("}");

				sb.AppendLine("@font-face {");
				sb.AppendLine("\tfont-family: \"font_j\";");
				sb.AppendLine("\tfont-weight: bold;");
				if (cjkJp != null)
				{
					sb.AppendLine($"\tsrc: local(\"{cleanJp} Bold\"), local(\"{cjkJp} Bold\"), local(\"{cleanJp} Medium\"), local(\"{cjkJp} Medium\"), local(\"{cleanJp}\"), local(\"{cjkJp}\");");
				}
				else
				{
					sb.AppendLine($"\tsrc: local(\"{cleanJp} Bold\"), local(\"{cleanJp}-Bold\"), local(\"{cleanJp} Medium\"), local(\"{cleanJp}\");");
				}
				sb.AppendLine("}");
			}

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
				sb.AppendLine($"\tsrc: local(\"{cleanLatin} Bold\"), local(\"{cleanLatin}-Bold\"), local(\"{cleanLatin}\");");
				sb.AppendLine($"\tunicode-range: {UnicodeRangeLatin};");
				sb.AppendLine("}");
			}

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
		/// カスタムフォント CSS を DOM へ動的に注入・更新（空の場合は削除）する JavaScript コードを生成します。
		/// </summary>
		public static string GenerateInjectionScript(string css)
		{
			string normalized = string.IsNullOrWhiteSpace(css) ? null : css;
			return InjectorScriptTemplate.TrimEnd() + "(" + LiveAdmiralNameMaskHelper.EscapeJsString(normalized) + ");";
		}

		/// <summary>
		/// ゲーム画面の HTML 内にカスタムフォント動的注入用スクリプトを先行挿入します。
		/// </summary>
		public static string InjectIntoHtml(string html, string css)
		{
			if (string.IsNullOrEmpty(html) || string.IsNullOrWhiteSpace(css))
			{
				return html;
			}

			string tagContent = "<script>" + GenerateInjectionScript(css) + "</script>";

			// Why not static <style> before </head>: main.js 実行直前に JS から DOM 末尾へ動的注入・監視を開始し、後続の公式 @font-face に上書きされないようにするため
			var mainMatch = Regex.Match(html, @"<script[^>]*main\.js[^>]*>", RegexOptions.IgnoreCase);
			if (mainMatch.Success)
			{
				int insertPos = mainMatch.Index;
				return html.Substring(0, insertPos) + tagContent + html.Substring(insertPos);
			}

			var pixiMatch = Regex.Match(html, @"<script[^>]*pixi(?:\.min)?\.js[^>]*></script>", RegexOptions.IgnoreCase);
			if (pixiMatch.Success)
			{
				int insertPos = pixiMatch.Index + pixiMatch.Length;
				return html.Substring(0, insertPos) + tagContent + html.Substring(insertPos);
			}

			int headCloseIdx = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
			if (headCloseIdx >= 0)
			{
				return html.Insert(headCloseIdx, tagContent);
			}

			int bodyOpenIdx = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
			if (bodyOpenIdx >= 0)
			{
				return html.Insert(bodyOpenIdx, tagContent);
			}

			return tagContent + html;
		}
	}
}
