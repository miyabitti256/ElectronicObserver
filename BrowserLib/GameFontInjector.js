// Why not static <style> before </head>: キャッシュヒット時にレスポンスフィルタがスキップされる上、後続・動的追加される公式 @font-face に逆順評価で上書きされるため
(function (css) {
	var ID = "eo-custom-game-font";
	function wakeFonts() {
		// Why not rely on implicit lazy load: 同一 font_j 内にロード済みの公式フォントがあると、未ロードの local() ルールが初回同期 Canvas 描画でスキップされるため
		if (typeof document !== "undefined" && document.fonts && typeof document.fonts.forEach === "function") {
			document.fonts.forEach(function (f) {
				if (f.status === "unloaded") {
					try {
						var p = f.load();
						if (p && typeof p.catch === "function") p.catch(function () {});
					} catch (e) {}
				}
			});
		}
	}
	function apply() {
		if (typeof document === "undefined") return false;
		var root = document.documentElement || document.head;
		if (!root) return false;
		var el = document.getElementById(ID);
		if (!css) {
			if (el && el.parentNode) el.parentNode.removeChild(el);
			if (typeof window !== "undefined" && window.__eoCustomFontObserver) {
				window.__eoCustomFontObserver.disconnect();
				window.__eoCustomFontObserver = null;
			}
			return true;
		}
		var changed = false;
		if (!el) {
			el = document.createElement("style");
			el.id = ID;
			changed = true;
		}
		if (el.textContent !== css) {
			el.textContent = css;
			changed = true;
		}
		// Why not append to head once: body 内や main.js が後から追加する公式スタイルより常に DOM 順で末尾に置き、@font-face の優先順位を維持するため
		if (el.parentNode !== root || root.lastElementChild !== el) {
			root.appendChild(el);
			changed = true;
		}
		if (changed) wakeFonts();
		return true;
	}
	if (!apply()) {
		var timer = setInterval(function () {
			if (apply()) clearInterval(timer);
		}, 10);
	}
	if (css && typeof window !== "undefined" && !window.__eoCustomFontObserver && typeof MutationObserver !== "undefined") {
		var observeRoot = function () {
			var root = typeof document !== "undefined" ? document.documentElement : null;
			if (!root) {
				setTimeout(observeRoot, 10);
				return;
			}
			apply();
			var obs = new MutationObserver(function () {
				var el = document.getElementById(ID);
				if (el && root.lastElementChild !== el) {
					root.appendChild(el);
					wakeFonts();
				}
			});
			// Why not subtree true: documentElement 直下の末尾さえ維持すれば head/body 内の全スタイルより必ず後方に位置し、ゲーム中の DOM 更新負荷をゼロにできるため
			obs.observe(root, { childList: true });
			window.__eoCustomFontObserver = obs;
		};
		observeRoot();
	}
})
