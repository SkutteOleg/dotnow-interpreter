#if ROSLYNCSHARP
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
                    DestroyImmediate(activeScript.GetInstanceAs<MonoBehaviour>(false));
                
                activeScript = type.CreateInstance(gameObject);
            }
        }
    }
}
#endif