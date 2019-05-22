using Entitas;
using Entitas.Unity;
using UnityEngine;

public class UnityView : MonoBehaviour, IView, IDestroyedListener
{
    private GameEntity _entity;

    public void OnDestroyed(GameEntity entity)
    {
#if UNITY_EDITOR
        gameObject.Unlink();
#endif
        Destroy(gameObject);
    }

    public void InitializeView(IEntity entity)
    {
        _entity = (GameEntity) entity;
        _entity.AddDestroyedListener(this);
#if UNITY_EDITOR
        gameObject.Link(entity);
#endif
    }
}