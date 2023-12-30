namespace SmartHome.Infrastructure.CatScale;

public interface ICatScaleSensor : IDisposable
{
    event PooCountChangedEventHandler PooCountChanged;
    
    void Start();
}

public delegate void PooCountChangedEventHandler(object sender, PooCountChangedEventArgs eventArgs);

public class PooCountChangedEventArgs : EventArgs
{
    public int PooCount { get; }

    public PooCountChangedEventArgs(int pooCount)
    {
        PooCount = pooCount;
    }
}