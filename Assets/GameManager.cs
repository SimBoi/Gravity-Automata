using Dummiesman;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using SFB;

public class GameManager : MonoBehaviour
{
    public CellularAutomata3D ca;
    public GameObject loadedObject = null;
    public GameObject LOAD;
    public GameObject GENERATE;

    public void LoadObj()
    {
        var extensions = new[] { new ExtensionFilter("Obj Files", "obj") };
        var paths = StandaloneFileBrowser.OpenFilePanel("Open File", "", extensions, true);
        loadedObject = new OBJLoader().Load(new System.Uri(paths[0]).AbsolutePath);

        Bounds bounds = new Bounds(loadedObject.transform.position, Vector3.zero);
        foreach (Renderer r in loadedObject.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
        float scale = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        loadedObject.transform.localScale *= ca.size / scale;

        LOAD.SetActive(false);
        GENERATE.SetActive(true);
    }

    public void GenerateEnvironment()
    {

    }

    public void ResetScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}