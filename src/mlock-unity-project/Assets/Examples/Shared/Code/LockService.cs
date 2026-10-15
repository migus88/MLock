using System;
using Migs.MLock.Debugging;
using Migs.MLock.Interfaces;
using UnityEngine;

namespace Migs.MLock.Examples.Shared
{
    /// <summary>
    /// A singleton implementation of the lock service.
    /// WARNING: Using singletons is generally considered a bad practice as it:
    /// 1. Makes code harder to test
    /// 2. Creates hidden dependencies
    /// 3. Makes it difficult to swap implementations
    /// 4. Can lead to global state issues
    /// 
    /// Instead, consider using dependency injection (DI) to manage your services.
    /// This is just an example for demonstration purposes.
    /// </summary>
    public abstract class LockService<TLockTags, TLockService> : MonoBehaviour, ILockService<TLockTags>
        where TLockTags : Enum 
        where TLockService : Component, ILockService<TLockTags>
    {
        private static TLockService _instance;
        private BaseLockService<TLockTags> _lockService;

        public static TLockService Instance
        {
            get
            {
                if (_instance == null && Application.isPlaying) 
                {
                    var go = new GameObject("LockService");
                    _instance = go.AddComponent<TLockService>();
                    DontDestroyOnLoad(go);
                }

                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _lockService = new BaseLockService<TLockTags>()
                .WithDebug<TLockTags, BaseLockService<TLockTags>>();
        }

        public LockSubscription<TLockTags> Subscribe(ILockable<TLockTags> lockable) => _lockService.Subscribe(lockable);
        public void Unsubscribe(ILockable<TLockTags> lockable) => _lockService.Unsubscribe(lockable);
        public LockHandle<TLockTags> Lock(TLockTags includeTags) => _lockService.Lock(includeTags);
        public LockHandle<TLockTags> LockAllExcept(TLockTags excludeTags) => _lockService.LockAllExcept(excludeTags);
        public LockHandle<TLockTags> LockAll() => _lockService.LockAll();
        public bool IsLocked(ILockable<TLockTags> lockable) => _lockService.IsLocked(lockable);
    }
}