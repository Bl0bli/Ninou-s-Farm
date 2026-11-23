using System;
using Godot;

/// <summary>
/// Horloge du jeu (port direct de TimeManager.cs) : une minute de jeu toutes les _minuteToRealTime secondes.
/// Les événements restent statiques, comme dans Unity : les abonnés doivent se désabonner dans _ExitTree.
/// </summary>
public partial class TimeManager : Node
{
    public static Action OnMinuteChanged;
    public static Action<float> OnHourChanged;

    public static Action OnSunrise;
    public static Action OnMorning;
    public static Action OnAfternoon;
    public static Action OnSunset;
    public static Action OnNight;

    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    [Export] private float _minuteToRealTime = 0.5f;

    private float _timer;

    public override void _Ready()
    {
        Minute = 0;
        Hour = 8;
        _timer = _minuteToRealTime;
    }

    public override void _Process(double delta)
    {
        _timer -= (float)delta;
        if (_timer > 0) return;

        Minute++;
        OnMinuteChanged?.Invoke();

        if (Minute >= 60)
        {
            Minute = 0;
            Hour++;
            if (Hour >= 24) Hour = 0;

            OnHourChanged?.Invoke(Hour + Minute / 60f);
            CheckDayTime();
        }

        _timer = _minuteToRealTime;
    }

    private static void CheckDayTime()
    {
        switch (Hour)
        {
            case 6: OnSunrise?.Invoke(); break;
            case 8: OnMorning?.Invoke(); break;
            case 12: OnAfternoon?.Invoke(); break;
            case 18: OnSunset?.Invoke(); break;
            case 20: OnNight?.Invoke(); break;
        }
    }
}
