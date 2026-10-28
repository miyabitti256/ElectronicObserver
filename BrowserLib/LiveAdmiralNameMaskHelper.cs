using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserLib
{
	/// <summary>
	/// ゲーム画面（ブラウザ）上で提督名をクライアントサイドマスクするためのスクリプト生成ヘルパーです。
	/// </summary>
	public static class LiveAdmiralNameMaskHelper
	{
		private static readonly string MaskScriptTemplate = LoadMaskScriptTemplate();

		private static string LoadMaskScriptTemplate()
		{
			var assembly = typeof(LiveAdmiralNameMaskHelper).Assembly;
			using (var stream = assembly.GetManifestResourceStream("BrowserLib.AdmiralNameMask.js"))
			{
				if (stream == null)
				{
					throw new InvalidOperationException("Embedded resource 'BrowserLib.AdmiralNameMask.js' not found.");
				}
				using (var reader = new StreamReader(stream, Encoding.UTF8))
				{
					return reader.ReadToEnd();
				}
			}
		}

		/// <summary>
		/// 文字列を JavaScript の文字列リテラルとして安全にエスケープします。
		/// </summary>
		public static string EscapeJsString(string str)
		{
			if (str == null) return "null";

			var sb = new StringBuilder("\"");
			foreach (char c in str)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '"': sb.Append("\\\""); break;
					case '\r': sb.Append("\\r"); break;
					case '\n': sb.Append("\\n"); break;
					case '\t': sb.Append("\\t"); break;
					case '<':
						// Why not literal '<': HTMLインラインscriptタグ内で </script> を検知して早期終了してしまうのを防ぐため
						sb.Append("\\x3c");
						break;
					default:
						if (c < 32)
						{
							sb.AppendFormat("\\u{0:x4}", (int)c);
						}
						else
						{
							sb.Append(c);
						}
						break;
				}
			}
			sb.Append("\"");
			return sb.ToString();
		}

		/// <summary>
		/// 描画フック用の JavaScript コードを生成します。
		/// </summary>
		public static string GenerateHookScript(string maskText)
		{
			return MaskScriptTemplate.TrimEnd() + "(" + EscapeJsString(string.IsNullOrEmpty(maskText) ? "提督" : maskText) + ");";
		}

		/// <summary>
		/// ゲーム画面の HTML 内にスクリプトを挿入します。
		/// </summary>
		public static string InjectIntoHtml(string html, bool preserveDrawingBuffer, bool maskEnabled, string maskText)
		{
			if (string.IsNullOrEmpty(html)) return html;

			var extraScript = new StringBuilder();
			if (preserveDrawingBuffer)
			{
				extraScript.Append("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;");
			}
			if (maskEnabled)
			{
				extraScript.Append(GenerateHookScript(maskText));
			}

			if (extraScript.Length == 0)
			{
				return html;
			}

			string tagContent = "<script>" + extraScript.ToString() + "</script>";

			// Why not pixi script 直後を最優先: main.js の初期化および API 通信開始より前に確実に JSON.parse フックを実行可能にするため
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

			int headIdx = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
			if (headIdx >= 0)
			{
				return html.Substring(0, headIdx) + tagContent + html.Substring(headIdx);
			}

			return html;
		}
	}
}
