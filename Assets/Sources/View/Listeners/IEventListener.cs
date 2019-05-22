public interface IEventListener
{
    bool enabled { get; set; }
    void RegisterListeners(GameEntity entity);
}