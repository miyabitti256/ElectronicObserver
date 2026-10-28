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

			// Why not hook _renderWebGL/render loop: 毎フレームの描画ループに介入すると負荷が高く多重置換の恐れがあるため、基底状態(JSON.parse/モデル)で置換してループを全廃する
			// Why not modify network stream: サーバー通信や本体プロキシの通信パケットには触れず、ブラウザ内のJSメモリ上でのみオブジェクトプロパティを差し替えるため
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
		maskText: {maskJs}
	}};

	function replaceName(str) {{
		if (!str || typeof str !== 'string') return str;
		var m = window.__eoAdmiralMask;
		if (!m || !m.enabled) return str;
		if (m.targetName && m.targetName !== m.maskText) {{
			return str.indexOf(m.targetName) !== -1 ? str.split(m.targetName).join(m.maskText) : str;
		}}
		return str;
	}}

	window.__setAdmiralMask = function(enabled, targetName, maskText) {{
		window.__eoAdmiralMask.enabled = !!enabled;
		window.__eoAdmiralMask.targetName = targetName || '';
		window.__eoAdmiralMask.maskText = maskText || '提督';
	}};

	// 1. 基底状態の置換: API レスポンスのパース時に提督名プロパティをクライアント側メモリ内で差し替える
	// これによりゲーム自身が置換後の名前をモデルに格納して描画するため、毎フレームの描画フックが不要になる
	if (typeof JSON !== 'undefined' && JSON.parse) {{
		try {{
			var origParse = JSON.parse;
			JSON.parse = function(text, reviver) {{
				var data = origParse.apply(this, arguments);
				try {{
					var m = window.__eoAdmiralMask;
					if (m && m.enabled && data && typeof data === 'object') {{
						// 自分自身の提督名 (api_port/port, api_get_member/basic, etc.)
						// Why not require targetName match for self: 初回ログイン時の母港画面表示の時点で即座に置換を反映させるため
						if (data.api_data && typeof data.api_data.api_nickname === 'string') {{
							if (!m.targetName) {{
								m.targetName = data.api_data.api_nickname;
							}}
							data.api_data.api_nickname = m.maskText;
						}}
						// 演習相手やランキング等のリスト内
						if (data.api_data && Array.isArray(data.api_data.api_list)) {{
							for (var i = 0; i < data.api_data.api_list.length; i++) {{
								var item = data.api_data.api_list[i];
								if (item && typeof item.api_nickname === 'string') {{
									if (m.targetName && (item.api_nickname === m.targetName || item.api_nickname.indexOf(m.targetName) !== -1)) {{
										item.api_nickname = m.maskText;
									}}
								}}
							}}
						}}
					}}
				}} catch (e) {{}}
				return data;
			}};
		}} catch (e) {{}}
	}}

	// 2. 万が一直接生成されるテキストに備えた軽量なフォールバック
	if (typeof CanvasRenderingContext2D !== 'undefined') {{
		try {{
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
		}} catch (e) {{}}
	}}

	// 3. PIXI.Text のセッターフック（直接代入される場合のフォールバック）
	if (typeof PIXI !== 'undefined' && PIXI.Text && PIXI.Text.prototype) {{
		try {{
			var desc = Object.getOwnPropertyDescriptor(PIXI.Text.prototype, 'text');
			if (desc && desc.set && !desc.set.__eoHooked) {{
				var origSet = desc.set;
				var newSet = function(val) {{
					var m = window.__eoAdmiralMask;
					if (m && m.enabled && m.targetName && typeof val === 'string' && val.indexOf(m.targetName) !== -1) {{
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
