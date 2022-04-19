using dotnow;
using System;
using System.Reflection;
using UnityEngine;

namespace dotnowRuntime
{
    public class RuntimeBindingsCache
    {
        // Private
        private static object nullMatchToken = new object();

        private CLRInstance instance = null;
        private object[] proxyMemberCache = null;

        // Constructor
        public RuntimeBindingsCache(CLRInstance instance, int memberCount)
        {
            this.instance = instance;
            this.proxyMemberCache = new object[memberCount];
        }

        // Methods
#if API_NET35
        public void InvokeProxyMethod(int offset, string methodName, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            MethodInfo info = FindProxyMethodInfo(offset, methodName, flags);

            if (info != null)
                info.Invoke(instance, null);
        }
#else
        public void InvokeProxyMethod(int offset, string methodName, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            Action invoke = FindProxyMethodDelegate(offset, methodName, flags);

            if (invoke != null)
                invoke();
        }
#endif

        public object InvokeProxyMethod(int offset, string methodName, object[] args, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            MethodInfo info = FindProxyMethodInfo(offset, methodName, flags);

            if (info != null)
                return info.Invoke(instance, args);

            return null;
        }

        private MethodInfo FindProxyMethodInfo(int offset, string methodName, BindingFlags flags)
        {
            return FindProxyMethodToken(offset, methodName, flags) as MethodInfo;
        }

        private Action FindProxyMethodDelegate(int offset, string methodName, BindingFlags flags)
        {
            return FindProxyMethodToken(offset, methodName, flags) as Action;
        }

        private object FindProxyMethodToken(int offset, string methodName, BindingFlags flags)
        {
            object token = proxyMemberCache[offset];
            Debug.Log(token);

            // Check for searched
            if (token != nullMatchToken && token == null)
            {
                // try to find the method
                MethodInfo method = instance.Type.GetMethod(methodName, flags);

#if API_NET35
                // Check for found
                if (method != null)
                {
                    proxyMemberCache[offset] = method;
                    token = method;
                }
                else
                {
                    // Searched for the member and was not found
                    proxyMemberCache[offset] = nullMatchToken;
                }
#else
                // Check for found
                if(method != null)
                {
                    // Check for delegate
                    if (method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
                    {
                        // Create delegate
                        Action invoke = (Action)method.CreateDelegate(typeof(Action), instance);

                        // Override cached result with delegate which will have lower overhead
                        proxyMemberCache[offset] = invoke;
                        token = invoke;
                    }
                    else
                    {
                        token = method;
                    }
                }
                else
                {
                    // Searched for the member and was not found
                    proxyMemberCache[offset] = nullMatchToken;
                }
#endif
            }

            //// Try to find member
            //if(proxyMemberCache.TryGetValue(methodName, out token) == false)
            //{
            //    // Find the method
            //    MethodInfo method = instance.Type.GetMethod(methodName, flags);

            //    // Cache the result
            //    proxyMemberCache[methodName] = method;
            //    token = method;

            //    // Check for found
            //    if(method != null)
            //    {
            //        // Check for delegate
            //        if(method.ReturnType == typeof(void) && method.GetParameters().Length == 0)
            //        {
            //            // Create delegate
            //            Action invoke = (Action)method.CreateDelegate(typeof(Action), instance);

            //            // Override cached result with delegate which will have lower overhead
            //            proxyMemberCache[methodName] = invoke;
            //            token = invoke;
            //        }
            //    }
            //}
            return token;
        }
    }
}