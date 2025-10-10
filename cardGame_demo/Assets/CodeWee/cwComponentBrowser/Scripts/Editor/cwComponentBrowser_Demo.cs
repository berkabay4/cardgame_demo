using UnityEditor;
using CodeWee.Editor.ComponentBrowser;

namespace MyNamespace
{
	public class cwComponentBrowser_Demo
	{

		[MenuItem("Window/CodeWee/Component Browser..")]
		public static void Open()
		{
			cwComponentBrowser window = EditorWindow.GetWindow<cwComponentBrowser>("CodeWee Component Browser");
			window.Show();
		}

	}
}
