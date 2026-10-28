// Why not PIXI/Canvas フック: main.js は API 応答を JSON.parse でモデル化してから描画するため、パース直後に差し替えれば初回描画から置換済みになり、描画ループにも関与しない
(function (MASK) {
	var selfId = null;
	var selfName = null;
	var parse = JSON.parse;
	function maskSelf(o) {
		if (o && typeof o.api_nickname === "string" && String(o.api_member_id) === selfId) o.api_nickname = MASK;
	}
	JSON.parse = function () {
		var r = parse.apply(this, arguments);
		var d = r && r.api_data;
		if (!d || typeof d !== "object") return r;
		var b = d.api_basic;
		if (b && b.api_member_id != null) {
			selfId = String(b.api_member_id);
			if (typeof b.api_nickname === "string") selfName = b.api_nickname;
		}
		maskSelf(b);
		maskSelf(d);
		// Why not api_member_id 照合: ランキング要素には ID が無く、名前の完全一致以外に自分の行を特定する手段が無いため
		if (selfName !== null && Array.isArray(d.api_list)) {
			d.api_list.forEach(function (e) {
				if (e && e.api_mtjmdcwtvhdr === selfName) e.api_mtjmdcwtvhdr = MASK;
			});
		}
		return r;
	};
})
