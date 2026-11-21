using UnityEngine;

public partial class Corridor
{
    const float ZoomHold = .26f;
    const float ZoomDuration = 1.25f;
    float zoomLead = -1f, zoomAge = -1f, zoomFrom;
    AudioSource hiss;
    AudioClip hissClip, beepClip;
    int lastCountdownSecond;
    readonly float[] alarmPhase = new float[2];

    void QueueDetailZoom()
    {
        if (nextGain >= gain*1.07f && zoomLead<0f)
            zoomLead = .24f;
    }

    bool AdvanceDetailZoom(float delta)
    {
        if (zoomAge<0)
        {
            if (zoomLead<0) return false;
            zoomLead -= delta;
            if (zoomLead>0) return false;
            zoomLead = -1;
            if (nextGain<gain*1.07f) return false;
            zoomFrom = gain;
            zoomAge = 0;
            launch.SetEnabled(false);
        }
        zoomAge += delta;
        gain = Mathf.SmoothStep(zoomFrom,nextGain,(zoomAge-ZoomHold)/ZoomDuration);
        if (zoomAge>=ZoomHold+ZoomDuration)
        {
            gain = nextGain;
            zoomAge = -1;
        }
        return true;
    }

    void CreateTensionAudio()
    {
        hiss = gameObject.AddComponent<AudioSource>();
        hiss.playOnAwake = false;
        hiss.loop = true;
        hissClip = MakeHiss();
        beepClip = MakeTensionClip("Countdown beep",.095f,t =>
            Mathf.Sin(2f*Mathf.PI*880f*t)*Mathf.Min(1f,t*320f)*Mathf.Exp(-25f*t));
        hiss.clip = hissClip;
    }

    static AudioClip MakeTensionClip(string name,float duration,System.Func<float,float> sample)
    {
        const int rate = 24000;
        var data = new float[Mathf.RoundToInt(duration*rate)];
        for (int i=0;i<data.Length;i++) data[i] = sample((float)i/rate);
        var clip = AudioClip.Create(name,data.Length,1,rate,false);
        clip.SetData(data,0);
        return clip;
    }

    static AudioClip MakeHiss()
    {
        var random = new System.Random(412);
        float low = 0;
        return MakeTensionClip("Approaching wave hiss",2f,t => {
            float noise = (float)(random.NextDouble()*2-1);
            low += .13f*(noise-low);
            float fade = Mathf.Min(1f,Mathf.Min(t,2f-t)*90f);
            return (noise-low)*fade;
        });
    }

    void ResetTension()
    {
        zoomLead = zoomAge = -1f;
        lastCountdownSecond = 31;
        alarmPhase[0] = .92f;
        alarmPhase[1] = .42f;
        hiss.volume = .0024f;
        hiss.Stop();
        hiss.Play();
    }

    void UpdateTension(float delta)
    {
        if (stage==Stage.Reveal) return;
        float approach = Mathf.Clamp01(elapsed/LevelDuration);
        hiss.volume = Mathf.Lerp(.0024f,.0504f,approach);
        int remaining = Mathf.CeilToInt(LevelDuration-elapsed);
        if (remaining<=30 && remaining>0 && remaining<lastCountdownSecond)
        {
            lastCountdownSecond = remaining;
            effects.PlayOneShot(beepClip,.23f);
        }
        float flashesPerSecond = Mathf.Lerp(.75f,3.2f,1f-corridorMood);
        for (int player=0;player<(secondJoined ? 2 : 1);player++)
            alarmPhase[player] = Mathf.Repeat(alarmPhase[player]+flashesPerSecond*delta,1f);
    }

    void DrawAlarmLamp(int player,Rect portraitArea)
    {
        const int radius = 6;
        float pixel = Mathf.Max(2f,portraitArea.width/38f);
        float x = Mathf.Round(portraitArea.center.x-(radius+.5f)*pixel);
        float y = Mathf.Round(portraitArea.yMax+10f);
        float brightness = alarmPhase[player]<.34f ? 1f : 0f;
        Color lens = Color.Lerp(new Color(.24f,.035f,.05f),new Color(1f,.16f,.085f),brightness);
        for (int row=-radius;row<=radius;row++)
        {
            int width = Mathf.FloorToInt(Mathf.Sqrt(radius*radius-row*row));
            Fill(new Rect(x+(radius-width)*pixel,y+(row+radius)*pixel,(2*width+1)*pixel,pixel),
                new Color(.13f,.18f,.23f));
            if (width>1)
                Fill(new Rect(x+(radius-width+1)*pixel,y+(row+radius)*pixel,(2*width-1)*pixel,pixel),lens);
        }
        Fill(new Rect(x+4*pixel,y+3*pixel,3*pixel,2*pixel),
            Color.Lerp(new Color(.43f,.13f,.13f),new Color(1f,.78f,.51f),brightness));
    }

    void DestroyTensionAudio()
    {
        Destroy(hissClip);
        Destroy(beepClip);
    }
}
