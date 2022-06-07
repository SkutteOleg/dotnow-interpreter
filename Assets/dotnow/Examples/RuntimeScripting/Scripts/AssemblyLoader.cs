using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using dotnow.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace dotnow.Examples.RuntimeScripting
{
    public class AssemblyLoader : MonoBehaviour
    {
        private CLRInstance activeScript;
        public TextAsset assemblyImage;

        public void RunScript()
        {
            AppDomain domain = new AppDomain();
            CLRModule module = domain.LoadModuleStream(new MemoryStream(assemblyImage.bytes), false);

            Type type = module
                .GetTypes()
                .FirstOrDefault(t => t.IsClass && t.Name != "<Module>");

            if (type != null)
            {
                if (activeScript != null)
                {

                    SaveStateRecursively(activeScript);
                    Destroy((Object)activeScript.InteropProxy);
                }
                
                activeScript = (CLRInstance)MonoBehaviourProxy.AddComponentOverride(domain, null, gameObject, new object[] { type });
                LoadStateRecursively(activeScript);
                _state.Clear();
            }
        }

        private string path;
        private Dictionary<string, object> _state = new Dictionary<string, object>();

        private bool SaveStateRecursively(CLRInstance obj)
        {
            if (obj == null)
                return false;
            
            foreach (var field in obj.Type.GetFields())
            {
                string oldPath = path;
                path += string.Format(".{0}", field.Name);

                if (field.FieldType.IsCLRType())
                    _state[path] = SaveStateRecursively((CLRInstance) field.GetValue(obj));
                else
                    _state[path] = field.GetValue(obj);

                path = oldPath;
            }

            return true;
        }

        private void LoadStateRecursively(CLRInstance obj)
        {
            foreach (var field in obj.Type.GetFields())
            {
                string oldPath = path;
                path += string.Format(".{0}", field.Name);
                
                if (!_state.ContainsKey(path))
                {
                    path = oldPath;
                    continue;
                }

                if (field.FieldType.IsCLRType())
                {
                    if ((bool) _state[path])
                    {
                        CLRInstance instance = (CLRInstance) AppDomain.Active.CreateInstance((CLRType) field.FieldType);
                        field.SetValue(obj, instance);
                        LoadStateRecursively(instance);
                    }
                }
                else if (field.FieldType.IsInstanceOfType(_state[path]))
                    field.SetValue(obj, _state[path]);

                path = oldPath;
            }
        }
    }
}