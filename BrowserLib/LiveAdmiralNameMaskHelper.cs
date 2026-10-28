using System;
using System.Text;

namespace BrowserLib
{
	/// <summary>
	/// ゲーム画面（ブラウザ）上で提督名をクライアントサイドマスクするためのスクリプト生成ヘルパーです。
	/// </summary>
	public static class LiveAdmiralNameMaskHelper
	{
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
		public static string GenerateHookScript(bool maskEnabled, string targetName, string maskText)
		{
			string enabledJs = maskEnabled ? "true" : "false";
			string targetJs = EscapeJsString(targetName ?? "");
			string maskJs = EscapeJsString(string.IsNullOrEmpty(maskText) ? "提督" : maskText);

			// Why not fetch/XHR hook: サーバー通信データには一切触れず、表示描画層（Canvas 2D / PixiJS）のみで差し替えるため
			// Why not only Canvas hook: PixiJS の内部テキストキャッシュ更新時にも安全に反映させるため text setter もフックする
			return $@"
(function() {{
	window.__eoAdmiralMask = {{
		enabled: {enabledJs},
		targetName: {targetJs},
		maskText: {maskJs}
	}};

	window.__setAdmiralMask = function(enabled, targetName, maskText) {{
		window.__eoAdmiralMask.enabled = !!enabled;
		window.__eoAdmiralMask.targetName = targetName || '';
		window.__eoAdmiralMask.maskText = maskText || '提督';
	}};

	function replaceName(str) {{
		if (!str || typeof str !== 'string') return str;
		var m = window.__eoAdmiralMask;
		if (!m || !m.enabled || !m.targetName) return str;
		return str.indexOf(m.targetName) !== -1 ? str.split(m.targetName).join(m.maskText) : str;
	}}

	var origFillText = CanvasRenderingContext2D.prototype.fillText;
	CanvasRenderingContext2D.prototype.fillText = function(text, x, y, maxWidth) {{
		var replaced = replaceName(text);
		return arguments.length >= 4 
			? origFillText.call(this, replaced, x, y, maxWidth) 
			: origFillText.call(this, replaced, x, y);
	}};

	var origStrokeText = CanvasRenderingContext2D.prototype.strokeText;
	CanvasRenderingContext2D.prototype.strokeText = function(text, x, y, maxWidth) {{
		var replaced = replaceName(text);
		return arguments.length >= 4 
			? origStrokeText.call(this, replaced, x, y, maxWidth) 
			: origStrokeText.call(this, replaced, x, y);
	}};

	var origMeasureText = CanvasRenderingContext2D.prototype.measureText;
	CanvasRenderingContext2D.prototype.measureText = function(text) {{
		var replaced = replaceName(text);
		return origMeasureText.call(this, replaced);
	}};

	if (typeof PIXI !== 'undefined' && PIXI.Text && PIXI.Text.prototype) {{
		try {{
			var desc = Object.getOwnPropertyDescriptor(PIXI.Text.prototype, 'text');
			if (desc && desc.set) {{
				var origSet = desc.set;
				Object.defineProperty(PIXI.Text.prototype, 'text', {{
					get: desc.get,
					set: function(val) {{
						origSet.call(this, replaceName(val));
					}},
					configurable: true,
					enumerable: desc.enumerable
				}});
			}}
		}} catch (e) {{}}
	}}
}})();";
		}

		/// <summary>
		/// 実行中にマスク設定や提督名を更新するための JavaScript コードを生成します。
		/// </summary>
		public static string GenerateUpdateScript(bool maskEnabled, string targetName, string maskText)
		{
			string enabledJs = maskEnabled ? "true" : "false";
			string targetJs = EscapeJsString(targetName ?? "");
			string maskJs = EscapeJsString(string.IsNullOrEmpty(maskText) ? "提督" : maskText);

			return $"if (window.__setAdmiralMask) {{ window.__setAdmiralMask({enabledJs}, {targetJs}, {maskJs}); }}";
		}

		/// <summary>
		/// ゲーム画面の HTML 内にスクリプトを挿入します。
		/// </summary>
		public static string InjectIntoHtml(string html, bool preserveDrawingBuffer, bool maskEnabled, string targetName, string maskText)
		{
			if (string.IsNullOrEmpty(html)) return html;

			var extraScript = new StringBuilder();
			if (preserveDrawingBuffer)
			{
				extraScript.Append("PIXI.settings.RENDER_OPTIONS.preserveDrawingBuffer=true;");
			}
			extraScript.Append(GenerateHookScript(maskEnabled, targetName, maskText));

			string targetTag = @"/pixi.js""></script>";
			int idx = html.IndexOf(targetTag, StringComparison.OrdinalIgnoreCase);
			if (idx >= 0)
			{
				int insertPos = idx + targetTag.Length;
				return html.Substring(0, insertPos) +
					   "<script>" + extraScript.ToString() + "</script>" +
					   html.Substring(insertPos);
			}

			return html;
		}
	}
}
