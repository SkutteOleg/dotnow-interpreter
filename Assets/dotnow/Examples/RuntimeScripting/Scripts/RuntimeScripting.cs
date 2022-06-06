#if ROSLYNCSHARP
using System.Collections.Generic;
using System.IO;
using dotnow.Interop;
using RoslynCSharp;
using UnityEngine;

namespace dotnow.Examples.RuntimeScripting
{
    public class RuntimeScripting : MonoBehaviour
    {
        private ScriptProxy activeScript;
        private ScriptDomain domain;

        public AssemblyReferenceAsset[] assemblyReferences;

        [TextArea(5, 50)] public string cSharpSource =
            @"using UnityEngine;

public class TestClass : MonoBehaviour {

	void Start()
	{        

	}
}";

        public bool interpreted = true;
        public bool saveAssemblyImage;

        public void Start()
        {
            domain = ScriptDomain.CreateDomain("RuntimeCode", true);
            foreach (AssemblyReferenceAsset reference in assemblyReferences)
                domain.RoslynCompilerService.ReferenceAssemblies.Add(reference);
        }

        public void RunScript()
        {
            var assembly = interpreted ? domain.CompileAndLoadSourceInterpreted(cSharpSource, ScriptSecurityMode.UseSettings, assemblyReferences) : domain.CompileAndLoadSource(cSharpSource, ScriptSecurityMode.UseSettings, assemblyReferences);

            if (saveAssemblyImage)
                File.WriteAllBytes($"{Application.dataPath}/dotnow/Examples/RuntimeScripting/AssemblyImages/{assembly.Name}.dll.txt", assembly.AssemblyImage);
            ScriptType type = assembly.MainType;


            if (type != null)
            {
                if (activeScript != null)
                {
                    if (activeScript.Instance is ICLRProxy == interpreted)
                        SaveStateRecursively(interpreted ? activeScript.GetInstanceAs<ICLRProxy>(true).GetInstance() : activeScript.Instance);
                    Destroy(activeScript.GetInstanceAs<MonoBehaviour>(false));
                }

                activeScript = type.CreateInstance(gameObject);
                
                if (activeScript.Instance is ICLRProxy == interpreted)
                    LoadStateRecursively(interpreted ? activeScript.GetInstanceAs<ICLRProxy>(true).GetInstance() : activeScript.Instance);

                state.Clear();
            }
        }

        private string path;
        private readonly Dictionary<string, object> state = new Dictionary<string, object>();

        private bool SaveStateRecursively(object obj)
        {
            if (obj == null)
                return false;

            foreach (var field in interpreted ? ((CLRInstance)obj).Type.GetFields() : obj.GetType().GetFields())
            {
                string oldPath = path;
                path += string.Format(".{0}", field.Name);

                if (field.FieldType.IsCLRType())
                    state[path] = SaveStateRecursively((CLRInstance)field.GetValue(obj));
                else
                    state[path] = field.GetValue(obj);

                path = oldPath;
            }

            return true;
        }

        private void LoadStateRecursively(object obj)
        {
            foreach (var field in interpreted ? ((CLRInstance)obj).Type.GetFields() : obj.GetType().GetFields())
            {
                string oldPath = path;
                path += string.Format(".{0}", field.Name);

                if (!state.ContainsKey(path))
                {
                    path = oldPath;
                    continue;
                }

                if (field.FieldType.IsCLRType())
                {
                    if ((bool)state[path])
                    {
                        CLRInstance instance = (CLRInstance)AppDomain.Active.CreateInstance((CLRType)field.FieldType);
                        field.SetValue(obj, instance);
                        LoadStateRecursively(instance);
                    }
                }
                else if (field.FieldType.IsInstanceOfType(state[path]))
                    field.SetValue(obj, state[path]);

                path = oldPath;
            }
        }
    }
}
#endif