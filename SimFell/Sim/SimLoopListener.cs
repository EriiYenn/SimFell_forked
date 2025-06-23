namespace SimFell;

public abstract class SimLoopListener
{
    public SimLoop SimLoop { get; }
    public SimLoopListener(SimLoop simLoop)
    {
        SimLoop = simLoop;
        SimLoop.OnUpdate += Update;
    }

    protected abstract void Update();

    public void Stop()
    {
        SimLoop.OnUpdate -= Update;
    }

    ~SimLoopListener()
    {
        SimLoop.OnUpdate -= Update;
    }
}