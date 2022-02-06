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
                    SaveState(activeScript.GetInstanceAs<MonoBehaviourProxy>(true).GetInstance());
                    DestroyImmediate(activeScript.GetInstanceAs<MonoBehaviour>(false));
                }

                activeScript = type.CreateInstance(gameObject);
                LoadState(activeScript.GetInstanceAs<MonoBehaviourProxy>(true).GetInstance());
            }
        }

        private Dictionary<string, object> _state = new Dictionary<string, object>();

        private void SaveState(CLRInstance obj)
        {
            foreach (var field in obj.Type.GetFields())
                _state[field.Name] = field.GetValue(obj);
        }

        private void LoadState(CLRInstance obj)
        {
            foreach (var field in obj.Type.GetFields())
            {
                //Ignoring interpreted types for now.
                //TODO - handle CLR types
                if (field.FieldType.IsCLRType())
                    continue;
                
                if (!_state.ContainsKey(field.Name))
                    continue;
                
                if (field.FieldType.IsInstanceOfType(_state[field.Name]))
                    field.SetValue(obj, _state[field.Name]);
            }
        }
    }
}
#endif