using System;
using System.Collections.Generic;
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

		private static readonly Regex WeightSuffixRegex = new Regex(
			@"^(?<base>.+?)[\s\-]+(?<weight>Thin|ExtraLight|UltraLight|Light|DemiLight|SemiLight|Semilight|Regular|Normal|Medium|DemiBold|SemiBold|Semibold|Bold|ボールド|ExtraBold|UltraBold|Black|Heavy|ExtraBlack|UltraBlack)$",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		/// <summary>
		/// フォント名末尾のウェイト表記（Bold, Medium, Light 等）を分離します。
		/// </summary>
		public static bool TrySplitFontWeight(string fontName, out string baseName, out string weight)
		{
			if (string.IsNullOrWhiteSpace(fontName))
			{
				baseName = string.Empty;
				weight = null;
				return false;
			}

			string trimmed = fontName.Trim();
			var match = WeightSuffixRegex.Match(trimmed);
			if (!match.Success)
			{
				baseName = trimmed;
				weight = null;
				return false;
			}

			baseName = match.Groups["base"].Value.TrimEnd();
			string rawWeight = match.Groups["weight"].Value;
			// Why not keep Japanese "ボールド" separate: ダイアログ保存時やフォールバック展開時に "Bold" として統一的に扱うため
			weight = string.Equals(rawWeight, "ボールド", StringComparison.Ordinal) ||
				string.Equals(rawWeight, "Bold", StringComparison.OrdinalIgnoreCase)
				? "Bold"
				: rawWeight;
			return true;
		}

		/// <summary>
		/// FontDialog で選択されたファミリ名と太字状態から設定保存用のフォント名を組み立てます。
		/// </summary>
		public static string FormatGameFontName(string familyName, bool isBold)
		{
			if (string.IsNullOrWhiteSpace(familyName))
			{
				return string.Empty;
			}

			string trimmed = familyName.Trim();
			if (!isBold)
			{
				return trimmed;
			}

			if (TrySplitFontWeight(trimmed, out _, out string existingWeight) &&
				string.Equals(existingWeight, "Bold", StringComparison.OrdinalIgnoreCase))
			{
				return trimmed;
			}

			return trimmed + " Bold";
		}

		/// <summary>
		/// 設定値から FontDialog 初期選択用のファミリ名と太字フラグを解決します。
		/// </summary>
		public static bool ResolveGameFontSpec(string fontSetting, Func<string, bool> isInstalledFamily, out string familyName, out bool isBold)
		{
			familyName = null;
			isBold = false;

			if (string.IsNullOrWhiteSpace(fontSetting) || isInstalledFamily == null)
			{
				return false;
			}

			string trimmed = fontSetting.Trim();
			if (isInstalledFamily(trimmed))
			{
				familyName = trimmed;
				return true;
			}

			if (TrySplitFontWeight(trimmed, out string baseName, out string weight) &&
				string.Equals(weight, "Bold", StringComparison.OrdinalIgnoreCase) &&
				isInstalledFamily(baseName))
			{
				familyName = baseName;
				isBold = true;
				return true;
			}

			return false;
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

		private static void AddLocalCandidate(List<string> candidates, HashSet<string> seen, string name)
		{
			if (!string.IsNullOrEmpty(name) && seen.Add(name))
			{
				candidates.Add($"local(\"{name}\")");
			}
		}

		private static string BuildExplicitWeightSrc(string baseName, string cjkBase, string weight, bool isJapanese)
		{
			var candidates = new List<string>();
			var seen = new HashSet<string>(StringComparer.Ordinal);

			AddLocalCandidate(candidates, seen, $"{baseName} {weight}");
			AddLocalCandidate(candidates, seen, $"{baseName}-{weight}");
			if (baseName.Contains(" "))
			{
				AddLocalCandidate(candidates, seen, $"{baseName.Replace(" ", "")}-{weight}");
			}
			if (cjkBase != null)
			{
				AddLocalCandidate(candidates, seen, $"{cjkBase} {weight}");
				AddLocalCandidate(candidates, seen, $"{cjkBase}-{weight}");
			}
			// Why not omit Japanese localized weight: メイリオ等の日本語環境フォントは OpenType Full Name が "メイリオ ボールド" となるため
			if (isJapanese && string.Equals(weight, "Bold", StringComparison.OrdinalIgnoreCase))
			{
				AddLocalCandidate(candidates, seen, $"{baseName} ボールド");
			}
			AddLocalCandidate(candidates, seen, baseName);
			if (cjkBase != null)
			{
				AddLocalCandidate(candidates, seen, cjkBase);
			}

			return string.Join(", ", candidates);
		}

		private static void AppendFontFaceRule(StringBuilder sb, string srcList, bool isBoldRule, bool includeLatinRange)
		{
			sb.AppendLine("@font-face {");
			sb.AppendLine("\tfont-family: \"font_j\";");
			if (isBoldRule)
			{
				sb.AppendLine("\tfont-weight: bold;");
			}
			sb.AppendLine($"\tsrc: {srcList};");
			if (includeLatinRange)
			{
				sb.AppendLine($"\tunicode-range: {UnicodeRangeLatin};");
			}
			sb.AppendLine("}");
		}

		private static void AppendFontFaceRules(StringBuilder sb, string cleanFont, bool isJapanese)
		{
			TrySplitFontWeight(cleanFont, out string baseName, out string weight);
			string cjkBase = isJapanese ? GetNotoCjkJpFallbackName(baseName) : null;
			bool isStandardRegular = string.IsNullOrEmpty(weight) ||
				string.Equals(weight, "Regular", StringComparison.OrdinalIgnoreCase) ||
				string.Equals(weight, "Normal", StringComparison.OrdinalIgnoreCase);

			string normalSrc;
			string boldSrc;
			if (!isStandardRegular)
			{
				normalSrc = BuildExplicitWeightSrc(baseName, cjkBase, weight, isJapanese);
				boldSrc = normalSrc;
			}
			else if (cjkBase != null)
			{
				normalSrc = $"local(\"{baseName}\"), local(\"{baseName} Regular\"), local(\"{cjkBase}\"), local(\"{cjkBase} Regular\")";
				boldSrc = $"local(\"{baseName} Bold\"), local(\"{cjkBase} Bold\"), local(\"{baseName} Medium\"), local(\"{cjkBase} Medium\"), local(\"{baseName}\"), local(\"{cjkBase}\")";
			}
			else if (isJapanese)
			{
				normalSrc = $"local(\"{baseName}\"), local(\"{baseName} Regular\"), local(\"{baseName}-Regular\")";
				boldSrc = $"local(\"{baseName} Bold\"), local(\"{baseName}-Bold\"), local(\"{baseName} ボールド\"), local(\"{baseName} Medium\"), local(\"{baseName}\")";
			}
			else
			{
				normalSrc = $"local(\"{baseName}\"), local(\"{baseName} Regular\"), local(\"{baseName}-Regular\")";
				boldSrc = $"local(\"{baseName} Bold\"), local(\"{baseName}-Bold\"), local(\"{baseName}\")";
			}

			AppendFontFaceRule(sb, normalSrc, isBoldRule: false, includeLatinRange: !isJapanese);
			AppendFontFaceRule(sb, boldSrc, isBoldRule: true, includeLatinRange: !isJapanese);
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
				AppendFontFaceRules(sb, cleanJp, isJapanese: true);
			}

			// Why not define Latin font before Japanese: CSSの@font-faceは後から定義されたルールが同一ファミリー内で優先マッチするため
			if (!string.IsNullOrEmpty(cleanLatin))
			{
				AppendFontFaceRules(sb, cleanLatin, isJapanese: false);
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
