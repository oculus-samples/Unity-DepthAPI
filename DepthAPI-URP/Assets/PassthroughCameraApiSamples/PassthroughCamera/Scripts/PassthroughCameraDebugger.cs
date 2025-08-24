// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace PassthroughCameraSamples
{
    [MetaCodeSample("PassthroughCameraApiSamples-PassthroughCamera")]
    public static class PassthroughCameraDebugger
    {
        public enum DebuglevelEnum
        {
            ALL,
            NONE,
            ONLY_ERROR,
            ONLY_LOG,
            ONLY_WARNING
        }

        public static DebuglevelEnum DebugLevel = DebuglevelEnum.ALL;

        /// <summary>
        /// Send debug information to Unity console based on DebugType and DebugLevel
        /// </summary>
        /// <param name="mType"></param>
        /// <param name="message"></param>
        public static void DebugMessage(LogType mType, string message)
        {
            switch (mType)
            {
                case LogType.Error:
                    if (DebugLevel is DebuglevelEnum.ALL or DebuglevelEnum.ONLY_ERROR)
                    {
                        Debug.LogError(message);
                    }
                    break;
                case LogType.Log:
                    if (DebugLevel is DebuglevelEnum.ALL or DebuglevelEnum.ONLY_LOG)
                    {
                        Debug.Log(message);
                    }
                    break;
                case LogType.Warning:
                    if (DebugLevel is DebuglevelEnum.ALL or DebuglevelEnum.ONLY_WARNING)
                    {
                        Debug.LogWarning(message);
                    }
                    break;
            }
        }
    }
}
