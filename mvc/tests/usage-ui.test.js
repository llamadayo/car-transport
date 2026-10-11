'use strict';

// Offline browser-script regression checks using only Node's standard library.
// Run from the repository root: node mvc/tests/usage-ui.test.js
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const source = fs.readFileSync(path.join(__dirname, '../Web/Scripts/cargo-usage.js'), 'utf8');

function usageForm(startValue, endValue) {
  function element(value = '') {
    return {
      value,
      textContent: '',
      validityMessage: '',
      listeners: {},
      addEventListener(event, listener) { this.listeners[event] = listener; },
      appendChild() {},
      setCustomValidity(message) { this.validityMessage = message; }
    };
  }
  const elements = {
    'usage-form': element(),
    Form_VehicleType: element(),
    Form_Vehicle: element(),
    Form_StartKm: element(startValue),
    Form_EndKm: element(endValue),
    'usage-distance': element()
  };
  vm.runInNewContext(source, {
    document: {
      getElementById(id) { return elements[id]; },
      querySelectorAll() { return []; },
      createElement() { return element(); }
    }
  }, { filename: 'cargo-usage.js' });
  return {
    start: elements.Form_StartKm,
    end: elements.Form_EndKm,
    distance: elements['usage-distance'],
    update(start, end) {
      this.start.value = start;
      this.end.value = end;
      this.end.listeners.input();
    }
  };
}

let passed = 0;
function test(name, run) {
  run();
  passed++;
  process.stdout.write('PASS ' + name + '\n');
}

test('the reported 100 to 101.05 midpoint agrees with decimal server rounding', () => {
  const form = usageForm('100', '101.05');
  assert.equal(form.distance.textContent, '1.1 km');
  assert.equal(form.start.value, '100');
  assert.equal(form.end.value, '101.05');
});

test('rounding distinguishes values below, at and above a midpoint', () => {
  const form = usageForm('100.001', '101.050');
  assert.equal(form.distance.textContent, '1.0 km');
  form.update('100.001', '101.051');
  assert.equal(form.distance.textContent, '1.1 km');
  form.update('100.001', '101.052');
  assert.equal(form.distance.textContent, '1.1 km');
  form.update('0', '.050');
  assert.equal(form.distance.textContent, '0.1 km');
});

test('large readings retain thousandths and midpoint precision', () => {
  const form = usageForm('999999997.950', '999999999');
  assert.equal(form.distance.textContent, '1.1 km');
  form.update('999999998.951', '999999999');
  assert.equal(form.distance.textContent, '0.0 km');
  form.update('999999998.950', '999999999');
  assert.equal(form.distance.textContent, '0.1 km');
  form.update('0.001', '999999999');
  assert.equal(form.distance.textContent, '999999999.0 km');
});

test('zero and equal readings display one decimal place', () => {
  const form = usageForm('0', '0');
  assert.equal(form.distance.textContent, '0.0 km');
  form.update('999999998.999', '999999998.999');
  assert.equal(form.distance.textContent, '0.0 km');
  form.update('000.0000', '0.0500');
  assert.equal(form.distance.textContent, '0.1 km');
});

test('blank, malformed, out-of-range and over-precision input never displays NaN or a distance', () => {
  for (const value of ['', ' ', '.', 'abc', 'NaN', 'Infinity', '-1', '1,000', '1.2.3', '1000000000', '999999999.001', '1.2345']) {
    const form = usageForm('0', '1');
    form.update('0', value);
    assert.equal(form.distance.textContent, '—', 'end reading: ' + JSON.stringify(value));
    form.update(value, '1');
    assert.equal(form.distance.textContent, '—', 'start reading: ' + JSON.stringify(value));
  }
});

test('reversed mileage keeps its validation error and clears it after correction', () => {
  const form = usageForm('101.05', '100');
  assert.equal(form.distance.textContent, '結束里程小於起始里程');
  assert.equal(form.end.validityMessage, '結束里程不可小於起始里程');
  form.update('100', '101.05');
  assert.equal(form.distance.textContent, '1.1 km');
  assert.equal(form.end.validityMessage, '');
});

process.stdout.write('PASS ' + passed + ' usage UI regression checks\n');
