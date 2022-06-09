using dotnow;
using dotnow.Interop;

namespace System
{
    [UnityEngine.Scripting.Preserve]
    [CLRProxyBinding(typeof(IDisposable))]
    public class IDisposableProxy : ICLRProxy, IDisposable
    {
        // Private
        private CLRInstance instance;

        public void Dispose()
        {
#if API_NET35
            if (instance.Type.GetMethod("Dispose") != null)
                instance.Type.GetMethod("Dispose").Invoke(instance, null);
#else
            instance.Type.GetMethod("Dispose")?.Invoke(instance, null);
#endif
        }

        public void InitializeProxy(dotnow.AppDomain domain, CLRInstance instance)
        {
            this.instance = instance;
        }

        public CLRInstance GetInstance()
        {
            return instance;
        }
    }
}
