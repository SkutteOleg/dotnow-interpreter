#if ROSLYNCSHARP
using System.Collections.Generic;
using dotnow;
using UnityEngine;

namespace RoslynCSharp.Example
{
    public class RuntimeScripting : MonoBehaviour
    {
        private ScriptProxy activeScript = null;
        private ScriptDomain domain = null;

        public AssemblyReferenceAsset[] assemblyReferences;
        [TextArea(5,50)]
        public string cSharpSource =
@"using UnityEngine;

public class TestClass : MonoBehaviour {

	void Start()
	{        

	}
}";

        public void Start()
        {
            domain = ScriptDomain.CreateDomain("RuntimeCode", true);
            foreach (AssemblyReferenceAsset reference in assemblyReferences)
                domain.RoslynCompilerService.ReferenceAssemblies.Add(reference);
        }

        public void RunScript()
        {
            ScriptType type = domain.CompileAndLoadMainSourceInterpreted(cSharpSource, ScriptSecurityMode.UseSettings, assemblyReferences );

            if (type != null)
            {
                if (activeScript != null)
                {
                    SaveStateRecursively(activeScript.GetInstanceAs<MonoBehaviourProxy>(true).GetInstance());
                    DestroyImmediate(activeScript.GetInstanceAs<MonoBehaviour>(false));
                }

                activeScript = type.CreateInstance(gameObject);
                LoadStateRecursively(activeScript.GetInstanceAs<MonoBehaviourProxy>(true).GetInstance());
            }
        }

        private string _path;
        private Dictionary<string, object> _state = new Dictionary<string, object>();

        private bool SaveStateRecursively(CLRInstance obj)
        {
            if (obj == null)
                return false;
            
            foreach (var field in obj.Type.GetFields())
            {
                string oldPath = _path;
                _path += string.Format(".{0}", field.Name);

                if (field.FieldType.IsCLRType())
                    _state[_path] = SaveStateRecursively((CLRInstance) field.GetValue(obj));
                else
                    _state[_path] = field.GetValue(obj);

                _path = oldPath;
            }

            return true;
        }

        private void LoadStateRecursively(CLRInstance obj)
        {
            foreach (var field in obj.Type.GetFields())
            {
                string oldPath = _path;
                _path += string.Format(".{0}", field.Name);
                
                if (!_state.ContainsKey(_path))
                {
                    _path = oldPath;
                    continue;
                }

                if (field.FieldType.IsCLRType())
                {
                    if ((bool) _state[_path])
                    {
                        CLRInstance instance = (CLRInstance) AppDomain.Active.CreateInstance((CLRType) field.FieldType);
                        field.SetValue(obj, instance);
                        LoadStateRecursively(instance);
                    }
                }
                else if (field.FieldType.IsInstanceOfType(_state[_path]))
                    field.SetValue(obj, _state[_path]);

                _path = oldPath;
            }
        }
    }
}
#endif