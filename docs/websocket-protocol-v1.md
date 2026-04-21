# Android XR Input WebSocket Protocol v1

This protocol connects a Unity Android phone app, a local laptop relay, and a browser-based simulated glasses view.

## Roles

- `android`: captures trackpad input and owns optional decoding.
- `browser`: renders demo state only.
- `relay`: forwards messages between clients.

## Envelope

Every message is JSON with this envelope:

```json
{
  "protocol": "androidxr.input.v1",
  "type": "trackpad.touch",
  "source": "android",
  "target": "browser",
  "sessionId": "local-demo",
  "seq": 1,
  "timestampMs": 1710000000000,
  "payload": {}
}
```

Coordinates are normalized floats in `[0,1]`. Origin is bottom-left, matching the Unity keyboard layout.

## Event Types

- `client.hello`: announces role and capabilities.
- `relay.status`: relay-generated client count/status update.
- `trackpad.touch`: normalized touch down/move/up/cancel event.
- `keyboard.commit`: Android-side decoder committed a key.
- `keyboard.clear`: Android cleared committed text.

## Payloads

### client.hello

```json
{
  "role": "android",
  "clientName": "Unity Trackpad",
  "capabilities": ["trackpad.touch", "keyboard.commit"]
}
```

### trackpad.touch

```json
{
  "pointerId": 0,
  "phase": "down",
  "x": 0.42,
  "y": 0.73,
  "pressure": 1.0
}
```

`phase` is one of `down`, `move`, `up`, or `cancel`.

### keyboard.commit

```json
{
  "keyId": "A",
  "label": "A",
  "text": "A",
  "kind": "character",
  "confidence": 0.91,
  "touch": {
    "x": 0.42,
    "y": 0.73
  }
}
```

