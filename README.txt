Android XR Trackpad Prototype
=============================

Overview
--------

This repository contains an early prototype for using an Android phone as a
trackpad/input device for an Android XR glasses experience.

Current prototype roles:

1. Android Unity app
   - Captures normalized touch input from the phone screen.
   - Runs the keyboard decoder on-device.
   - Runs the language-model correction on-device.
   - Sends JSON WebSocket messages to the laptop relay.

2. Laptop relay server
   - Runs locally with Node.js.
   - Accepts WebSocket clients from Android and browser.
   - Forwards messages by role.
   - Does not own decoding, rendering, or app state.

3. Browser glasses demo
   - Simulates the Android XR glasses view.
   - Renders typed text, suggestions, QWERTY keyboard, cursor, and highlights.
   - Does not decode touch input.


Run the Laptop Relay and Web Demo
---------------------------------

From the repository root:

    cd relay-server
    npm install
    npm start

Then open this URL on the laptop:

    http://localhost:8787

The page is served by the relay server. The WebSocket endpoint uses the same
host and port:

    ws://localhost:8787

Do not open ws://localhost:8787 directly in the browser address bar. That is a
WebSocket endpoint, not a webpage.


Run the Android App
-------------------

Open this repository in Unity.

Unity should resolve the NativeWebSocket package from Packages/manifest.json.

Then build and run to an Android phone.


Phone-to-Laptop Connection Options
----------------------------------

Option A: USB with adb reverse

1. Enable USB debugging on the Android phone.
2. Connect the phone to the laptop over USB.
3. Verify adb sees the device:

       adb devices

4. Forward the phone's localhost:8787 to the laptop's localhost:8787:

       adb reverse tcp:8787 tcp:8787

5. Keep the Unity relay URL as:

       ws://localhost:8787

With adb reverse, localhost inside the Android app is forwarded over USB to the
laptop relay.

Option B: Same Wi-Fi

Use this when the phone is not connected by USB.

1. Find the laptop LAN IP address, for example:

       192.168.1.23

2. Set the Unity relay URL to:

       ws://192.168.1.23:8787

3. Make sure the phone and laptop are on the same network.
4. Make sure the laptop firewall allows port 8787.



Message Protocol
----------------

All runtime communication uses WebSocket JSON messages.

Common envelope:

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

Important message types:

    client.hello
    relay.status
    trackpad.touch
    keyboard.preview
    keyboard.commit
    keyboard.suggestions

Protocol details live in:

    docs/websocket-protocol-v1.md


Decoder and Language Model
--------------------------

The decoder runs on Android/Unity, not in the browser.

Touch decoding:

- Each letter has a Gaussian touch distribution.
- Letter parameters are defined in KeyboardLayoutData.cs.
- The decoder supports mean offsets, sigma values, and rho correlation.

Language model:

- dict10k.txt contains candidate words.
- sorted_bigram.txt contains bigram probabilities.
- Both files are loaded from:

      Assets/StreamingAssets/

Current language-model behavior:

1. Raw letters are displayed immediately.
2. Android accumulates touch observations for the current word.
3. Android sends top 3 suggestions as the word is typed.
4. Pressing SPACE corrects/replaces the current word using the language model.
5. Tapping the suggestion band accepts a suggested word directly.


Useful Files
------------

Unity Android app:

    Assets/Scripts/KeyboardDemo/
    Assets/Scripts/LanguageModel/
    Assets/Scripts/Protocol/
    Assets/Scripts/Transport/

Language model data:

    Assets/StreamingAssets/dict10k.txt
    Assets/StreamingAssets/sorted_bigram.txt

Relay server:

    relay-server/server.js

Browser demo:

    web-demo/index.html
    web-demo/styles.css
    web-demo/app.js




