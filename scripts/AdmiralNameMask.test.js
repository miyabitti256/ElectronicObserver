const test = require('node:test');
const assert = require('node:assert');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');

const scriptSrc = fs.readFileSync(path.join(__dirname, '../BrowserLib/AdmiralNameMask.js'), 'utf8');

function createContext(mask = '提督') {
  const context = vm.createContext({});
  const rawParse = vm.runInContext('JSON.parse', context);
  vm.runInContext(scriptSrc + `(${JSON.stringify(mask)});`, context);
  const hookedParse = vm.runInContext('JSON.parse', context);
  return {
    parse: (jsonStr, reviver) => hookedParse(jsonStr, reviver),
    rawParse: (jsonStr, reviver) => rawParse(jsonStr, reviver)
  };
}

test('require_info で ID を学習した後に port を受信した場合、port 内の api_basic.api_nickname がマスク文字列に置換されることを保証する', () => {
  const { parse } = createContext();
  parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 12345 }
    }
  }));
  const port = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 12345, api_nickname: '本名提督' }
    }
  }));
  assert.strictEqual(port.api_data.api_basic.api_nickname, '提督');
});

test('require_info を経由せず port を単独で受信した場合でも api_basic.api_nickname が置換されることを保証する', () => {
  const { parse } = createContext();
  const port = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 99999, api_nickname: '単独提督' }
    }
  }));
  assert.strictEqual(port.api_data.api_basic.api_nickname, '提督');
});

test('port 受信後、api_data 直下に同一の api_member_id を持つレスポンス(basic・record)の api_nickname が置換されることを保証する', () => {
  const { parse } = createContext();
  parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 100, api_nickname: '吹雪提督' }
    }
  }));
  const basic = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_member_id: 100,
      api_nickname: '吹雪提督'
    }
  }));
  assert.strictEqual(basic.api_data.api_nickname, '提督');
});

test('port 受信後、api_data 直下に異なる api_member_id を持つレスポンス(演習相手等)の api_nickname は置換されないことを保証する', () => {
  const { parse } = createContext();
  parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 100, api_nickname: '吹雪提督' }
    }
  }));
  const rival = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_member_id: 200,
      api_nickname: '赤城提督'
    }
  }));
  assert.strictEqual(rival.api_data.api_nickname, '赤城提督');
});

test('port 受信後、ランキング api_list[] のうち本名と完全一致する要素のみが置換され部分一致は置換されないことを保証する', () => {
  const { parse } = createContext();
  parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_basic: { api_member_id: 100, api_nickname: '田中' }
    }
  }));
  const ranking = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_list: [
        { api_mtjmdcwtvhdr: '田中' },
        { api_mtjmdcwtvhdr: '田中太郎' },
        { api_mtjmdcwtvhdr: '元祖田中' },
        { api_mtjmdcwtvhdr: '佐藤' }
      ]
    }
  }));
  assert.strictEqual(ranking.api_data.api_list[0].api_mtjmdcwtvhdr, '提督');
  assert.strictEqual(ranking.api_data.api_list[1].api_mtjmdcwtvhdr, '田中太郎');
  assert.strictEqual(ranking.api_data.api_list[2].api_mtjmdcwtvhdr, '元祖田中');
  assert.strictEqual(ranking.api_data.api_list[3].api_mtjmdcwtvhdr, '佐藤');
});

test('port を受信する前のランキングでは要素が置換されないことを保証する', () => {
  const { parse } = createContext();
  const ranking = parse(JSON.stringify({
    api_result: 1,
    api_data: {
      api_list: [
        { api_mtjmdcwtvhdr: '田中' }
      ]
    }
  }));
  assert.strictEqual(ranking.api_data.api_list[0].api_mtjmdcwtvhdr, '田中');
});

test('api_data を持たない JSON や配列・プリミティブ値が素の JSON.parse と同一の結果を返すことを保証する', () => {
  const { parse, rawParse } = createContext();
  const testInputs = [
    '{"other":123,"nested":{"val":true}}',
    '[1, 2, "test", null]',
    '12345',
    'null',
    '"hello world"',
    'true',
    'false'
  ];
  for (const input of testInputs) {
    const custom = parse(input);
    const standard = rawParse(input);
    assert.deepStrictEqual(custom, standard);
  }
});

test('JSON.parse の reviver 関数が素の JSON.parse と同一に機能することを保証する', () => {
  const { parse, rawParse } = createContext();
  const reviver = (key, value) => {
    if (typeof value === 'number') {
      return value * 2;
    }
    return value;
  };
  const jsonStr = '{"a":1,"b":2,"c":"str"}';
  const custom = parse(jsonStr, reviver);
  const standard = rawParse(jsonStr, reviver);
  assert.deepStrictEqual(custom, standard);
  assert.strictEqual(custom.a, 2);
  assert.strictEqual(custom.b, 4);
  assert.strictEqual(custom.c, 'str');
});
