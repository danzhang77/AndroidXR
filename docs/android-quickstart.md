# Android Phone Quickstart

The phone app is the Unity project itself. It captures touches in the lower simulation pad, decodes keys on Android, and sends JSON WebSocket events to the laptop relay.

## 1. Start the Laptop Relay

From the repo root:

```sh
cd relay-server
npm start
```

Open the browser demo:

```text
http://localhost:8787
```

## 2. Pick a Phone Connection Mode

### Option A: USB, easiest

Keep the Unity default relay URL as:

```text
ws://localhost:8787
```

Connect the Android phone over USB with USB debugging enabled, then run:

```sh
adb reverse tcp:8787 tcp:8787
```

This makes `localhost:8787` inside the Android app forward to the laptop relay.

### Option B: Same Wi-Fi Network

Find the laptop LAN IP address, for example `192.168.1.23`.

In Unity, select the `KeyboardDemoController` if present, or use the generated runtime controller default and change the serialized `relayUrl` value in code before building:

```text
ws://192.168.1.23:8787
```

The phone and laptop must be on the same network, and the laptop firewall must allow port `8787`.

## 3. Build And Run From Unity

1. Open the repo in Unity.
2. Wait for Unity Package Manager to resolve `NativeWebSocket`.
3. Go to `File > Build Profiles` or `File > Build Settings`.
4. Switch platform to Android.
5. Confirm `Assets/Scenes/SampleScene.unity` is in the scene list.
6. Connect the phone with USB debugging enabled.
7. Build and Run.

## 4. Expected Behavior

- Laptop browser page should show `Android connected: 1`.
- Touching/moving on the phone pad should move the yellow cursor in the browser.
- Releasing near a key should send `keyboard.commit` and append text in the browser.

If the browser says connected but no Android appears, check the phone relay URL and whether `adb reverse tcp:8787 tcp:8787` is active.

