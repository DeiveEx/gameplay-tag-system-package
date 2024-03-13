using System.Collections.Generic;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem
{
    public static class GameObjectTagExtension
    {
        private static Dictionary<GameObject, GameplayTagContainer> _tagContainers = new();

        public static GameplayTagContainer GetGameplayTags(this GameObject instance)
        {   
            if (_tagContainers.TryGetValue(instance, out var container))
                return container;

            var newContainer = new GameplayTagContainer();
            _tagContainers.Add(instance, newContainer);

            return newContainer;
        }
    }
}
