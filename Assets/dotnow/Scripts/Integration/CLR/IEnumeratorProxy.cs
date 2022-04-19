using dotnow;
using dotnow.Interop;

namespace System.Collections
{
    [CLRProxyBinding(typeof(IEnumerator))]
    public class IEnumeratorProxy : ICLRProxy, IEnumerator
    {
        // Private
        private CLRInstance instance;

        public object Current
        {
            get
            {
#if API_NET35
                return instance.Type.GetMethod("System.Collections.Generic.IEnumerator.get_Current") != null ? instance.Type.GetMethod("System.Collections.Generic.IEnumerator.get_Current").Invoke(instance, null) : null;
#else
                return instance.Type.GetMethod("System.Collections.Generic.IEnumerator.get_Current")?.Invoke(instance, null);
#endif
            }
        }

        public void InitializeProxy(dotnow.AppDomain domain, CLRInstance instance)
        {
            this.instance = instance;
        }

        public CLRInstance GetInstance()
        {
            return instance;
        }

        public bool MoveNext()
        {
#if API_NET35
            return instance.Type.GetMethod("MoveNext") != null ? (bool)instance.Type.GetMethod("MoveNext").Invoke(instance, null) : true;
#else
            return (bool)instance.Type.GetMethod("MoveNext")?.Invoke(instance, null);
#endif
        }

        public void Reset()
        {
#if API_NET35
            if (instance.Type.GetMethod("Reset") != null)
                instance.Type.GetMethod("Reset").Invoke(instance, null);
#else
            instance.Type.GetMethod("Reset")?.Invoke(instance, null);
#endif
        }
    }
}
