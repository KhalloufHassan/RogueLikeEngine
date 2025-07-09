using System;
using System.Collections.Generic;
using System.Linq;
using RogueLikeEngine.Optimization.Pooling;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class Pool<T> : ScriptableObject,IPool where T : Component, IPoolObject
{
    public T prefab;
    
    public int defaultCapacity = 10;
    public int maxSize = 2000;
    public bool disposeAllActiveOnSceneReload = true;
    
    [NonSerialized] private ObjectPool<T> _pool;
    private Transform transform;
    private HashSet<T> m_activeObjects;

    private void InitializePool()
    {
        _pool = new ObjectPool<T>(OnCreate, OnGet, OnReleased, _=> {  }, true, defaultCapacity, maxSize);
        transform = new GameObject($"Pool_of_{typeof(T).Name}").transform;
        transform.position = Vector3.zero;
        if(disposeAllActiveOnSceneReload) m_activeObjects = new HashSet<T>();
        
        DontDestroyOnLoad(transform.gameObject);
    }

    public T Request()
    {
        if(_pool == null)
            InitializePool();
        T obj = _pool?.Get();
        if(obj)
            obj.IsDisposed = false;
        if (disposeAllActiveOnSceneReload) m_activeObjects.Add(obj);
        return obj;
    }
    
    public void ReturnToPool(IPoolObject obj)
    {
        T t = obj as T;
        if(t?.IsDisposed ?? false) return;
        if (t != null)
        {
            t.IsDisposed = true;
            t.transform.SetParent(transform,false);
        }
        if (disposeAllActiveOnSceneReload) m_activeObjects.Remove(t);
        _pool?.Release(t);
    }


    #region Unity Pool Implementation

    private static void OnReleased(T obj)
    {
        obj.gameObject.SetActive(false);
        obj.OnDisposed();
    }

    private static void OnGet(T obj)
    {
        obj.gameObject.SetActive(true);
        obj.OnRequested();
    }

    private T OnCreate()
    {
        T instance = Instantiate(prefab,transform);
        instance.gameObject.SetActive(false);
        instance.transform.SetParent(transform);
        instance.ParentPool = this;
        return instance;
    }

    private void DisposeAllActiveOnSceneReload(Scene arg0, LoadSceneMode loadSceneMode)
    {
        if (m_activeObjects?.Count > 0)
        {
            foreach (T poolObject in m_activeObjects.ToList())
            {
                ReturnToPool(poolObject);
            }
        }
    }

    #endregion
    
    private void OnEnable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged += HandleOnPlayModeChanged;
#endif
        if(disposeAllActiveOnSceneReload)
            SceneManager.sceneLoaded += DisposeAllActiveOnSceneReload;
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.playModeStateChanged -= HandleOnPlayModeChanged;
#endif
        if(disposeAllActiveOnSceneReload)
            SceneManager.sceneLoaded -= DisposeAllActiveOnSceneReload;
    }
    
    #region Editor Only

    /*
     * The only reason we have this block here, is to account for editor with disable domain reload
     * The pool list will still be there while the game objets inside it are destroyed causing errors
     */
#if UNITY_EDITOR
    private void HandleOnPlayModeChanged(PlayModeStateChange obj)
    {
        if (obj == PlayModeStateChange.ExitingPlayMode && _pool != null)
        {
            _pool.Clear();
            _pool = null;
        }
    }
#endif

    #endregion
}
