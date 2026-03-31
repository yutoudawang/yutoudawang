using System;
using System.Diagnostics;
using HalconDotNet;

namespace CameraApp
{
    /// <summary>
    /// Shared display and image-processing utility methods used across the project.
    /// </summary>
    public static class Tools
    {
        // ── Display helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Display any HALCON object (image, region, XLD …) in the given window.
        /// Silently swallows HALCON errors so a corrupt object never crashes the UI.
        /// </summary>
        /// <param name="obj">HALCON object to display (may be <c>null</c>).</param>
        /// <param name="window">Target Halcon window handle (may be <c>null</c>).</param>
        public static void DispObj(HObject obj, HWindow window)
        {
            if (obj == null || window == null) return;
            try
            {
                HOperatorSet.DispObj(obj, window);
            }
            catch (HalconException ex)
            {
                Debug.WriteLine($"[Tools.DispObj] {ex.Message}");
            }
        }

        /// <summary>
        /// Set the display viewport so the entire image fills the window, clear
        /// the window, and apply a standard drawing style.
        /// </summary>
        /// <param name="window">HALCON window handle.</param>
        /// <param name="width">Image width in pixels.</param>
        /// <param name="height">Image height in pixels.</param>
        public static void SetupWindow(HWindow window, int width, int height)
        {
            if (window == null) return;
            try
            {
                if (width > 0 && height > 0)
                    HOperatorSet.SetPart(window, 0, 0, height - 1, width - 1);

                HOperatorSet.ClearWindow(window);
                HOperatorSet.SetDraw(window, "margin");
                HOperatorSet.SetColor(window, "green");
                HOperatorSet.SetLineWidth(window, 2);
            }
            catch (HalconException ex)
            {
                Debug.WriteLine($"[Tools.SetupWindow] {ex.Message}");
            }
        }

        /// <summary>
        /// Convenience overload: set up window using dimensions reported in a
        /// <see cref="FrameEventArgs"/> received from a camera callback.
        /// </summary>
        public static void SetupWindow(HWindow window, FrameEventArgs frame)
        {
            if (frame != null)
                SetupWindow(window, frame.Width, frame.Height);
        }
    }
}
