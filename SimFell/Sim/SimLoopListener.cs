namespace SimFell;

public abstract class SimLoopListener
{
    public SimLoop SimLoop { get; set; }
    public SimLoopListener()
    {
    }

    public void SetSimLoop(SimLoop simLoop)
    {
        SimLoop = simLoop;
        SimLoop.OnUpdate += Update;
    }

    protected abstract void Update();

    public void Stop()
    {
        if (SimLoop != null)
            SimLoop.OnUpdate -= Update;
    }

    ~SimLoopListener()
    {
        if (SimLoop != null)
            SimLoop.OnUpdate -= Update;
    }
}