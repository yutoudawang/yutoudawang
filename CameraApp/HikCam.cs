using System;
using System.Runtime.InteropServices;
using HalconDotNet;
using MvCameraControl.Net;

namespace CameraApp
{
    // ─────────────────────────────────────────────────────────────────────────
    // FrameEventArgs
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Payload delivered to every <see cref="HikCam.FrameReceived"/> subscriber.
    /// </summary>
    public sealed class FrameEventArgs : EventArgs
    {
        /// <summary>Raw pixel bytes copied from the SDK-managed buffer.</summary>
        public byte[] Data     { get; }
        /// <summary>Image width in pixels.</summary>
        public int    Width    { get; }
        /// <summary>Image height in pixels.</summary>
        public int    Height   { get; }
        /// <summary>MVS pixel-format descriptor.</summary>
        public MvGvspPixelType PixelType { get; }

        internal FrameEventArgs(byte[] data, int width, int height, MvGvspPixelType pixelType)
        {
            Data      = data;
            Width     = width;
            Height    = height;
            PixelType = pixelType;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TriggerMode
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Frame-acquisition trigger mode.</summary>
    public enum TriggerMode
    {
        /// <summary>Free-run / continuous – camera grabs at maximum frame rate.</summary>
        Continuous = 0,
        /// <summary>Software trigger – one frame per <c>TriggerSoftware</c> command.</summary>
        Software   = 1,
        /// <summary>Hardware line trigger – one frame per electrical edge on Line0.</summary>
        Hardware   = 2
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HikCam
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Thin, thread-safe wrapper around the Hikvision MVS SDK
    /// <see cref="MyCamera"/> class that supports:
    /// <list type="bullet">
    ///   <item>Enumerate / open / close a GigE or USB3 camera.</item>
    ///   <item>Three trigger modes: Continuous, Software, and Hardware (line trigger).</item>
    ///   <item>Callback-based (async) acquisition – subscribe to
    ///         <see cref="FrameReceived"/> to receive every frame.</item>
    ///   <item>Blocking (polling) acquisition via <see cref="GrabImage"/>.</item>
    ///   <item>Safe resource management through <see cref="IDisposable"/>.</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// <b>Threading</b>: <see cref="FrameReceived"/> fires on an SDK-managed
    /// thread-pool thread.  Subscribers must marshal any UI work to the UI thread
    /// themselves (e.g. <c>Control.BeginInvoke</c>).
    /// </remarks>
    public sealed class HikCam : IDisposable
    {
        // ── MVS SDK state ───────────────────────────────────────────────────
        private readonly MyCamera _cam = new MyCamera();
        private bool _isOpen;
        private bool _isGrabbing;
        private bool _disposed;

        // Keep a strong reference so the GC does not collect the delegate
        // while the SDK is still calling it.
        private MyCamera.cbOutputExdelegate _frameCallbackDelegate;

        // ── Public surface ──────────────────────────────────────────────────

        /// <summary>
        /// Fires on every frame received from the camera (callback / async mode).
        /// Raised on an SDK thread – marshal to the UI thread as needed.
        /// </summary>
        public event EventHandler<FrameEventArgs> FrameReceived;

        /// <summary>
        /// Last image grabbed via <see cref="GrabImage"/> (blocking / polling mode).
        /// The caller owns this object and must dispose it when no longer needed.
        /// </summary>
        public HObject ho_Image { get; private set; }

        // ── Open / Close ─────────────────────────────────────────────────────

        /// <summary>
        /// Enumerate all GigE + USB3 cameras and open the one at
        /// <paramref name="deviceIndex"/> (0-based).
        /// </summary>
        /// <returns><c>true</c> on success; <c>false</c> if the index is out of
        /// range or if any SDK call fails.</returns>
        public bool OpenCam(uint deviceIndex = 0)
        {
            ThrowIfDisposed();

            var deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int ret = MyCamera.MV_CC_EnumDevices_NET(
                MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE,
                ref deviceList);

            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_EnumDevices_NET failed: 0x{ret:X8}");
                return false;
            }

            if (deviceList.nDeviceNum == 0 || deviceIndex >= deviceList.nDeviceNum)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] No camera at index {deviceIndex} " +
                    $"(found {deviceList.nDeviceNum} device(s)).");
                return false;
            }

            var devInfo = (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(
                deviceList.pDeviceInfo[deviceIndex],
                typeof(MyCamera.MV_CC_DEVICE_INFO));

            ret = _cam.MV_CC_CreateHandle_NET(ref devInfo);
            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_CreateHandle_NET failed: 0x{ret:X8}");
                return false;
            }

            ret = _cam.MV_CC_OpenDevice_NET();
            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_OpenDevice_NET failed: 0x{ret:X8}");
                _cam.MV_CC_DestroyHandle_NET();
                return false;
            }

            _isOpen = true;
            return true;
        }

        /// <summary>
        /// Stop grabbing (if active) and close the camera device.
        /// Safe to call even if the camera was never opened.
        /// </summary>
        public void CloseCam()
        {
            if (!_isOpen) return;

            if (_isGrabbing) StopGrabbing();

            _cam.MV_CC_CloseDevice_NET();
            _cam.MV_CC_DestroyHandle_NET();
            _isOpen = false;
        }

        // ── Trigger configuration ────────────────────────────────────────────

        /// <summary>
        /// Set the camera trigger mode.
        /// <list type="bullet">
        ///   <item><see cref="TriggerMode.Continuous"/> – turn off trigger; free-run.</item>
        ///   <item><see cref="TriggerMode.Software"/> – trigger via software command.</item>
        ///   <item><see cref="TriggerMode.Hardware"/> – trigger on rising edge of Line0.</item>
        /// </list>
        /// Call <b>before</b> <see cref="StartGrabbing"/>.
        /// To change modes while grabbing, call <see cref="StopGrabbing"/> first.
        /// </summary>
        /// <param name="mode">Desired trigger mode.</param>
        /// <param name="activationMode">
        /// Edge polarity for hardware trigger (0 = RisingEdge [default],
        /// 1 = FallingEdge, 4 = LevelLow). Ignored for other modes.
        /// </param>
        public bool ConfigureTrigger(TriggerMode mode, uint activationMode = 0)
        {
            ThrowIfDisposed();
            if (!_isOpen) throw new InvalidOperationException("Camera is not open.");

            int ret;

            switch (mode)
            {
                case TriggerMode.Continuous:
                    // TriggerMode = Off (0) → camera runs freely.
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerMode", 0u);
                    return ret == MyCamera.MV_OK;

                case TriggerMode.Software:
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerMode", 1u /* On */);
                    if (ret != MyCamera.MV_OK) return false;
                    // TriggerSource = 7 → Software
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerSource", 7u);
                    return ret == MyCamera.MV_OK;

                case TriggerMode.Hardware:
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerMode", 1u /* On */);
                    if (ret != MyCamera.MV_OK) return false;
                    // TriggerSource = 0 → Line0 (adjust to 1/2/3 for Line1/2/3)
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerSource", 0u /* Line0 */);
                    if (ret != MyCamera.MV_OK) return false;
                    // TriggerActivation: 0 = RisingEdge, 1 = FallingEdge, etc.
                    ret = _cam.MV_CC_SetEnumValue_NET("TriggerActivation", activationMode);
                    return ret == MyCamera.MV_OK;

                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        // ── Callback (async) grabbing ─────────────────────────────────────────

        /// <summary>
        /// Register the internal frame callback and start continuous acquisition.
        /// Each received frame raises <see cref="FrameReceived"/>.
        /// </summary>
        public bool StartGrabbing()
        {
            ThrowIfDisposed();
            if (!_isOpen) throw new InvalidOperationException("Camera is not open.");
            if (_isGrabbing) return true;

            _frameCallbackDelegate = FrameCallbackInternal;
            int ret = _cam.MV_CC_RegisterImageCallBackEx_NET(
                _frameCallbackDelegate, IntPtr.Zero);

            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_RegisterImageCallBackEx_NET failed: 0x{ret:X8}");
                return false;
            }

            ret = _cam.MV_CC_StartGrabbing_NET();
            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_StartGrabbing_NET failed: 0x{ret:X8}");
                return false;
            }

            _isGrabbing = true;
            return true;
        }

        /// <summary>Stop callback-based acquisition.</summary>
        public bool StopGrabbing()
        {
            if (!_isGrabbing) return true;

            int ret = _cam.MV_CC_StopGrabbing_NET();
            _isGrabbing = false;
            _frameCallbackDelegate = null;

            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_StopGrabbing_NET failed: 0x{ret:X8}");
                return false;
            }

            return true;
        }

        // ── Manual (polling) grab ─────────────────────────────────────────────

        /// <summary>
        /// Synchronous (blocking) frame grab.  Stores the result in
        /// <see cref="ho_Image"/> as a HALCON <see cref="HObject"/>.
        /// Suitable for continuous or software-trigger mode.
        /// </summary>
        /// <param name="timeoutMs">Maximum wait time in milliseconds.</param>
        /// <returns><c>true</c> if a frame was captured successfully.</returns>
        public bool GrabImage(int timeoutMs = 1000)
        {
            ThrowIfDisposed();
            if (!_isOpen) throw new InvalidOperationException("Camera is not open.");

            var frameOut = new MyCamera.MV_FRAME_OUT();
            int ret = _cam.MV_CC_GetImageBuffer_NET(ref frameOut, timeoutMs);
            if (ret != MyCamera.MV_OK)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] MV_CC_GetImageBuffer_NET failed: 0x{ret:X8}");
                return false;
            }

            try
            {
                HObject newImage = BuildHalconImage(
                    frameOut.pBufAddr,
                    frameOut.stFrameInfo.nWidth,
                    frameOut.stFrameInfo.nHeight,
                    frameOut.stFrameInfo.enPixelType);

                if (newImage == null) return false;

                ho_Image?.Dispose();
                ho_Image = newImage;
                return true;
            }
            finally
            {
                _cam.MV_CC_FreeImageBuffer_NET(ref frameOut);
            }
        }

        // ── IDisposable ──────────────────────────────────────────────────────

        /// <summary>
        /// Stop grabbing, close the camera, and release all managed resources.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            CloseCam();
            ho_Image?.Dispose();
            _disposed = true;
        }

        // ── Private helpers ──────────────────────────────────────────────────

        // Called on an SDK-managed thread pool thread.
        private void FrameCallbackInternal(
            IntPtr pData,
            ref MyCamera.MV_FRAME_OUT_INFO_EX pFrameInfo,
            IntPtr pUser)
        {
            if (FrameReceived == null) return;

            int size = (int)pFrameInfo.nFrameLen;
            if (size <= 0) return;

            // Copy SDK buffer before the SDK recycles it.
            var data = new byte[size];
            Marshal.Copy(pData, data, 0, size);

            FrameReceived?.Invoke(this, new FrameEventArgs(
                data,
                pFrameInfo.nWidth,
                pFrameInfo.nHeight,
                pFrameInfo.enPixelType));
        }

        /// <summary>
        /// Create a HALCON <see cref="HObject"/> image from a raw SDK pixel buffer.
        /// Supports Mono8 and RGB8-packed formats; falls back to Mono8 for others.
        /// </summary>
        internal static HObject BuildHalconImage(
            IntPtr pData, int width, int height, MvGvspPixelType pixelType)
        {
            if (pData == IntPtr.Zero || width <= 0 || height <= 0) return null;

            try
            {
                HObject image;
                switch (pixelType)
                {
                    case MvGvspPixelType.PixelType_Gvsp_Mono8:
                        HOperatorSet.GenImage1(out image, "byte", width, height, pData);
                        break;

                    case MvGvspPixelType.PixelType_Gvsp_RGB8_Packed:
                        // HALCON GenImageInterleaved accepts packed (interleaved) RGB.
                        // WidthStepP1/P2/P3 = 0 (not used for non-planar "rgb" layout).
                        HOperatorSet.GenImageInterleaved(
                            out image, pData, "rgb", width, height,
                            -1, "byte", 0, 0, 0, 0, -1, 0);
                        break;

                    default:
                        // Treat unknown formats as single-channel byte images.
                        HOperatorSet.GenImage1(out image, "byte", width, height, pData);
                        break;
                }
                return image;
            }
            catch (HalconException ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[HikCam] BuildHalconImage failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Create a HALCON image from a managed byte array.
        /// Pins the array so HALCON can read from it safely.
        /// </summary>
        internal static HObject BuildHalconImageFromBytes(
            byte[] data, int width, int height, MvGvspPixelType pixelType)
        {
            if (data == null || data.Length == 0 || width <= 0 || height <= 0)
                return null;

            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return BuildHalconImage(
                    handle.AddrOfPinnedObject(), width, height, pixelType);
            }
            finally
            {
                handle.Free();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(HikCam));
        }
    }
}
