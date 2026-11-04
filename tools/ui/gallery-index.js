// The screen gallery's page (tools/ui/gallery.ps1 inlines it; docs/ui/gallery.md, §5): one pass at a time; a find and
// a filter over its entries, its sections to jump to and the pictures' size in the bar; a viewer that pages through the
// entries and shows an entry in the other passes, a changed one with its former picture and the box round what changed.
// No double quotes in this file: tools/ui/gallery_deploy.py checks the page's quoted strings for pictures it left out.
(() => {
  const nav = document.querySelector('nav');
  const tabs = [...document.querySelectorAll('.tab')];
  const panes = [...document.querySelectorAll('.pass')];
  const figures = [...document.querySelectorAll('figure[data-id]')];
  const texts = new Map(figures.map(f => [f, f.querySelector('figcaption').textContent.toLowerCase()]));
  // The viewer's own conveniences only (the pictures' size); a page shown as a data: URL has no storage.
  const store = {
    get(key) { try { return localStorage.getItem('kichikuou-gallery.' + key); } catch { return null; } },
    set(key, value) { try { localStorage.setItem('kichikuou-gallery.' + key, value); } catch { } },
  };
  const setHash = id => { try { history.replaceState(null, '', '#' + id); } catch { } };
  let pane = null;

  function element(tag, props = {}, ...children) {
    const node = Object.assign(document.createElement(tag), props);
    node.append(...children);
    return node;
  }

  // A button that gives the keyboard back at once: Space would press it again instead of reaching the viewer.
  function button(text, title, act) {
    const node = element('button', { type: 'button', textContent: text, title });
    node.addEventListener('click', () => { act(); node.blur(); });
    return node;
  }

  // ---------------------------------------------------------------- the bar: find, filter, sections, size

  const find = element('input', { type: 'search', placeholder: 'Find: id, title, note  ( / )' });
  const changedOnly = element('input', { type: 'checkbox' });
  const changedLabel = element('label', { title: 'Only the entries that changed or are new since the former dump' },
    changedOnly, 'Changed and new');
  const sections = element('span', { className: 'sections' });
  const size = element('input', { type: 'range', min: 0.5, max: 2, step: 0.25, value: store.get('scale') ?? 1 });
  const count = element('span', { className: 'count' });
  const nothing = element('p', { className: 'empty' });
  nav.append(element('div', { className: 'tools' }, find, changedLabel, sections,
    element('label', { title: 'The pictures’ size in the grid' }, 'Size', size), count));

  const isChanged = f => f.dataset.since === 'changed' || f.dataset.since === 'new';
  changedLabel.hidden = !figures.some(isChanged);

  function filter() {
    const words = find.value.toLowerCase().split(/\s+/).filter(Boolean);
    for (const f of figures)
      f.hidden = !words.every(w => texts.get(f).includes(w)) || (changedOnly.checked && !isChanged(f));
    for (const heading of document.querySelectorAll('.pass h3'))
      heading.hidden = heading.nextElementSibling.hidden = !heading.nextElementSibling.querySelector('figure:not([hidden])');
    for (const chip of sections.children)
      chip.disabled = chip.heading.hidden;
    const all = [...pane.querySelectorAll('figure[data-id]')];
    const shown = all.filter(f => !f.hidden).length;
    count.textContent = shown === all.length ? `${all.length} entries` : `${shown} of ${all.length} entries`;
    nothing.textContent = changedOnly.checked && !all.some(isChanged) ? 'Nothing in this tab changed since the former dump.'
      : 'No entry matches.';
    nothing.hidden = shown > 0;
    pane.append(nothing);
  }

  function sectionChips() {
    sections.replaceChildren(...[...pane.querySelectorAll('h3')].map(heading => {
      const chip = button(heading.textContent, 'Go to the section', () => heading.scrollIntoView());
      chip.heading = heading;
      return chip;
    }));
  }

  function applySize() {
    document.documentElement.style.setProperty('--scale', size.value);
    store.set('scale', size.value);
  }

  // The sticky bar's height, so that a section or an entry scrolled to stops under it.
  const measure = () => document.documentElement.style.setProperty('--nav', nav.offsetHeight + 'px');

  // A new find or filter shows its first entries: the pass's top under the bar, if the page was scrolled past it.
  function refilter() {
    filter();
    if (pane.getBoundingClientRect().top < 0)
      pane.scrollIntoView();
  }

  find.addEventListener('input', refilter);
  changedOnly.addEventListener('change', refilter);
  size.addEventListener('input', applySize);
  addEventListener('resize', measure);

  // ---------------------------------------------------------------- the passes

  function flash(f) {
    f.classList.add('flash');
    setTimeout(() => f.classList.remove('flash'), 1600);
  }

  // The pass the address names (#tablet, or an entry's #tablet-treasure), else the first; an entry is scrolled to.
  function show(hash) {
    const target = hash ? document.getElementById(hash) : null;
    const next = target?.closest('.pass') ?? panes[0];
    if (next !== pane) {
      pane = next;
      panes.forEach(p => p.hidden = p !== pane);
      tabs.forEach(t => t.classList.toggle('on', t.dataset.pass === pane.id));
      sectionChips();
      filter();
      measure();
    }
    if (target?.matches('figure')) {
      target.scrollIntoView();
      flash(target);
    }
  }

  // A tab switches by itself, the address following where it can (a page shown as a data: URL has no hash to set).
  tabs.forEach(t => t.addEventListener('click', e => {
    e.preventDefault();
    show(t.dataset.pass);
    setHash(t.dataset.pass);
  }));
  addEventListener('hashchange', () => show(decodeURIComponent(location.hash.slice(1))));

  // ---------------------------------------------------------------- the viewer

  const headTitle = element('b'), headId = element('code'), headSince = element('span'), headPosition = element('small');
  const passButtons = element('div', { className: 'passes' });
  const beforeButton = button('Before', 'The former picture (Space)', () => toggleBefore());
  const boxButton = button('Box', 'The box round what changed (B)', () => toggleBox());
  const alone = element('a', { textContent: 'Alone', target: '_blank', title: 'The picture alone, in a new tab' });
  const picture = element('img', { alt: '' });
  const box = element('div', { className: 'box' });
  const label = element('b', { className: 'label', textContent: 'BEFORE' });
  const frame = element('div', { className: 'frame' }, picture, box, label);
  const stage = element('div', { className: 'stage' }, frame);
  const note = element('div', { className: 'note' });
  const keys = element('div');
  keys.innerHTML = '<kbd>←</kbd> <kbd>→</kbd> the entries &middot; <kbd>↑</kbd> <kbd>↓</kbd> this screen in ' +
    'the other tabs &middot; <kbd>Space</kbd> before / now &middot; <kbd>B</kbd> the box &middot; <kbd>Esc</kbd> close';
  const viewer = element('div', { id: 'viewer', hidden: true },
    element('div', { className: 'bar' },
      element('div', { className: 'head' }, headTitle, ' ', headId, ' ', headSince, ' ', headPosition),
      passButtons,
      element('div', { className: 'actions' }, button('◀', 'The entry before (←)', () => step(-1)),
        button('▶', 'The next entry (→)', () => step(1)), beforeButton, boxButton, alone,
        button('✕', 'Close (Esc)', () => close()))),
    stage,
    element('div', { className: 'foot' }, note, keys));
  document.body.append(viewer);

  let current = null, before = false, boxOn = true;

  // A changed entry's former picture (the website's copy has none: it leaves out data-since).
  const formerOf = f => f.dataset.since === 'changed'
    ? f.querySelector('img').getAttribute('src').replace('/current/', '/previous/') : null;

  // The shown pass's entries the filter leaves, the one in the viewer always among them.
  const entries = () => [...pane.querySelectorAll('figure[data-id]')]
    .filter(f => (!f.hidden || f === current) && f.querySelector('img'));

  // The entry in every pass that has it, in the tabs' order.
  const twins = f => figures.filter(t => t.dataset.id === f.dataset.id && t.querySelector('img'));

  function open(f, showBefore = false) {
    current = f;
    before = showBefore && formerOf(f) !== null;
    viewer.hidden = false;
    document.body.classList.add('viewing');
    setHash(f.id);
    render();
  }

  function close() {
    viewer.hidden = true;
    document.body.classList.remove('viewing');
    current.scrollIntoView({ block: 'nearest' });
    flash(current);
  }

  function step(delta) {
    const list = entries();
    if (list.length)
      open(list[(list.indexOf(current) + delta + list.length) % list.length]);
  }

  function goTo(twin) {
    show(twin.closest('.pass').id);
    open(twin);
  }

  function stepPass(delta) {
    const list = twins(current);
    if (list.length > 1)
      goTo(list[(list.indexOf(current) + delta + list.length) % list.length]);
  }

  function toggleBefore() {
    if (formerOf(current) === null)
      return;
    before = !before;
    render();
  }

  function toggleBox() {
    boxOn = !boxOn;
    render();
  }

  function render() {
    const f = current, former = formerOf(f);
    const src = before ? former : f.querySelector('img').getAttribute('src');
    headTitle.textContent = f.querySelector('.title').textContent;
    headId.textContent = f.dataset.id;
    headSince.className = f.dataset.since ?? '';
    headSince.textContent = f.dataset.since === 'changed' ? `changed · ${f.dataset.differing} px`
      : f.dataset.since === 'new' ? 'new' : '';
    const list = entries();
    headPosition.textContent = `${list.indexOf(f) + 1} / ${list.length}`;
    passButtons.replaceChildren(...twins(f).map(t => {
      const chip = button(t.closest('.pass').dataset.title, 'This screen in that tab (↑ ↓)', () => goTo(t));
      chip.classList.toggle('on', t === f);
      return chip;
    }));
    beforeButton.hidden = boxButton.hidden = former === null;
    beforeButton.classList.toggle('on', before);
    boxButton.classList.toggle('on', boxOn);
    label.hidden = !before;
    note.textContent = f.querySelector('.note')?.textContent.replace(/^\s*—\s*/, '') ?? '';
    note.hidden = !note.textContent;
    alone.href = src;
    if (picture.getAttribute('src') === src)
      fit();
    else
      picture.src = src;
    // The former picture loaded ahead, so that Space shows it at once.
    if (former !== null && !before)
      new Image().src = former;
  }

  // The picture at its own size, or smaller to fit the window; the box over what changed (left, top, right, bottom,
  // inclusive, in the picture's pixels).
  function fit() {
    const width = picture.naturalWidth, height = picture.naturalHeight;
    if (!width)
      return;
    const scale = Math.min(1, (stage.clientWidth - 16) / width, (stage.clientHeight - 16) / height);
    frame.style.width = `${Math.floor(width * scale)}px`;
    frame.style.height = `${Math.floor(height * scale)}px`;
    const edges = current.dataset.box?.split(',').map(Number);
    box.hidden = !boxOn || !edges || formerOf(current) === null;
    if (!box.hidden) {
      const [left, top, right, bottom] = edges;
      Object.assign(box.style, {
        left: `${left / width * 100}%`, top: `${top / height * 100}%`,
        width: `${(right - left + 1) / width * 100}%`, height: `${(bottom - top + 1) / height * 100}%`,
      });
    }
  }

  picture.addEventListener('load', fit);
  addEventListener('resize', () => { if (!viewer.hidden) fit(); });
  stage.addEventListener('click', e => { if (e.target === stage) close(); });

  // A picture's click opens the viewer, a changed badge's with the former picture; a click with a key held, or the
  // middle button, opens the picture alone as before.
  document.addEventListener('click', e => {
    if (e.button !== 0 || e.ctrlKey || e.metaKey || e.shiftKey || e.altKey)
      return;
    const link = e.target.closest('figure[data-id] a');
    if (!link || !link.closest('figure').querySelector('img'))
      return;
    e.preventDefault();
    open(link.closest('figure'), link.classList.contains('changed'));
  });

  document.addEventListener('keydown', e => {
    if (e.ctrlKey || e.metaKey || e.altKey)
      return;
    if (!viewer.hidden) {
      const act = {
        ArrowLeft: () => step(-1), ArrowRight: () => step(1), ArrowUp: () => stepPass(-1), ArrowDown: () => stepPass(1),
        Home: () => open(entries()[0]), End: () => open(entries().at(-1)),
        ' ': toggleBefore, b: toggleBox, B: toggleBox, Escape: close,
      }[e.key];
      if (act) {
        e.preventDefault();
        act();
      }
    } else if (e.key === '/' && document.activeElement !== find) {
      e.preventDefault();
      find.focus();
      find.select();
    } else if (e.key === 'Escape' && document.activeElement === find) {
      find.value = '';
      filter();
      find.blur();
    }
  });

  // The entry the address names is scrolled to by the page alone: the browser's own jump (made while every pass was
  // shown) and its restored position after a reload would land elsewhere.
  try { history.scrollRestoration = 'manual'; } catch { }
  applySize();
  const first = decodeURIComponent(location.hash.slice(1));
  show(first);
  addEventListener('load', () => show(first), { once: true });
})();
