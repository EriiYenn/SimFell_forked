namespace SimFell;

public class RPPM
{
    private readonly double _ppm; // Proc per minute
    private double _lastProcAttemptTime;

    public RPPM(double ppm)
    {
        _ppm = ppm;
        _lastProcAttemptTime = 0;
    }

    /// <summary>
    /// Attempts to proc based on time since last check. Returns true if proc occurs.
    /// </summary>
    public bool TryProc(Unit unit)
    {
        double currentTime = unit.SimLoop.GetElapsed();
        double deltaTime = currentTime - _lastProcAttemptTime;
        _lastProcAttemptTime = currentTime;

        if (deltaTime <= 0)
            return false;

        double procChance = (_ppm * deltaTime) / 60.0 * 100.0;

        return SimRandom.Roll(procChance);
    }

    /// <summary>
    /// Resets the internal timer (optional if needed).
    /// </summary>
    public void Reset(Unit unit, bool resetBasedOnElapsedTime = false)
    {
        _lastProcAttemptTime = resetBasedOnElapsedTime ? unit.SimLoop.GetElapsed() : 0;
    }
}