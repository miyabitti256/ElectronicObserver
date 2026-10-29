const test = require('node:test');
const assert = require('node:assert');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

const scriptSrc = fs.readFileSync(path.join(__dirname, '../BrowserLib/GameFontInjector.js'), 'utf8');

function createDomEnv({ delayedRoot = false } = {}) {
	const nodesById = new Map();
	const observers = [];
	const fontFaces = [
		{ family: 'font_j', status: 'loaded', loadCalled: 0, load() { this.loadCalled++; this.status = 'loaded'; return Promise.resolve(this); } },
		{ family: 'font_j', status: 'unloaded', loadCalled: 0, load() { this.loadCalled++; this.status = 'loaded'; return Promise.resolve(this); } }
	];

	function makeElement(tagName) {
		const el = {
			tagName: tagName.toUpperCase(),
			_id: '',
			textContent: '',
			parentNode: null,
			children: [],
			get id() { return this._id; },
			set id(v) {
				if (this._id) nodesById.delete(this._id);
				this._id = v;
				if (v) nodesById.set(v, this);
			},
			get lastElementChild() {
				return this.children.length > 0 ? this.children[this.children.length - 1] : null;
			},
			appendChild(child) {
				if (child.parentNode) {
					const idx = child.parentNode.children.indexOf(child);
					if (idx >= 0) child.parentNode.children.splice(idx, 1);
				}
				child.parentNode = this;
				this.children.push(child);
				if (child.id) nodesById.set(child.id, child);
				for (const obs of observers) {
					if (obs.active && obs.target === this) {
						obs.callback([{ type: 'childList', target: this }]);
					}
				}
				return child;
			},
			removeChild(child) {
				const idx = this.children.indexOf(child);
				if (idx >= 0) {
					this.children.splice(idx, 1);
					child.parentNode = null;
					if (child.id) nodesById.delete(child.id);
				}
				return child;
			}
		};
		return el;
	}

	const htmlRoot = makeElement('html');
	const head = makeElement('head');
	htmlRoot.appendChild(head);

	class MockMutationObserver {
		constructor(callback) {
			this.callback = callback;
			this.active = false;
			this.target = null;
			this.options = null;
			this.disconnectCount = 0;
			observers.push(this);
		}
		observe(target, options) {
			this.target = target;
			this.options = options;
			this.active = true;
		}
		disconnect() {
			this.active = false;
			this.disconnectCount++;
		}
	}

	const timers = [];
	const document = {
		documentElement: delayedRoot ? null : htmlRoot,
		head: delayedRoot ? null : head,
		getElementById(id) { return nodesById.get(id) || null; },
		createElement(tag) { return makeElement(tag); },
		fonts: {
			forEach(fn) { fontFaces.forEach(fn); }
		}
	};
	const window = {};

	const context = vm.createContext({
		window,
		document,
		MutationObserver: MockMutationObserver,
		setInterval(fn) {
			const entry = { fn, cleared: false };
			timers.push(entry);
			return entry;
		},
		clearInterval(entry) {
			if (entry) entry.cleared = true;
		},
		setTimeout(fn) {
			const entry = { fn, cleared: false, once: true };
			timers.push(entry);
			return entry;
		}
	});

	function runInjector(css) {
		vm.runInContext(scriptSrc + `(${JSON.stringify(css)});`, context);
	}

	function attachRootAndFlushTimers() {
		document.documentElement = htmlRoot;
		document.head = head;
		for (const t of [...timers]) {
			if (!t.cleared) {
				t.fn();
				if (t.once) t.cleared = true;
			}
		}
	}

	return { htmlRoot, head, document, window, observers, fontFaces, timers, makeElement, runInjector, attachRootAndFlushTimers };
}

test('What: 初回注入で documentElement 末尾に eo-custom-game-font の style 要素が生成され未ロード FontFace の load() が呼ばれること', () => {
	const env = createDomEnv();
	const css = '@font-face { font-family: "font_j"; src: local("Noto Sans JP"); }';

	env.runInjector(css);

	const style = env.document.getElementById('eo-custom-game-font');
	assert.ok(style);
	assert.strictEqual(style.tagName, 'STYLE');
	assert.strictEqual(style.textContent, css);
	assert.strictEqual(env.htmlRoot.lastElementChild, style);
	assert.strictEqual(env.fontFaces[0].loadCalled, 0);
	assert.strictEqual(env.fontFaces[1].loadCalled, 1);
});

test('What: 同一 CSS で複数回注入されても style 要素や MutationObserver が重複登録されないこと', () => {
	const env = createDomEnv();
	const css = '@font-face { font-family: "font_j"; src: local("Roboto"); }';

	env.runInjector(css);
	env.runInjector(css);
	env.runInjector(css);

	const matching = env.htmlRoot.children.filter(c => c.id === 'eo-custom-game-font');
	assert.strictEqual(matching.length, 1);
	assert.strictEqual(env.observers.length, 1);
	assert.strictEqual(env.observers[0].options.childList, true);
	assert.strictEqual(env.observers[0].options.subtree, undefined);
});

test('What: 後から body や公式 style が documentElement に追加されても eo-custom-game-font が常に最末尾へ移動すること', () => {
	const env = createDomEnv();
	const css = '@font-face { font-family: "font_j"; src: local("Noto Sans JP"); }';

	env.runInjector(css);

	const body = env.makeElement('body');
	env.htmlRoot.appendChild(body);
	assert.strictEqual(env.htmlRoot.lastElementChild.id, 'eo-custom-game-font');

	const officialLateStyle = env.makeElement('style');
	officialLateStyle.textContent = '@font-face { font-family: "font_j"; src: url("UDShinGo.woff2"); }';
	env.htmlRoot.appendChild(officialLateStyle);
	assert.strictEqual(env.htmlRoot.lastElementChild.id, 'eo-custom-game-font');
});

test('What: CSS 変更時に既存 style の textContent が新しい CSS に更新されること', () => {
	const env = createDomEnv();
	env.runInjector('@font-face { font-family: "font_j"; src: local("Meiryo"); }');
	env.runInjector('@font-face { font-family: "font_j"; src: local("BIZ UDPGothic"); }');

	const style = env.document.getElementById('eo-custom-game-font');
	assert.ok(style);
	assert.ok(style.textContent.includes('BIZ UDPGothic'));
	assert.ok(!style.textContent.includes('Meiryo'));
});

test('What: null または空文字が渡された際に既存の eo-custom-game-font が削除され MutationObserver が解除されること', () => {
	const env = createDomEnv();
	env.runInjector('@font-face { font-family: "font_j"; src: local("Noto Sans JP"); }');
	assert.ok(env.document.getElementById('eo-custom-game-font'));

	env.runInjector(null);

	assert.strictEqual(env.document.getElementById('eo-custom-game-font'), null);
	assert.strictEqual(env.window.__eoCustomFontObserver, null);
	assert.strictEqual(env.observers[0].active, false);
});

test('What: FrameLoadStart 極早期で documentElement が未生成の場合でも DOM 構築後に自動適用されること', () => {
	const env = createDomEnv({ delayedRoot: true });
	const css = '@font-face { font-family: "font_j"; src: local("Noto Sans JP"); }';

	env.runInjector(css);
	assert.strictEqual(env.document.getElementById('eo-custom-game-font'), null);

	env.attachRootAndFlushTimers();

	const style = env.document.getElementById('eo-custom-game-font');
	assert.ok(style);
	assert.strictEqual(style.textContent, css);
	assert.strictEqual(env.htmlRoot.lastElementChild, style);
});
