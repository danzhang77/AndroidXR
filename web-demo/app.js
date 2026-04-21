const PROTOCOL = 'androidxr.input.v1';

const form = document.querySelector('#connectionForm');
const serverUrlInput = document.querySelector('#serverUrl');
const statusEl = document.querySelector('#status');
const roleStatusEl = document.querySelector('#roleStatus');
const lastTouchEl = document.querySelector('#lastTouch');
const cursorEl = document.querySelector('#cursor');
const typedTextEl = document.querySelector('#typedText');
const keyboardEl = document.querySelector('#keyboard');

let socket = null;
let seq = 1;
let typedText = '';
let keys = [];
let previewKeyId = null;
let flashTimeout = null;

if (window.location.protocol === 'http:' || window.location.protocol === 'https:') {
  const wsProtocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
  serverUrlInput.value = `${wsProtocol}//${window.location.host}`;
}

function envelope(type, target, payload) {
  return {
    protocol: PROTOCOL,
    type,
    source: 'browser',
    target,
    sessionId: 'local-demo',
    seq: seq++,
    timestampMs: Date.now(),
    payload
  };
}

function send(message) {
  if (!socket || socket.readyState !== WebSocket.OPEN) {
    return;
  }

  socket.send(JSON.stringify(message));
}

function setStatus(value) {
  statusEl.textContent = value;
}

function renderTouch(payload) {
  const x = clamp01(Number(payload.x));
  const y = clamp01(Number(payload.y));
  cursorEl.style.left = `${x * 100}%`;
  cursorEl.style.bottom = `${y * 100}%`;
  cursorEl.classList.toggle('active', payload.phase !== 'up' && payload.phase !== 'cancel');
  lastTouchEl.textContent = `x ${x.toFixed(3)}, y ${y.toFixed(3)}`;

  if (payload.phase === 'up' || payload.phase === 'cancel') {
    clearPreviewKey();
    return;
  }

  setPreviewKey(findClosestKey(x, y));
}

function renderCommit(payload) {
  if (typeof payload.text === 'string') {
    if (payload.kind === 'backspace') {
      typedText = typedText.slice(0, -1);
    } else {
      typedText += payload.text;
    }
  }

  typedTextEl.textContent = typedText.length > 0 ? typedText : 'Type on the phone trackpad';
  flashKey(payload.keyId);
}

function renderRelayStatus(payload) {
  const androidCount = payload.roles && payload.roles.android || 0;
  roleStatusEl.textContent = androidCount > 0 ? `Android connected: ${androidCount}` : 'Waiting for Android';
}

function clamp01(value) {
  if (!Number.isFinite(value)) {
    return 0;
  }

  return Math.min(1, Math.max(0, value));
}

function createKeyboardLayout() {
  const createdKeys = [];
  const horizontalPadding = 0.035;
  const verticalPadding = 0.06;
  const horizontalGap = 0.012;
  const verticalGap = 0.024;
  const rowCount = 4;
  const rowHeight = (1 - (2 * verticalPadding) - ((rowCount - 1) * verticalGap)) / rowCount;
  const letterWidth = (1 - (2 * horizontalPadding) - (9 * horizontalGap)) / 10;
  const secondRowOffset = (letterWidth + horizontalGap) * 0.5;
  const thirdRowOffset = letterWidth + horizontalGap;
  const backWidth = (2 * letterWidth) + horizontalGap;
  const spaceWidth = (4 * letterWidth) + (3 * horizontalGap);
  const sideWidth = (3 * letterWidth) + (2 * horizontalGap);

  addRow(createdKeys, 0, ['Q', 'W', 'E', 'R', 'T', 'Y', 'U', 'I', 'O', 'P'], horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, 0);
  addRow(createdKeys, 1, ['A', 'S', 'D', 'F', 'G', 'H', 'J', 'K', 'L'], horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, secondRowOffset);
  addRow(createdKeys, 2, ['Z', 'X', 'C', 'V', 'B', 'N', 'M'], horizontalPadding, verticalPadding, letterWidth, rowHeight, horizontalGap, verticalGap, thirdRowOffset);

  const backY = 1 - verticalPadding - rowHeight - (2 * (rowHeight + verticalGap));
  const backX = horizontalPadding + thirdRowOffset + (7 * (letterWidth + horizontalGap));
  addKey(createdKeys, 'BACK', 'BACK', backX, backY, backWidth, rowHeight);

  const spaceY = 1 - verticalPadding - rowHeight - (3 * (rowHeight + verticalGap));
  addKey(createdKeys, 'SPACE', 'SPACE', horizontalPadding + sideWidth + horizontalGap, spaceY, spaceWidth, rowHeight);

  return createdKeys;
}

function addRow(target, rowIndex, labels, horizontalPadding, verticalPadding, keyWidth, keyHeight, horizontalGap, verticalGap, horizontalOffset) {
  const y = 1 - verticalPadding - keyHeight - (rowIndex * (keyHeight + verticalGap));
  labels.forEach((label, index) => {
    const x = horizontalPadding + horizontalOffset + (index * (keyWidth + horizontalGap));
    addKey(target, label, label, x, y, keyWidth, keyHeight);
  });
}

function addKey(target, id, label, x, y, width, height) {
  target.push({
    id,
    label,
    x,
    y,
    width,
    height,
    centerX: x + (width * 0.5),
    centerY: y + (height * 0.5)
  });
}

function renderKeyboard() {
  keys = createKeyboardLayout();
  keyboardEl.replaceChildren();

  keys.forEach((key) => {
    const element = document.createElement('div');
    element.className = 'key';
    element.dataset.keyId = key.id;
    element.textContent = key.label;
    element.style.left = `${key.x * 100}%`;
    element.style.bottom = `${key.y * 100}%`;
    element.style.width = `${key.width * 100}%`;
    element.style.height = `${key.height * 100}%`;
    keyboardEl.appendChild(element);
  });
}

function findClosestKey(x, y) {
  let closest = null;
  let closestDistance = Number.POSITIVE_INFINITY;

  keys.forEach((key) => {
    const dx = x - key.centerX;
    const dy = y - key.centerY;
    const distance = Math.sqrt((dx * dx) + (dy * dy));
    if (distance < closestDistance) {
      closest = key;
      closestDistance = distance;
    }
  });

  return closest;
}

function setPreviewKey(key) {
  if (!key || key.id === previewKeyId) {
    return;
  }

  clearPreviewKey();
  previewKeyId = key.id;
  keyElement(key.id)?.classList.add('preview');
}

function clearPreviewKey() {
  if (!previewKeyId) {
    return;
  }

  keyElement(previewKeyId)?.classList.remove('preview');
  previewKeyId = null;
}

function flashKey(keyId) {
  if (!keyId) {
    return;
  }

  if (flashTimeout) {
    window.clearTimeout(flashTimeout);
  }

  document.querySelectorAll('.key.flash').forEach((element) => element.classList.remove('flash'));
  const element = keyElement(keyId);
  if (!element) {
    return;
  }

  element.classList.add('flash');
  flashTimeout = window.setTimeout(() => {
    element.classList.remove('flash');
    flashTimeout = null;
  }, 180);
}

function keyElement(keyId) {
  return keyboardEl.querySelector(`[data-key-id="${CSS.escape(keyId)}"]`);
}

function connect(url) {
  if (socket) {
    socket.close();
  }

  setStatus(`Connecting to ${url}`);
  socket = new WebSocket(url);

  socket.addEventListener('open', () => {
    setStatus(`Connected to ${url}`);
    roleStatusEl.textContent = 'Connected, waiting for Android';
    send(envelope('client.hello', 'relay', {
      role: 'browser',
      clientName: 'Browser Glasses Demo',
      capabilities: ['trackpad.touch', 'keyboard.commit']
    }));
  });

  socket.addEventListener('message', (event) => {
    let message;
    try {
      message = JSON.parse(event.data);
    } catch {
      return;
    }

    if (!message || message.protocol !== PROTOCOL) {
      return;
    }

    if (message.type === 'trackpad.touch') {
      renderTouch(message.payload);
    } else if (message.type === 'keyboard.commit') {
      renderCommit(message.payload);
    } else if (message.type === 'relay.status') {
      renderRelayStatus(message.payload);
    }
  });

  socket.addEventListener('close', () => {
    setStatus('Disconnected');
    roleStatusEl.textContent = 'Waiting for Android';
  });

  socket.addEventListener('error', () => {
    setStatus('Connection error');
  });
}

form.addEventListener('submit', (event) => {
  event.preventDefault();
  connect(serverUrlInput.value.trim());
});

renderKeyboard();
connect(serverUrlInput.value);
