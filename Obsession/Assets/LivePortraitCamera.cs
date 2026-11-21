using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

public sealed class LivePortraitCamera : IDisposable
{
    const int Size = 96;
    const int MarkerBytes = 8;
    readonly object sync = new object();
    readonly byte[] pixels = new byte[Size * Size * 3];
    readonly byte[][] markers = { new byte[MarkerBytes], new byte[MarkerBytes] };
    readonly Texture2D[] textures = {
        new Texture2D(Size,Size,TextureFormat.RGB24,false),
        new Texture2D(Size,Size,TextureFormat.RGB24,false)
    };
    Process process;
    readonly byte[][] latest = new byte[2][];
    bool disposed;
    readonly bool[] hasFace = new bool[2];
    volatile bool cameraOpen;

    public void Start()
    {
        var worker = Resources.Load<TextAsset>("PortraitCameraWorker");
        if (worker == null) return;
        string configured = Environment.GetEnvironmentVariable("OBSESSION_CAMERA_PYTHON");
        string local = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "anaconda3","envs","neat-snake","bin","python");
        string python = string.IsNullOrWhiteSpace(configured)
            ? (File.Exists(local) ? local : "python3") : configured;
        var start = new ProcessStartInfo(python) {
            UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true,
            CreateNoWindow = true
        };
        start.ArgumentList.Add("-u");
        start.ArgumentList.Add("-c");
        start.ArgumentList.Add(worker.text);
        try
        {
            process = Process.Start(start);
            if (process == null) return;
            process.ErrorDataReceived += (_, __) => { };
            process.BeginErrorReadLine();
            var output = process.StandardOutput.BaseStream;
            Task.Run(() => ReadFrames(output));
        }
        catch (System.ComponentModel.Win32Exception) { }
    }

    void ReadFrames(Stream output)
    {
        try
        {
            while (true)
            {
                var frame = new byte[1 + MarkerBytes + pixels.Length];
                int offset = 0;
                while (offset < frame.Length)
                {
                    int count = output.Read(frame,offset,frame.Length-offset);
                    if (count == 0) return;
                    offset += count;
                }
                if (frame[0] == 255)
                {
                    cameraOpen = true;
                    continue;
                }
                lock (sync)
                {
                    if (disposed) return;
                    if (frame[0]<latest.Length) latest[frame[0]] = frame;
                }
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    public bool Draw(int player,Rect area,float strain,bool blink,bool impact)
    {
        byte[] frame;
        lock (sync) { frame = latest[player]; latest[player] = null; }
        if (frame != null)
        {
            Buffer.BlockCopy(frame,1,markers[player],0,MarkerBytes);
            Buffer.BlockCopy(frame,1+MarkerBytes,pixels,0,pixels.Length);
            textures[player].LoadRawTextureData(pixels);
            textures[player].Apply();
            hasFace[player] = true;
        }
        if (!cameraOpen && (process == null || process.HasExited)) return false;
        if (!hasFace[player])
        {
            Color previous = GUI.color;
            GUI.color = new Color(.035f,.055f,.065f);
            GUI.DrawTexture(area,Texture2D.whiteTexture);
            GUI.color = previous;
            var centered = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
            GUI.Label(area,"Looking for face",centered);
            return true;
        }

        Color original = GUI.color;
        GUI.DrawTexture(area,textures[player]);
        GUI.color = new Color(.68f,.025f,.08f,strain*.23f);
        GUI.DrawTexture(area,Texture2D.whiteTexture);
        if (strain>.24f) Mark(markers[player],area,0,3,10,new Color(.52f,.01f,.045f,strain*.85f));
        if (strain>.48f) Mark(markers[player],area,2,5,14,new Color(.61f,.015f,.045f,strain*.9f));
        if (strain>.72f) Mark(markers[player],area,6,8,4,new Color(.45f,.005f,.025f,strain*.95f));
        if (blink) Mark(markers[player],area,4,9,2,new Color(.19f,.035f,.045f,.82f));
        if (impact) Mark(markers[player],area,6,10,3,new Color(1f,.79f,.57f,.62f));
        GUI.color = original;
        return true;
    }

    void Mark(byte[] markers,Rect area,int marker,int width,int height,Color color)
    {
        float unit = area.width/Size;
        Rect mark = new Rect(area.x+(markers[marker]-width*.5f)*unit,
            area.y+markers[marker+1]*unit,width*unit,height*unit);
        Rect clipped = Rect.MinMaxRect(Mathf.Max(mark.xMin,area.xMin),Mathf.Max(mark.yMin,area.yMin),
            Mathf.Min(mark.xMax,area.xMax),Mathf.Min(mark.yMax,area.yMax));
        GUI.color = color;
        GUI.DrawTexture(clipped,Texture2D.whiteTexture);
    }

    public void Dispose()
    {
        lock (sync) { disposed = true; Array.Clear(latest,0,latest.Length); }
        if (process != null)
        {
            try { if (!process.HasExited) process.Kill(); }
            catch (InvalidOperationException) { }
            process.Dispose();
        }
        foreach (var texture in textures) UnityEngine.Object.Destroy(texture);
    }
}
