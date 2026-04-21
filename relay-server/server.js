const { WebSocketServer } = require('ws');
const fs = require('fs');
const http = require('http');
const path = require('path');

const PORT = Number(process.env.PORT || 8787);
const PROTOCOL = 'androidxr.input.v1';
const WEB_DEMO_DIR = path.resolve(__dirname, '..', 'web-demo');
const CONTENT_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.js': 'application/javascript; charset=utf-8'
};

const httpServer = http.createServer(handleHttpRequest);
const server = new WebSocketServer({ server: httpServer });
const clients = new Map();

let nextClientId = 1;

function sendJson(socket, message) {
  if (socket.readyState !== socket.OPEN) {
    return;
  }

  socket.send(JSON.stringify(message));
}

function nowMs() {
  return Date.now();
}

function makeStatus() {
  const counts = { android: 0, browser: 0, unknown: 0 };
  for (const client of clients.values()) {
    counts[client.role] = (counts[client.role] || 0) + 1;
  }

  return {
    protocol: PROTOCOL,
    type: 'relay.status',
    source: 'relay',
    target: 'all',
    sessionId: 'local-demo',
    seq: 0,
    timestampMs: nowMs(),
    payload: {
      clientCount: clients.size,
      roles: counts
    }
  };
}

function broadcastStatus() {
  const status = makeStatus();
  for (const client of clients.values()) {
    sendJson(client.socket, status);
  }
}

function isEnvelope(message) {
  return message
    && message.protocol === PROTOCOL
    && typeof message.type === 'string'
    && typeof message.source === 'string'
    && typeof message.payload === 'object';
}

function routeMessage(sender, message) {
  if (message.type === 'client.hello') {
    const role = message.payload && message.payload.role;
    sender.role = role === 'android' || role === 'browser' ? role : 'unknown';
    sender.clientName = message.payload && message.payload.clientName || sender.clientName;
    console.log(`[hello] #${sender.id} role=${sender.role} name=${sender.clientName}`);
    broadcastStatus();
    return;
  }

  const recipients = [];
  for (const client of clients.values()) {
    if (client.id === sender.id) {
      continue;
    }

    if (message.target === 'all' || message.target === client.role) {
      recipients.push(client);
    }
  }

  for (const recipient of recipients) {
    sendJson(recipient.socket, message);
  }

  console.log(`[forward] #${sender.id} ${message.source}->${message.target} ${message.type} recipients=${recipients.length}`);
}

function handleHttpRequest(request, response) {
  const requestPath = request.url === '/' ? '/index.html' : request.url;
  const safePath = path.normalize(decodeURIComponent(requestPath)).replace(/^(\.\.[/\\])+/, '');
  const filePath = path.join(WEB_DEMO_DIR, safePath);

  if (!filePath.startsWith(WEB_DEMO_DIR)) {
    response.writeHead(403);
    response.end('Forbidden');
    return;
  }

  fs.readFile(filePath, (error, data) => {
    if (error) {
      response.writeHead(404);
      response.end('Not found');
      return;
    }

    response.writeHead(200, {
      'Content-Type': CONTENT_TYPES[path.extname(filePath)] || 'application/octet-stream'
    });
    response.end(data);
  });
}

server.on('connection', (socket, request) => {
  const client = {
    id: nextClientId++,
    socket,
    role: 'unknown',
    clientName: request.socket.remoteAddress || 'unknown'
  };

  clients.set(client.id, client);
  console.log(`[connect] #${client.id} ${client.clientName}`);
  broadcastStatus();

  socket.on('message', (data) => {
    let message;
    try {
      message = JSON.parse(data.toString());
    } catch (error) {
      console.warn(`[drop] #${client.id} invalid json`);
      return;
    }

    if (!isEnvelope(message)) {
      console.warn(`[drop] #${client.id} invalid envelope`);
      return;
    }

    routeMessage(client, message);
  });

  socket.on('close', () => {
    clients.delete(client.id);
    console.log(`[disconnect] #${client.id}`);
    broadcastStatus();
  });

  socket.on('error', (error) => {
    console.warn(`[error] #${client.id} ${error.message}`);
  });
});

httpServer.listen(PORT, '0.0.0.0', () => {
  console.log(`Android XR demo page available at http://localhost:${PORT}`);
  console.log(`Android XR relay listening on ws://0.0.0.0:${PORT}`);
});

