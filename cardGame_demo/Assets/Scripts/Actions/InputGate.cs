using UnityEngine;

public class InputGate
{
    readonly float _debounce;
    float _lastClick = -999f;

    public InputGate(float debounceSeconds) { _debounce = Mathf.Max(0f, debounceSeconds); }

    public bool AllowClick()
    {
        float now = Time.unscaledTime;
        if (now - _lastClick < _debounce) return false;
        _lastClick = now;
        return true;
    }
}
