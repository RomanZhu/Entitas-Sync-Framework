public abstract class Service
{
    protected readonly Contexts _contexts;

    public Service(Contexts contexts)
    {
        _contexts = contexts;
    }
}