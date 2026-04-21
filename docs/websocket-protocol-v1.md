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
- `keyboard.suggestions`: Android-side decoder produced current word suggestions.
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

For Android-side language model correction, `kind` may also be:

```text
replace_current_word
```

In that case, `text` contains the replacement word plus any trailing delimiter, for example:

```json
{
  "keyId": "SPACE",
  "label": "SPACE",
  "text": "hello ",
  "kind": "replace_current_word",
  "confidence": 1.0,
  "touch": {
    "x": 0.5,
    "y": 0.1
  }
}
```

The browser replaces the currently visible unfinished word with `text`.

### keyboard.suggestions

```json
{
  "rawWord": "hek",
  "suggestions": ["her", "hey", "he"]
}
```

The browser renders up to three suggestions above the keyboard. This message is advisory only and does not commit text.
