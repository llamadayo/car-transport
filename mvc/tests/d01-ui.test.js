// Run with: node mvc/tests/d01-ui.test.js (Node built-ins only).
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');
const { test } = require('node:test');

const web = path.join(__dirname, '..', 'Web');
const script = fs.readFileSync(path.join(web, 'Scripts', 'cargo-d01.js'), 'utf8');
const views = Object.fromEntries(['Locations', 'Cargo', 'SelfDrivers'].map(kind => [
  kind, fs.readFileSync(path.join(web, 'Views', 'CARGO_D01', '_' + kind + '.cshtml'), 'utf8')
]));

// Small DOM adapter: tests execute the production event handlers against real
// template controls, with node moves retaining their current form values.
class Element {
  constructor(tag, attributes = {}) {
    this.tag = tag;
    this.attributes = { ...attributes };
    this.children = [];
    this.parentNode = null;
    this.value = attributes.value || '';
    this.checked = Object.hasOwn(attributes, 'checked');
    this.disabled = Object.hasOwn(attributes, 'disabled');
    this.textContent = '';
    this.listeners = {};
  }
  get id() { return this.attributes.id; }
  get name() { return this.attributes.name; }
  set name(value) { this.attributes.name = value; }
  get rows() { return this.children.filter(child => child.tag === 'tr'); }
  get previousElementSibling() {
    const siblings = this.parentNode.children;
    return siblings[siblings.indexOf(this) - 1] || null;
  }
  get nextElementSibling() {
    const siblings = this.parentNode.children;
    return siblings[siblings.indexOf(this) + 1] || null;
  }
  getAttribute(name) { return this.attributes[name] ?? null; }
  removeAttribute(name) { delete this.attributes[name]; }
  appendChild(child) {
    if (child.tag === '#fragment') {
      for (const member of [...child.children]) this.appendChild(member);
      return child;
    }
    if (child.parentNode) child.parentNode.removeChild(child);
    this.children.push(child);
    child.parentNode = this;
    return child;
  }
  removeChild(child) {
    const index = this.children.indexOf(child);
    assert.notEqual(index, -1, 'removeChild requires a child');
    this.children.splice(index, 1);
    child.parentNode = null;
    return child;
  }
  insertBefore(child, reference) {
    if (child === reference) return child;
    if (child.parentNode) child.parentNode.removeChild(child);
    const index = this.children.indexOf(reference);
    assert.notEqual(index, -1, 'insertBefore requires a child reference');
    this.children.splice(index, 0, child);
    child.parentNode = this;
    return child;
  }
  matches(selector) {
    const checked = selector.endsWith(':checked');
    if (checked) selector = selector.slice(0, -8);
    const attribute = /^\[([^=\]]+)(?:="([^"]*)")?\]$/.exec(selector);
    let match;
    if (attribute) {
      match = Object.hasOwn(this.attributes, attribute[1]) &&
        (attribute[2] === undefined || this.getAttribute(attribute[1]) === attribute[2]);
    } else if (selector.startsWith('.')) {
      match = (this.attributes.class || '').split(/\s+/).includes(selector.slice(1));
    } else match = this.tag === selector;
    return match && (!checked || this.checked);
  }
  closest(selector) {
    for (let element = this; element; element = element.parentNode) {
      if (element.matches(selector)) return element;
    }
    return null;
  }
  querySelectorAll(selector) {
    return this.children.flatMap(child => [
      ...(child.matches(selector) ? [child] : []), ...child.querySelectorAll(selector)
    ]);
  }
  querySelector(selector) { return this.querySelectorAll(selector)[0] || null; }
  cloneNode(deep) {
    const copy = new Element(this.tag, this.attributes);
    copy.value = this.value;
    copy.checked = this.checked;
    copy.disabled = this.disabled;
    copy.textContent = this.textContent;
    if (deep) this.children.forEach(child => copy.appendChild(child.cloneNode(true)));
    return copy;
  }
  addEventListener(type, handler) { (this.listeners[type] ||= []).push(handler); }
}

function parseTemplate(kind) {
  const markup = /<template[^>]*>([\s\S]*?)<\/template>/.exec(views[kind])[1];
  const root = new Element('#fragment'), stack = [root];
  for (const token of markup.matchAll(/<\/?([a-z]+)\b([^>]*)>/g)) {
    if (token[0].startsWith('</')) { stack.pop(); continue; }
    const attributes = Object.fromEntries(Array.from(
      token[2].matchAll(/([\w-]+)(?:="([^"]*)")?/g), match => [match[1], match[2] || '']
    ));
    const element = new Element(token[1], attributes);
    stack.at(-1).appendChild(element);
    if (token[1] !== 'input' && !token[0].endsWith('/>')) stack.push(element);
  }
  return root;
}

function fixture(data = {}) {
  const form = new Element('form', { id: 'application-form' });
  const elements = { 'application-form': form };
  function register(element) { elements[element.id] = element; form.appendChild(element); return element; }
  for (const kind of ['Locations', 'Cargo', 'SelfDrivers']) {
    const table = register(new Element('tbody', { id: 'rows-' + kind }));
    const template = parseTemplate(kind);
    elements['template-' + kind] = { content: template };
    (data[kind] || []).forEach((values, index) => {
      const row = template.cloneNode(true).children[0];
      table.appendChild(row);
      row.querySelectorAll('[name]').forEach(control => {
        control.name = control.name.replace('__index__', index);
        const field = control.name.split('.').at(-1);
        control.attributes.id = kind + '_' + index + '__' + field;
        if (control.getAttribute('type') === 'checkbox') control.checked = !!values[field];
        else if (control.getAttribute('type') !== 'hidden' && Object.hasOwn(values, field)) control.value = values[field];
      });
      if (kind === 'Locations') {
        row.querySelector('[data-sequence]').textContent = String(index + 1);
        row.querySelector('[data-move="-1"]').disabled = index === 0;
        row.querySelector('[data-move="1"]').disabled = index === data[kind].length - 1;
      }
    });
  }
  for (const id of ['exercise-field', 'self-driver-section', 'submit-request', 'Category',
    'TransportType', 'StartAt', 'StartAtDate', 'StartAtTime', 'EndAt', 'EndAtDate', 'EndAtTime']) {
    register(new Element('input', { id }));
  }
  elements.Category.value = 'general';
  elements.TransportType.value = '一般';
  form.appendChild(new Element('input', { name: 'SelfDrive', value: 'false', checked: '' }));
  form.appendChild(new Element('input', { name: 'RequestReview', value: 'true', checked: '' }));
  const alerts = [];
  vm.runInNewContext(script, {
    document: { getElementById: id => elements[id], createElement: tag => new Element(tag) },
    window: { alert: message => alerts.push(message) }
  }, { filename: 'cargo-d01.js' });
  return {
    form, elements, alerts,
    rows: kind => elements['rows-' + kind].rows,
    click(target) {
      for (const listener of form.listeners.click) listener({ target });
    },
    add(kind) { this.click(new Element('button', { 'data-add': kind })); }
  };
}

function locationValues(ui) {
  return ui.rows('Locations').map(row => row.querySelector('[name]').value);
}
function assertLocationOrder(ui, expected) {
  assert.deepEqual(locationValues(ui), expected);
  ui.rows('Locations').forEach((row, index, rows) => {
    assert.equal(row.querySelector('[name]').name, 'Locations[' + index + '].Name');
    assert.equal(String(row.querySelector('[data-sequence]').textContent), String(index + 1));
    assert.equal(row.querySelector('[data-move="-1"]').disabled, index === 0);
    assert.equal(row.querySelector('[data-move="1"]').disabled, index === rows.length - 1);
  });
}
function postedFields(row) {
  return row.querySelectorAll('[name]')
    .filter(control => control.getAttribute('type') !== 'checkbox' || control.checked)
    .map(control => [control.name, control.value]);
}

test('existing and new cargo quantity controls accept positive integer counts', () => {
  const existing = /\.Quantity",[^\n]*?new \{([^}]+)\}/.exec(views.Cargo)[1];
  const attributes = Object.fromEntries(Array.from(existing.matchAll(/(\w+) = "([^"]*)"/g), match => [match[1], match[2]]));
  const added = parseTemplate('Cargo').querySelector('[name="Cargo[__index__].Quantity"]').attributes;
  for (const control of [attributes, added]) {
    assert.equal(control.type, 'number');
    assert.equal(control.min, '1');
    assert.equal(control.step, '1');
    const accepts = value => value >= Number(control.min) &&
      Number.isInteger((value - Number(control.min)) / Number(control.step));
    for (const value of [1, 2, 10]) assert.equal(accepts(value), true);
    for (const value of [0, -1, 0.001, 1.5]) assert.equal(accepts(value), false);
  }
});

test('server-rendered location rows include sequence and both move controls', () => {
  const serverRows = views.Locations.split('<template')[0];
  assert.match(serverRows, /<th>排序<\/th>/);
  assert.match(serverRows, /data-sequence>@\(i \+ 1\)/);
  assert.match(serverRows, /data-move="-1"/);
  assert.match(serverRows, /data-move="1"/);
});

test('moving locations up and down preserves unsaved input nodes and posted order', () => {
  const ui = fixture({ Locations: [{ Name: 'A' }, { Name: 'B' }, { Name: 'C' }] });
  const row = ui.rows('Locations')[1], input = row.querySelector('[name]');
  input.value = 'B edited <&>';
  ui.click(row.querySelector('[data-move="-1"]'));
  assertLocationOrder(ui, ['B edited <&>', 'A', 'C']);
  assert.equal(ui.rows('Locations')[0], row);
  assert.equal(row.querySelector('[name]'), input);
  ui.click(row.querySelector('[data-move="1"]'));
  assertLocationOrder(ui, ['A', 'B edited <&>', 'C']);
  assert.deepEqual(postedFields(row), [['Locations[1].Name', 'B edited <&>']]);
});

test('boundary moves are disabled and cannot move a row outside the itinerary', () => {
  const ui = fixture({ Locations: [{ Name: 'A' }, { Name: 'B' }] });
  ui.click(ui.rows('Locations')[0].querySelector('[data-move="-1"]'));
  ui.click(ui.rows('Locations')[1].querySelector('[data-move="1"]'));
  assertLocationOrder(ui, ['A', 'B']);
});

test('add, reorder, and delete keep contiguous binding names and update boundaries', () => {
  const ui = fixture();
  ui.add('Locations');
  assertLocationOrder(ui, ['']);
  ui.rows('Locations')[0].querySelector('[name]').value = 'A';
  ui.add('Locations');
  ui.rows('Locations')[1].querySelector('[name]').value = 'B';
  ui.add('Locations');
  ui.rows('Locations')[2].querySelector('[name]').value = 'C';
  ui.click(ui.rows('Locations')[2].querySelector('[data-move="-1"]'));
  assertLocationOrder(ui, ['A', 'C', 'B']);
  ui.click(ui.rows('Locations')[0].querySelector('[data-remove]'));
  assertLocationOrder(ui, ['C', 'B']);
  ui.click(ui.rows('Locations')[1].querySelector('[data-remove]'));
  assertLocationOrder(ui, ['C']);
  ui.click(ui.rows('Locations')[0].querySelector('[data-remove]'));
  assertLocationOrder(ui, []);
  ui.add('Locations');
  assertLocationOrder(ui, ['']);
});

test('cargo add and delete preserve values and reindex both checkbox binding inputs', () => {
  const ui = fixture({ Cargo: [
    { Name: 'remove', Quantity: '1' },
    { Name: 'keep', Length: '12.5', Width: '8', Height: '9', Weight: '0', Quantity: '2',
      Category: 'BOX', Packaging: 'edited package', Hazardous: true }
  ] });
  const kept = ui.rows('Cargo')[1];
  const originalValues = postedFields(kept).map(([, value]) => value);
  ui.click(ui.rows('Cargo')[0].querySelector('[data-remove]'));
  assert.equal(ui.rows('Cargo')[0], kept);
  assert.deepEqual(postedFields(kept).map(([, value]) => value), originalValues);
  assert.ok(kept.querySelectorAll('[name]').every(control => control.name.startsWith('Cargo[0].')));
  assert.deepEqual(postedFields(kept).filter(([name]) => name.endsWith('.Hazardous')), [
    ['Cargo[0].Hazardous', 'true'], ['Cargo[0].Hazardous', 'false']
  ]);
  ui.add('Cargo');
  const added = ui.rows('Cargo')[1];
  assert.equal(added.querySelector('[name="Cargo[1].Quantity"]').value, '1');
  assert.deepEqual(postedFields(added).filter(([name]) => name.endsWith('.Hazardous')), [
    ['Cargo[1].Hazardous', 'false']
  ]);
});

test('self-driver add/delete retains all fields and the existing eight-row limit', () => {
  const ui = fixture({ SelfDrivers: [{ Name: 'remove' }, { Name: 'Driver', Extension: '123', Phone: '0900' }] });
  const kept = ui.rows('SelfDrivers')[1];
  ui.click(ui.rows('SelfDrivers')[0].querySelector('[data-remove]'));
  assert.deepEqual(postedFields(kept), [
    ['SelfDrivers[0].Name', 'Driver'], ['SelfDrivers[0].Extension', '123'], ['SelfDrivers[0].Phone', '0900']
  ]);
  for (let count = 1; count < 8; count++) ui.add('SelfDrivers');
  ui.add('SelfDrivers');
  assert.equal(ui.rows('SelfDrivers').length, 8);
  assert.equal(ui.alerts.length, 1);
  assert.deepEqual(postedFields(kept), [
    ['SelfDrivers[0].Name', 'Driver'], ['SelfDrivers[0].Extension', '123'], ['SelfDrivers[0].Phone', '0900']
  ]);
});
