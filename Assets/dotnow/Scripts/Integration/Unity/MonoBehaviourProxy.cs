#if !UNITY_DISABLE
#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_IOS || UNITY_ANDROID || UNITY_WSA || UNITY_WEBGL || UNITY_SWITCH)
using System.Reflection;
using dotnow;
using dotnow.Interop;
using dotnow.Runtime;
using dotnow.Scripts.Integration.Locomotorica;
using OlegSkutte.Utils;
using RoslynCSharp;
using AppDomain = dotnow.AppDomain;

namespace UnityEngine
{
#if API_NET35
    [CLRProxyBinding(typeof(MonoBehaviour))]
    public class MonoBehaviourProxy : MonoBehaviour, ICLRProxy
    {
        // Private
        private AppDomain domain = null;
        private CLRType instanceType = null;
        private CLRInstance instance = null;

        private RuntimeBindingsCache cache = null;

        // Methods
        public void InitializeProxy(AppDomain domain, CLRInstance instance)
        {
            this.domain = domain;
            this.instanceType = instance.Type;
            this.instance = instance;

            cache = new RuntimeBindingsCache(instance, 12);

            // Manually call awake and OnEnable since they will do nothing when called by Unity
            Awake();
            OnEnable();
        }

        public void Awake()
        {
            // When Unity calls this method, we have not yet had chance to 'InitializeProxy'. This method will be called manually when ready.
            if (domain == null)
                return;

            cache.InvokeProxyMethod(0, "Awake");
        }

        public void Start()
        {
            cache.InvokeProxyMethod(1, "Start");
        }

        public void OnDestroy()
        {
            cache.InvokeProxyMethod(2, "OnDestroy");
        }

        public void OnEnable()
        {
            // When Unity calls this method, we have not yet had chance to 'InitializeProxy'. This method will be called manually when ready.
            if (domain == null)
                return;

            cache.InvokeProxyMethod(3, "OnEnable");
        }

        public void OnDisable()
        {
            cache.InvokeProxyMethod(4, "OnDisable");
        }

        public void Update()
        {
            cache.InvokeProxyMethod(5, "Update");
        }

        public void LateUpdate()
        {
            cache.InvokeProxyMethod(6, "LateUpdate");
        }

        public void FixedUpdate()
        {
            cache.InvokeProxyMethod(7, "FixedUpdate");
        }

        public void OnCollisionEnter(Collision collision)
        {
            cache.InvokeProxyMethod(8, "OnCollisionEnter", new object[] { collision });
        }

        public void OnCollisionStay(Collision collision)
        {
            cache.InvokeProxyMethod(9, "OnCollisionStay", new object[] { collision });
        }

        public void OnCollisionExit(Collision collision)
        {
            cache.InvokeProxyMethod(10, "OnCollisionExit", new object[] { collision });
        }

        [CLRMethodBinding(typeof(GameObject), "AddComponent", typeof(Type))]
        public static object AddComponentOverride(AppDomain domain, MethodInfo overrideMethod, object instance, object[] args)
        {
            // Get instance
            GameObject go = instance as GameObject;

            // Get argument
            Type componentType = args[0] as Type;

            // Check for clr type
            if (componentType.IsCLRType() == false)
            {
                // Use default unity behaviour
                return go.AddComponent(componentType);
            }

            // Handle add component manually
            Type proxyType = domain.GetCLRProxyBindingForType(componentType.BaseType);

            // Validate type
            if (typeof(MonoBehaviour).IsAssignableFrom(proxyType) == false)
                throw new InvalidOperationException("A type deriving from mono behaviour must be provided");

            // Create proxy instance
            ICLRProxy proxyInstance = (ICLRProxy)go.AddComponent(proxyType);

            // Create clr instance
            return domain.CreateInstanceFromProxy(componentType, proxyInstance);
        }

        [CLRMethodBinding(typeof(GameObject), "GetComponent", typeof(Type))]
        public static object GetComponentOverride(AppDomain domain, MethodInfo overrideMethod, object instance, object[] args)
        {
            // Get instance
            GameObject go = instance as GameObject;

            // Get argument
            Type componentType = args[0] as Type;

            // Check for clr type
            if (componentType.IsCLRType() == false)
            {
                // Use default unity behaviour
                return go.GetComponent(componentType);
            }

            // Get proxy types
            foreach (MonoBehaviourProxy proxy in go.GetComponents<MonoBehaviourProxy>())
            {
                if (proxy.instanceType == componentType)
                {
                    return proxy.instance;
                }
            }
            return null;
        }
    }
#else
    [CLRProxyBinding(typeof(MonoBehaviour))]
    public class MonoBehaviourProxy : MonoBehaviour, ICLRProxy
    {
        private AppDomain domain = null;
        [SerializeField]
        private string assemblyInfo = null;
        [SerializeField]
        private string typeInfo = null;

        private CLRType instanceType = null;
        private CLRInstance instance = null;

        private RuntimeBindingsCache cache = null;

        // Properties
        public CLRInstance Instance
        {
            get { return instance; }
        }

        // Methods
        public void InitializeProxy(AppDomain domain, CLRInstance instance)
        {
            this.domain = domain;            
            this.instanceType = instance.Type;
            this.instance = instance;

            cache = new RuntimeBindingsCache(instance, 12);

            // Avoid instantiate loop
            this.assemblyInfo = null;
            this.typeInfo = null;

            // Manually call awake and OnEnable since they will do nothing when called by Unity
            LoadState(this.instance);
            Hook(this.instance, this);
            Awake();

            // Important - these serialized values must be set after Awake otherwise we have infinite instantiate loop
            this.assemblyInfo = instance.Type.Assembly.FullName;
            this.typeInfo = instance.Type.Name;
        }

        private void InstantiateProxy()
        {
            this.domain = ScriptDomain.Active.GetAppDomain();

            // Resolve type
            CLRType type = domain.ResolveType(assemblyInfo, typeInfo) as CLRType;

            // Check for error
            if(type == null)
            {
                Debug.LogError("Failed to resolve CLR type during instantiate! Script cannot run", this);
                enabled = false;
                return;
            }

            // Create initialized instance from this instantiated proxy
            domain.CreateInstanceFromProxy(type, this);
        }

        private string path;
        public SerializableDictionary<string, string> Json = new();
        public SerializableDictionary<string, Object> Objects = new();
        
        public bool SaveState(object obj)
        {
            if (obj == null)
                return false;

            foreach (var field in obj.IsCLRInstance() ? ((CLRInstance)obj).Type.GetFields() : obj.GetType().GetFields())
            {
                string oldPath = path;
                path += $".{field.Name}";

                if (field.FieldType.IsCLRType())
                    Json[path] = SaveState((CLRInstance)field.GetValue(obj)).ToJson();
                else
                {
                    if (typeof(Object).IsAssignableFrom(field.FieldType))
                        Objects[path] = field.GetValue(obj) as Object;
                    else
                        Json[path] = field.GetValue(obj).ToJson();
                }

                path = oldPath;
            }

            return true;
        }
        
        public void LoadState(object obj)
        {
            foreach (var field in obj.IsCLRInstance() ? ((CLRInstance)obj).Type.GetFields() : obj.GetType().GetFields())
            {
                string oldPath = path;
                path += $".{field.Name}";

                if (!Json.ContainsKey(path) && !Objects.ContainsKey(path))
                {
                    path = oldPath;
                    continue;
                }

                if (field.FieldType.IsCLRType())
                {
                    if ((bool)Json[path].FromJson(typeof(bool)))
                    {
                        var instance = (CLRInstance)ScriptDomain.Active.GetAppDomain().CreateInstance((CLRType)field.FieldType);
                        field.SetValue(obj, instance);
                        LoadState(instance);
                    }
                }
                else
                    field.SetValue(obj, typeof(Object).IsAssignableFrom(field.FieldType) ? Objects[path] : Json[path].FromJson(field.FieldType));

                path = oldPath;
            }
        }
        
        private readonly SerializableDictionary<FieldInfo, string> _fieldMap = new();
        
        private void Hook(CLRInstance clrInstance, MonoBehaviourProxy proxy)
        {
            if (clrInstance == null)
                return;
            
            foreach (var field in clrInstance.Type.GetFields())
            {
                string oldPath = path;
                path += $".{field.Name}";

                _fieldMap[field] = path;

                if (field.FieldType.IsCLRType()) 
                    Hook((CLRInstance)field.GetValue(clrInstance), proxy);

                path = oldPath;
            }

            clrInstance.OnSetFieldValue += (field, value) =>
            {
                if (value is Object obj)
                    proxy.Objects[_fieldMap[field]] = obj;
                else
                    proxy.Json[_fieldMap[field]] = value.ToJson();
            };
        }

        public void Awake()
        {
            // When Unity calls this method, we have not yet had chance to 'InitializeProxy'. This method will be called manually when ready.
            if (string.IsNullOrEmpty(typeInfo) == false)
            {
                InstantiateProxy();
            }
            else
            {
                if (domain == null || !LocomotoricaBridge.ScriptsEnabled)
                    return;

                cache.InvokeProxyMethod(0, nameof(Awake));
            }
        }

        public void Start()
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(1, nameof(Start));
        }

        public void OnDestroy()
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(2, nameof(OnDestroy));
        }

        public void OnEnable()
        {
            // When Unity calls this method, we have not yet had chance to 'InitializeProxy'. This method will be called manually when ready.
            if (domain == null || !LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(3, nameof(OnEnable));
        }

        public void OnDisable()
        {
            if (domain == null || !LocomotoricaBridge.ScriptsEnabled)
                return;
            
            cache.InvokeProxyMethod(4, nameof(OnDisable));
        }

        public void Update()
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;
            
            cache.InvokeProxyMethod(5, nameof(Update));
        }

        public void LateUpdate()
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;
            
            cache.InvokeProxyMethod(6, nameof(LateUpdate));
        }

        public void FixedUpdate()
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;
            
            cache.InvokeProxyMethod(7, nameof(FixedUpdate));
        }

        public void OnCollisionEnter(Collision collision)
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(8, nameof(OnCollisionEnter), new object[] { collision });
        }

        public void OnCollisionStay(Collision collision)
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(9, nameof(OnCollisionStay), new object[] { collision });
        }

        public void OnCollisionExit(Collision collision)
        {
            if (!LocomotoricaBridge.ScriptsEnabled)
                return;

            cache.InvokeProxyMethod(10, nameof(OnCollisionExit), new object[] { collision });
        }
    }
#endif
}
#endif
#endif