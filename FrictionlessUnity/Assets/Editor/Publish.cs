using UnityEngine;
using UnityEditor;
using System.Collections;

public static class Publish
{
	[MenuItem("Frictionless/Publish Package")]
	public static void PublishPackage()
	{
		const string exportFilename = "../Frictionless.unitypackage";
		AssetDatabase.ExportPackage("Assets/Frictionless", exportFilename, ExportPackageOptions.Default);
		Debug.Log ("Exported " + exportFilename);
	}
}
