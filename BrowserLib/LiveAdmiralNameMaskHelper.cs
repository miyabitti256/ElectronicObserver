using System;
using System.Text;
using System.Text.RegularExpressions;

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
	if (window.__eoAdmiralMaskInstalled) {{
		if (window.__setAdmiralMask) {{
			window.__setAdmiralMask({enabledJs}, {targetJs}, {maskJs});
		}}
		return;
	}}
	window.__eoAdmiralMaskInstalled = true;

	window.__eoAdmiralMask = {{
		enabled: {enabledJs},
		targetName: {targetJs},
		maskText: {maskJs},
		nodes: []
	}};

	function replaceName(str) {{
		if (!str || typeof str !== 'string') return str;
		var m = window.__eoAdmiralMask;
		if (!m || !m.enabled || !m.targetName) return str;
		return str.indexOf(m.targetName) !== -1 ? str.split(m.targetName).join(m.maskText) : str;
	}}

	window.__setAdmiralMask = function(enabled, targetName, maskText) {{
		window.__eoAdmiralMask.enabled = !!enabled;
		window.__eoAdmiralMask.targetName = targetName || '';
		window.__eoAdmiralMask.maskText = maskText || '提督';

		var nodes = window.__eoAdmiralMask.nodes;
		if (nodes && nodes.length) {{
			for (var i = 0; i < nodes.length; i++) {{
				try {{
					var node = nodes[i];
					if (node && !node.destroyed && typeof node._text === 'string') {{
						if (window.__eoAdmiralMask.enabled && window.__eoAdmiralMask.targetName) {{
							if (node._text.indexOf(window.__eoAdmiralMask.targetName) !== -1) {{
								if (!node.__eoOriginalText) node.__eoOriginalText = node._text;
								node.text = replaceName(node._text);
								if (node.updateText) node.updateText();
							}}
						}} else if (!window.__eoAdmiralMask.enabled && node.__eoOriginalText) {{
							var orig = node.__eoOriginalText;
							node.__eoOriginalText = null;
							node.text = orig;
							if (node.updateText) node.updateText();
						}}
					}}
				}} catch (e) {{}}
			}}
		}}

		try {{
			if (typeof PIXI !== 'undefined' && PIXI.Text) {{
				function scanDisplayObject(obj, depth) {{
					if (!obj || depth > 20) return;
					if (obj instanceof PIXI.Text && typeof obj._text === 'string') {{
						if (window.__eoAdmiralMask.nodes.indexOf(obj) === -1) {{
							window.__eoAdmiralMask.nodes.push(obj);
						}}
						if (window.__eoAdmiralMask.enabled && window.__eoAdmiralMask.targetName) {{
							if (obj._text.indexOf(window.__eoAdmiralMask.targetName) !== -1) {{
								if (!obj.__eoOriginalText) obj.__eoOriginalText = obj._text;
								obj.text = replaceName(obj._text);
								if (obj.updateText) obj.updateText();
							}}
						}} else if (!window.__eoAdmiralMask.enabled && obj.__eoOriginalText) {{
							var orig = obj.__eoOriginalText;
							obj.__eoOriginalText = null;
							obj.text = orig;
							if (obj.updateText) obj.updateText();
						}}
					}}
					if (obj.children && obj.children.length) {{
						for (var j = 0; j < obj.children.length; j++) {{
							scanDisplayObject(obj.children[j], depth + 1);
						}}
					}}
				}}
				for (var k in window) {{
					try {{
						var val = window[k];
						if (val && (val.stage || val.children)) {{
							scanDisplayObject(val.stage || val, 0);
						}}
					}} catch (e) {{}}
				}}
			}}
		}} catch (e) {{}}
	}};

	if (typeof CanvasRenderingContext2D !== 'undefined') {{
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
	}}

	if (typeof PIXI !== 'undefined' && PIXI.Text && PIXI.Text.prototype) {{
		try {{
			var origRenderWebGL = PIXI.Text.prototype._renderWebGL;
			if (origRenderWebGL && !PIXI.Text.prototype._renderWebGL.__eoHooked) {{
				var renderWebGLHook = function(renderer) {{
					var m = window.__eoAdmiralMask;
					if (m && m.enabled && m.targetName && typeof this._text === 'string') {{
						if (this._text.indexOf(m.targetName) !== -1) {{
							if (!this.__eoOriginalText) {{
								this.__eoOriginalText = this._text;
							}}
							this.text = replaceName(this._text);
						}}
					}} else if (m && !m.enabled && this.__eoOriginalText) {{
						var orig = this.__eoOriginalText;
						this.__eoOriginalText = null;
						this.text = orig;
					}}
					return origRenderWebGL.apply(this, arguments);
				}};
				renderWebGLHook.__eoHooked = true;
				PIXI.Text.prototype._renderWebGL = renderWebGLHook;
			}}
		}} catch (e) {{}}

		try {{
			var origRenderCanvas = PIXI.Text.prototype._renderCanvas;
			if (origRenderCanvas && !PIXI.Text.prototype._renderCanvas.__eoHooked) {{
				var renderCanvasHook = function(renderer) {{
					var m = window.__eoAdmiralMask;
					if (m && m.enabled && m.targetName && typeof this._text === 'string') {{
						if (this._text.indexOf(m.targetName) !== -1) {{
							if (!this.__eoOriginalText) {{
								this.__eoOriginalText = this._text;
							}}
							this.text = replaceName(this._text);
						}}
					}} else if (m && !m.enabled && this.__eoOriginalText) {{
						var orig = this.__eoOriginalText;
						this.__eoOriginalText = null;
						this.text = orig;
					}}
					return origRenderCanvas.apply(this, arguments);
				}};
				renderCanvasHook.__eoHooked = true;
				PIXI.Text.prototype._renderCanvas = renderCanvasHook;
			}}
		}} catch (e) {{}}

		try {{
			var desc = Object.getOwnPropertyDescriptor(PIXI.Text.prototype, 'text');
			if (desc && desc.set && !desc.set.__eoHooked) {{
				var origSet = desc.set;
				var newSet = function(val) {{
					if (window.__eoAdmiralMask.nodes.indexOf(this) === -1) {{
						window.__eoAdmiralMask.nodes.push(this);
					}}
					var m = window.__eoAdmiralMask;
					if (m && m.enabled && m.targetName && typeof val === 'string' && val.indexOf(m.targetName) !== -1) {{
						if (!this.__eoOriginalText) {{
							this.__eoOriginalText = val;
						}}
						origSet.call(this, replaceName(val));
					}} else {{
						origSet.call(this, val);
					}}
				}};
				newSet.__eoHooked = true;
				Object.defineProperty(PIXI.Text.prototype, 'text', {{
					get: desc.get,
					set: newSet,
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
		/// 未注入の場合はその場でフックスクリプトを初期化して適用します。
		/// </summary>
		public static string GenerateUpdateScript(bool maskEnabled, string targetName, string maskText)
		{
			string enabledJs = maskEnabled ? "true" : "false";
			string targetJs = EscapeJsString(targetName ?? "");
			string maskJs = EscapeJsString(string.IsNullOrEmpty(maskText) ? "提督" : maskText);
			string hookScript = GenerateHookScript(maskEnabled, targetName, maskText);

			// Why not only check window.__setAdmiralMask: HTMLフィルタが未実行の環境でも即座に自己初期化してフックを有効化するため
			return $@"
if (typeof window.__setAdmiralMask === 'undefined') {{
{hookScript}
}}
if (window.__setAdmiralMask) {{
	window.__setAdmiralMask({enabledJs}, {targetJs}, {maskJs});
}}";
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
			string tagContent = "<script>" + extraScript.ToString() + "</script>";

			// 1. pixi.js / pixi.min.js の script タグ直後を探す（CDN版 pixi.min.js 対応）
			var pixiMatch = Regex.Match(html, @"<script[^>]*pixi(?:\.min)?\.js[^>]*></script>", RegexOptions.IgnoreCase);
			if (pixiMatch.Success)
			{
				int insertPos = pixiMatch.Index + pixiMatch.Length;
				return html.Substring(0, insertPos) + tagContent + html.Substring(insertPos);
			}

			// 2. フォールバック: main.js の直前（pixi読込後かつmain実行前）
			var mainMatch = Regex.Match(html, @"<script[^>]*main\.js[^>]*>", RegexOptions.IgnoreCase);
			if (mainMatch.Success)
			{
				int insertPos = mainMatch.Index;
				return html.Substring(0, insertPos) + tagContent + html.Substring(insertPos);
			}

			// 3. フォールバック: </head> 直前
			int headIdx = html.IndexOf("</head>", StringComparison.OrdinalIgnoreCase);
			if (headIdx >= 0)
			{
				return html.Substring(0, headIdx) + tagContent + html.Substring(headIdx);
			}

			return html;
		}
	}
}
