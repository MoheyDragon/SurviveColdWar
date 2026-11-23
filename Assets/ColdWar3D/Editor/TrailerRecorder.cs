using System.IO;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

// Trailer-only helper: records the Game view to an MP4 while in Play mode.
public static class TrailerRecorder
{
    static RecorderController controller;
    public static string Folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Recordings/Trailer"));

    public static string Begin(string name, int width = 1920, int height = 1080, float fps = 30f)
    {
        Stop();
        Directory.CreateDirectory(Folder);
        var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie.name = name;
        movie.Enabled = true;
        movie.EncoderSettings = new CoreEncoderSettings
        {
            Codec = CoreEncoderSettings.OutputCodec.MP4,
            EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.High,
        };
        movie.CaptureAudio = false;
        movie.ImageInputSettings = new GameViewInputSettings { OutputWidth = width, OutputHeight = height };
        movie.OutputFile = Path.Combine(Folder, name);
        settings.AddRecorderSettings(movie);
        settings.SetRecordModeToManual();
        settings.FrameRate = fps;
        settings.FrameRatePlayback = FrameRatePlayback.Constant;
        settings.CapFrameRate = true;
        controller = new RecorderController(settings);
        controller.PrepareRecording();
        bool ok = controller.StartRecording();
        return ok ? movie.OutputFile + ".mp4" : "failed";
    }

    // Records for a number of captured frames' worth of seconds, then stops by itself.
    public static string For(string name, float seconds, float fps = 30f)
    {
        string file = Begin(name, 1920, 1080, fps);
        int stopAt = Time.frameCount + Mathf.CeilToInt(seconds * fps) + 1;
        EditorApplication.CallbackFunction tick = null;
        tick = () =>
        {
            if (!Application.isPlaying || Time.frameCount >= stopAt)
            {
                Stop();
                EditorApplication.update -= tick;
            }
        };
        EditorApplication.update += tick;
        return file;
    }

    public static bool IsRecording => controller != null && controller.IsRecording();

    public static void Stop()
    {
        if (controller != null && controller.IsRecording())
            controller.StopRecording();
        controller = null;
    }
}
