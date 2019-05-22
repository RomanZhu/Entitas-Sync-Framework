using UnityEngine;

public static class GameObjectUtility
{
    public static void InitializeObject(GameObject go, GameEntity entity, bool isServer)
    {
        var view = go.GetComponent<IView>();
        view?.InitializeView(entity);
    }
}