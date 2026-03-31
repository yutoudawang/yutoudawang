# Hardware-Trigger Image Acquisition – Hikvision MVS Cameras

This guide covers wiring, trigger-mode selection, code usage, and troubleshooting for the three-camera hardware-trigger workflow implemented in `CameraApp`.

---

## 1. Overview

| Camera | Index | HALCON Window      | Stored image field |
|--------|-------|--------------------|--------------------|
| H1     | 0     | `hWindowControl1`  | `camH1_ho_Image`   |
| H2     | 1     | `hWindowControl2`  | `camH2_ho_Image`   |
| H3     | 2     | `hWindowControl3`  | `camH3_ho_Image`   |

Each camera is a `HikCam` instance.  Frames are displayed via `Tools.DispObj` on the UI thread after being marshalled from the SDK callback thread.

---

## 2. Hardware Wiring (Line Trigger)

```
External trigger source
        │
        ├─── OPTO-IN (Line0) ──► Camera H1  (GigE / USB3)
        ├─── OPTO-IN (Line0) ──► Camera H2
        └─── OPTO-IN (Line0) ──► Camera H3
```

### Electrical requirements

| Parameter               | Typical value                   |
|-------------------------|---------------------------------|
| Input voltage (HIGH)    | 5 V – 24 V DC                   |
| Pulse width (minimum)   | ≥ 1 µs (check camera data sheet)|
| Rise / fall time        | ≤ 1 µs recommended              |
| Polarity (default)      | Rising edge (configurable)      |

> **Caution:** Never apply voltages outside the camera's rated input range.
> Use an optically-isolated signal when connecting to industrial PLCs.

### Connector pinout (MV-CA / MV-CE series – 6-pin Hirose HR10)

```
Pin 1 – Power (12 V / 24 V out, optional)
Pin 2 – GND
Pin 3 – Line0+ (OPTO-IN positive)
Pin 4 – Line0– (OPTO-IN negative / GND reference)
Pin 5 – Line1 / Strobe (output)
Pin 6 – GND
```

Refer to the specific camera's hardware manual for the exact connector type and pinout.

---

## 3. Trigger Mode Selection

### 3.1 Via the UI

| Button              | Mode description                         |
|---------------------|------------------------------------------|
| Start Hardware Trigger | Rising-edge trigger on Line0 – one frame per pulse |
| Start Continuous       | Free-run at maximum camera frame rate   |
| Stop                   | Stop all acquisition                    |
| Grab H1 / H2 / H3      | Single blocking grab (software trigger or continuous) |

### 3.2 Via code (`HikCam`)

```csharp
// ── Continuous (free-run) ────────────────────────────────────────────────
cam.ConfigureTrigger(TriggerMode.Continuous);
cam.StartGrabbing();

// ── Software trigger ─────────────────────────────────────────────────────
cam.ConfigureTrigger(TriggerMode.Software);
cam.StartGrabbing();
// Issue one frame:
cam.GrabImage(timeoutMs: 2000);

// ── Hardware trigger (rising edge on Line0) ──────────────────────────────
cam.ConfigureTrigger(TriggerMode.Hardware);          // default: rising edge
cam.StartGrabbing();
// Camera now waits for an electrical pulse.

// ── Hardware trigger (falling edge) ─────────────────────────────────────
cam.ConfigureTrigger(TriggerMode.Hardware, activationMode: 1); // 1 = FallingEdge

// ── Switch modes at runtime ──────────────────────────────────────────────
cam.StopGrabbing();
cam.ConfigureTrigger(TriggerMode.Continuous);
cam.StartGrabbing();

// ── Stop and release ─────────────────────────────────────────────────────
cam.StopGrabbing();
cam.CloseCam();
cam.Dispose();
```

### 3.3 TriggerActivation values

| Value | Meaning       |
|-------|---------------|
| 0     | RisingEdge    |
| 1     | FallingEdge   |
| 2     | AnyEdge       |
| 3     | LevelHigh     |
| 4     | LevelLow      |

### 3.4 TriggerSource constants

| Value | Meaning   |
|-------|-----------|
| 0     | Line0     |
| 1     | Line1     |
| 2     | Line2     |
| 3     | Line3     |
| 7     | Software  |

To use a different line, change the constant inside `HikCam.ConfigureTrigger`:

```csharp
case TriggerMode.Hardware:
    _cam.MV_CC_SetEnumValue_NET("TriggerMode",      1u);
    _cam.MV_CC_SetEnumValue_NET("TriggerSource",    1u); // ← Line1
    _cam.MV_CC_SetEnumValue_NET("TriggerActivation", activationMode);
```

---

## 4. Frame Callback and UI Display

```
SDK thread                       UI thread
─────────────────────────────    ─────────────────────────────────────
FrameCallbackInternal()          BeginInvoke(uiAction)
  │  copy bytes → FrameEventArgs     │
  │                                  ▼
  └─► FrameReceived event        DisplayFrame()
       │                           ├── dispose old HObject
       ▼                           ├── store new HObject
      OnCamH1Frame()               ├── Tools.SetupWindow(window, e)
        │                          └── Tools.DispObj(image, window)
        └──────────────────────────►
```

All image construction (`BuildHalconImageFromBytes`) happens on the callback thread to keep the UI thread free.  The HALCON `HObject` is only handed off to the UI thread for display.

---

## 5. Resource Management

The application follows this lifecycle:

```
Form1_Load
  └── OpenCam(0/1/2)          ← open all cameras

(user action)
  ├── StartHardwareTriggerAcquisition()
  │     └── ConfigureTrigger + StartGrabbing  per camera
  │
  └── StopAcquisition()
        └── StopGrabbing  per camera

Form1_FormClosing
  ├── StopAcquisition()
  ├── Dispose() × 3          ← CloseCam + DestroyHandle inside
  └── camH*_ho_Image.Dispose()
```

`HikCam` implements `IDisposable`.  Calling `Dispose()` calls `CloseCam()` which calls `StopGrabbing()` → `MV_CC_CloseDevice_NET()` → `MV_CC_DestroyHandle_NET()` in the correct order.

---

## 6. Supported Pixel Formats

| MVS pixel type                  | HALCON image type         |
|---------------------------------|---------------------------|
| `PixelType_Gvsp_Mono8`          | `"byte"` (single channel) |
| `PixelType_Gvsp_RGB8_Packed`    | RGB (interleaved → HALCON `GenImageInterleaved`) |
| Other                           | Falls back to `"byte"` single-channel |

---

## 7. Prerequisites and Setup

### 7.1 Software dependencies

| Component    | Version / notes                                           |
|--------------|-----------------------------------------------------------|
| .NET Framework | 4.7.2                                                  |
| HALCON       | 23.11 – install with "dotnet" component; sets `%HALCONROOT%` |
| MVS SDK      | Latest from Hikrobotics – sets `%MVSCAMCTRL%`            |

### 7.2 Environment variables used by the project

| Variable       | Typical path (Windows)                                         |
|----------------|----------------------------------------------------------------|
| `HALCONROOT`   | `C:\Program Files\MVTec\HALCON-23.11-Progress`                 |
| `MVSCAMCTRL`   | `C:\Program Files (x86)\MVS`                                   |

If HALCON or MVS is installed to a non-default path, update the `<HintPath>` entries in `CameraApp.csproj`.

### 7.3 GigE camera network setup

1. Set the host NIC to a static IP on the same subnet as the cameras (e.g. `192.168.1.x/24`).
2. Increase the NIC **Jumbo Frames** MTU to 9000 bytes to maximise throughput.
3. Disable Windows Firewall for the GigE interface, or create an explicit allow rule for MVS.
4. Use the **MVS Client** software to verify cameras are visible before running the application.

---

## 8. Troubleshooting

### Camera not detected

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `nDeviceNum == 0` | MVS service not running | Start **MVS** or reboot |
| Status shows "OFFLINE" | GigE subnet mismatch | Align IP addresses |
| Only 1 of 3 cameras visible | USB bandwidth | Spread cameras across USB controllers |

### No frames received in hardware trigger mode

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `FrameReceived` never fires | No electrical pulse arriving | Verify signal with oscilloscope on Line0+ / Line0– |
| Frames fire on every 2nd pulse | Wrong edge polarity | Change `activationMode` to `1` (FallingEdge) |
| Frames arrive but image is black | Exposure too short for flash | Increase `ExposureTime` via MVS Client |

```csharp
// Adjust exposure programmatically (µs):
_cam.MV_CC_SetFloatValue_NET("ExposureTime", 5000.0f);   // 5 ms
```

### HALCON display errors

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `hWindowControl` appears blank | `SetPart` never called | Ensure `Tools.SetupWindow(window, width, height)` is called before `DispObj` |
| `HalconException: Invalid image` | Pixel format not handled | Add a new `case` for the format in `HikCam.BuildHalconImage` |

### SDK return codes

| Code (hex) | Meaning |
|-----------|---------|
| `0x00000000` | `MV_OK` – success |
| `0x80000001` | Handle already exists |
| `0x80000100` | Device not found |
| `0x80000102` | Already connected |
| `0x80000105` | Timeout |
| `0x80000106` | Invalid parameter |

A full list is in the MVS SDK documentation: `MVS SDK Help.chm` → *Error Codes*.

---

## 9. Sample: Full Hardware-Trigger Snippet

```csharp
using CameraApp;

// 1. Open camera
var cam = new HikCam();
if (!cam.OpenCam(0))
    throw new Exception("Failed to open camera at index 0");

// 2. Configure hardware trigger (rising edge, Line0)
cam.ConfigureTrigger(TriggerMode.Hardware);

// 3. Subscribe to frame event
cam.FrameReceived += (sender, e) =>
{
    // e.Data     – raw pixel bytes (Mono8 or RGB8)
    // e.Width    – image width
    // e.Height   – image height
    // e.PixelType – MVS pixel format

    HalconDotNet.HObject img =
        HikCam.BuildHalconImageFromBytes(e.Data, e.Width, e.Height, e.PixelType);

    // Marshal to UI thread and display
    myForm.BeginInvoke(new Action(() =>
    {
        Tools.SetupWindow(myHalconWindow, e.Width, e.Height);
        Tools.DispObj(img, myHalconWindow);
    }));
};

// 4. Start grabbing – camera waits for Line0 pulses
cam.StartGrabbing();

// … later …

// 5. Stop and release
cam.StopGrabbing();
cam.Dispose();
```
