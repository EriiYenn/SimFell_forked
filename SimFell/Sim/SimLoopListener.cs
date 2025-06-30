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
        SimLoop.OnUpdate -= Update;
    }

    ~SimLoopListener()
    {
        SimLoop.OnUpdate -= Update;
    }
}