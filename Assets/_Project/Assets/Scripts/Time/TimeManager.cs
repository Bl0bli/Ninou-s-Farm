using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static Action OnMinuteChanged;
    public static Action OnHourChanged;
    
    public static Action OnSunrise;
    public static Action OnMorning;
    public static Action OnAfternoon;
    public static Action OnSunset;
    public static Action OnNight;
    
    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    [SerializeField] private float _minuteToRealTime = 0.5f;

    private float _timer;
    
    void Start()
    {
        Minute = 0;
        Hour = 8;
        _timer = _minuteToRealTime;
    }

    void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0)
        {
            Minute++;
            OnMinuteChanged?.Invoke();
            
            if(Minute >= 60)
            {
                Minute = 0;
                Hour++;

                if (Hour >= 24) Hour = 0;

                OnHourChanged?.Invoke();
                CheckDayTime();
            }

            _timer = _minuteToRealTime;
        }
    }

    private void CheckDayTime()
    {
        if (Hour == 6)
        {
            OnSunrise?.Invoke();
        }
        else if (Hour == 8)
        {
            OnMorning?.Invoke();
        }
        else if (Hour == 12)
        {
            OnAfternoon?.Invoke();
        }
        else if (Hour == 18)
        {
            OnSunset?.Invoke();
        }
        else if (Hour == 20)
        {
            OnNight?.Invoke();
        }
    }
}
