using System.Collections.Generic;
using UnityEngine;

namespace DeiveEx.GameplayTagSystem
{
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    public class GameplayTagContainerComponent : MonoBehaviour
    {
        #region Fields

        [SerializeField, GameplayTag] private List<string> _initialTags = new();

        private bool _alreadySetup;

        #endregion

        #region Unity Events

        private void Awake()
        {
            Setup();
        }

        #endregion
        
        #region Public Methods

        public void Setup()
        {
            if(_alreadySetup)
                return;
            
            var tags = gameObject.GetGameplayTags();
            
            foreach (var initialTag in _initialTags)
            {
                tags.AddTag(initialTag);
            }

            _alreadySetup = true;
        }

        public GameplayTagContainer GetContainer()
        {
            return gameObject.GetGameplayTags();
        }
        
        #endregion
    }
}
