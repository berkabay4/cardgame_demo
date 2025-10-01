using UnityEditor;
using OneLine;
#if !UNITY_6000_0_OR_NEWER
namespace OneLine.Examples {
[CustomPropertyDrawer(typeof(OneLineDrawerExample.RootField))]
public class OneLineDrawerExampleEditor : OneLinePropertyDrawer {
}
}
#endif  