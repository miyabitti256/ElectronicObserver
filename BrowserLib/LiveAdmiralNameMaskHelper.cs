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
			// Why not permanent interval: 常時タイマーは無駄なCPU消費になるため、画面初期化直後の過渡期（3秒間）のみ数回走査して自動停止させるため
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

	var trackedTexts = [];
	var watchTimer = null;
	var watchCount = 0;

	function replaceName(str) {{
		if (!str || typeof str !== 'string') return str;
		var m = window.__eoAdmiralMask;
		if (!m || !m.enabled) return str;
		if (m.targetName && m.targetName !== m.maskText) {{
			return str.indexOf(m.targetName) !== -1 ? str.split(m.targetName).join(m.maskText) : str;
		}}
		return str;
	}}

	function trackText(textObj) {{
		if (!textObj || textObj.__eoTracked) return;
		textObj.__eoTracked = true;
		trackedTexts.push(textObj);
	}}

	function updateExistingTexts() {{
		var m = window.__eoAdmiralMask;
		if (!m || !m.enabled || !m.targetName || m.targetName === m.maskText) return;
		var target = m.targetName;
		var mask = m.maskText;

		// 1. 追跡中の PIXI.Text インスタンスを更新
		for (var i = 0; i < trackedTexts.length; i++) {{
			var t = trackedTexts[i];
			try {{
				if (t && !t.destroyed && typeof t.text === 'string' && t.text.indexOf(target) !== -1) {{
					t.text = t.text.split(target).join(mask);
					if (t.dirty !== undefined) t.dirty = true;
					if (typeof t.updateText === 'function') t.updateText();
				}}
			}} catch (e) {{}}
		}}

		// 2. ステージツリー走査フォールバック
		try {{
			if (window.stage && window.stage.children) {{
				scanContainer(window.stage, target, mask);
			}}
		}} catch (e) {{}}
	}}

	function scanContainer(parent, target, mask) {{
		if (!parent || !parent.children || !Array.isArray(parent.children)) return;
		for (var i = 0; i < parent.children.length; i++) {{
			var child = parent.children[i];
			if (!child) continue;
			try {{
				if (typeof child.text === 'string' && child.text.indexOf(target) !== -1) {{
					child.text = child.text.split(target).join(mask);
					if (child.dirty !== undefined) child.dirty = true;
					if (typeof child.updateText === 'function') child.updateText();
				}}
				if (child.children && child.children.length > 0) {{
					scanContainer(child, target, mask);
				}}
			}} catch (e) {{}}
		}}
	}}

	function startTemporaryWatch() {{
		if (watchTimer) {{
			clearInterval(watchTimer);
			watchTimer = null;
		}}
		watchCount = 0;
		updateExistingTexts();
		watchTimer = setInterval(function() {{
			watchCount++;
			updateExistingTexts();
			if (watchCount >= 20) {{
				clearInterval(watchTimer);
				watchTimer = null;
			}}
		}}, 150);
	}}

	window.__setAdmiralMask = function(enabled, targetName, maskText) {{
		var prevTarget = window.__eoAdmiralMask.targetName;
		window.__eoAdmiralMask.enabled = !!enabled;
		window.__eoAdmiralMask.targetName = targetName || '';
		window.__eoAdmiralMask.maskText = maskText || '提督';

		if (window.__eoAdmiralMask.enabled && window.__eoAdmiralMask.targetName) {{
			if (prevTarget !== window.__eoAdmiralMask.targetName) {{
				startTemporaryWatch();
			}} else {{
				updateExistingTexts();
			}}
		}}
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
							var detected = data.api_data.api_nickname;
							if (!m.targetName && detected !== m.maskText) {{
								m.targetName = detected;
								startTemporaryWatch();
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

	// 3. PIXI.Text のフック（コンストラクタ＆セッター＆遅延ロード対応）
	function hookPixiText() {{
		if (typeof PIXI === 'undefined' || !PIXI.Text || !PIXI.Text.prototype) return;

		try {{
			var desc = Object.getOwnPropertyDescriptor(PIXI.Text.prototype, 'text');
			if (desc && desc.set && !desc.set.__eoHooked) {{
				var origSet = desc.set;
				var newSet = function(val) {{
					trackText(this);
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

		try {{
			if (!PIXI.Text.__eoHooked) {{
				var origPixiText = PIXI.Text;
				var newPixiText = function(text, style, canvas) {{
					var replaced = replaceName(text);
					var inst = new origPixiText(replaced, style, canvas);
					trackText(inst);
					return inst;
				}};
				newPixiText.prototype = origPixiText.prototype;
				newPixiText.__eoHooked = true;
				for (var prop in origPixiText) {{
					if (origPixiText.hasOwnProperty(prop)) {{
						newPixiText[prop] = origPixiText[prop];
					}}
				}}
				PIXI.Text = newPixiText;
			}}
		}} catch (e) {{}}
	}}

	hookPixiText();
	if (typeof PIXI === 'undefined') {{
		try {{
			var _pixi = undefined;
			Object.defineProperty(window, 'PIXI', {{
				configurable: true,
				enumerable: true,
				get: function() {{ return _pixi; }},
				set: function(val) {{
					_pixi = val;
					hookPixiText();
				}}
			}});
		}} catch (e) {{}}
	}}

	if (window.__eoAdmiralMask.enabled && window.__eoAdmiralMask.targetName) {{
		startTemporaryWatch();
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
