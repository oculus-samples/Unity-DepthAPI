using UnityEngine;

public static class PassthroughIntrinsicsChecker
{
    public static bool IntrinsicsNeedScaling(
        PassthroughCameraSamples.PassthroughCameraEye eye,
        WebCamTexture wct,
        out string report)
    {
        report = "";
        if (wct == null || wct.width <= 0 || wct.height <= 0)
        {
            report = "WebCamTexture not ready.";
            return false;
        }

        // 1) Base intrinsics from Camera2 (calibrated at max sensor resolution)
        var baseIntr = PassthroughCameraSamples.PassthroughCameraUtils.GetCameraIntrinsics(eye);

        // 2) Adjust the stream resolution for the video rotation (90/270 swaps W/H)
        var rotated = (wct.videoRotationAngle % 180 != 0)
            ? new Vector2Int(wct.height, wct.width)
            : new Vector2Int(wct.width, wct.height);

        // 3) Compare resolutions (exact)
        bool sameRes = (rotated == baseIntr.Resolution);

        // 4) Compute scaling factors and aspect drift
        float sx = (float)rotated.x / baseIntr.Resolution.x;
        float sy = (float)rotated.y / baseIntr.Resolution.y;
        float aspectDrift = Mathf.Abs((rotated.x / (float)rotated.y) - (baseIntr.Resolution.x / (float)baseIntr.Resolution.y));

        // 5) Horizontal FoV sanity-check: if we use unscaled intrinsics on the stream size,
        //    the FoV we compute should match the FoV computed at the calibration size.
        float HFoV(Vector2Int res, Vector2 focal, Vector2 principal)
        {
            // rays through left/right midpoints using provided intrinsics
            Vector3 dirL = new Vector3((0 - principal.x) / focal.x,
                                       (res.y * 0.5f - principal.y) / focal.y, 1f);
            Vector3 dirR = new Vector3(((res.x - 1) - principal.x) / focal.x,
                                       (res.y * 0.5f - principal.y) / focal.y, 1f);
            return Vector3.Angle(dirL, dirR);
        }

        float hFov_at_calib = HFoV(baseIntr.Resolution, baseIntr.FocalLength, baseIntr.PrincipalPoint);
        float hFov_unscaled_on_stream = HFoV(rotated, baseIntr.FocalLength, baseIntr.PrincipalPoint);
        float fovErrorDeg = Mathf.Abs(hFov_unscaled_on_stream - hFov_at_calib);

        // Heuristics:
        // - if resolutions differ or aspect drift is noticeable, we need scaling
        // - if FoV error > ~0.1 deg, we need scaling
        bool needsScaling = !sameRes || aspectDrift > 1e-4f || fovErrorDeg > 0.1f;

        report =
            $"[Intrinsics Check]\n" +
            $"- WebCamTexture: {wct.width}x{wct.height}, rotation:{wct.videoRotationAngle}, mirrored:{wct.videoVerticallyMirrored}\n" +
            $"- Stream (post-rotation): {rotated.x}x{rotated.y}\n" +
            $"- Calib (Camera2)       : {baseIntr.Resolution.x}x{baseIntr.Resolution.y}\n" +
            $"- Scale (sx, sy)        : ({sx:F6}, {sy:F6}), aspect drift: {aspectDrift:F6}\n" +
            $"- HFoV@calib            : {hFov_at_calib:F4}°\n" +
            $"- HFoV@stream (unscaled): {hFov_unscaled_on_stream:F4}°  -> Δ={fovErrorDeg:F4}°\n" +
            $"- Verdict               : {(needsScaling ? "INTRINSICS NEED SCALING" : "intrinsics OK")}";

        return needsScaling;
    }
}
