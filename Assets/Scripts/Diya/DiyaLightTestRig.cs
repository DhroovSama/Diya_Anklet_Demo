using UnityEngine;

public class DiyaLightTestRig : MonoBehaviour
{
    [Header("Build a quick dark room to test the Diya.")]
    public bool killDirectionalLights = true;
    public bool buildRoom = true;
    public Vector3 roomSize = new Vector3(10f, 3f, 10f);
    public Color roomColor = new Color(0.06f, 0.06f, 0.06f, 1f);

    void Start()
    {
        if (killDirectionalLights)
        {
            foreach (var l in FindObjectsOfType<Light>())
                if (l.type == LightType.Directional) l.enabled = false;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = Color.black;
        }

        if (buildRoom)
        {
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = roomColor;

            // Floor
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "TestFloor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(roomSize.x / 10f, 1f, roomSize.z / 10f);
            floor.GetComponent<Renderer>().sharedMaterial = mat;

            // Ceiling
            var ceil = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ceil.name = "TestCeiling";
            ceil.transform.position = new Vector3(0f, roomSize.y, 0f);
            ceil.transform.localScale = new Vector3(roomSize.x / 10f, 1f, roomSize.z / 10f);
            ceil.transform.rotation = Quaternion.Euler(180f, 0f, 0f);
            ceil.GetComponent<Renderer>().sharedMaterial = mat;

            // Walls (4)
            MakeWall("WallNorth", new Vector3(0f, roomSize.y * 0.5f, roomSize.z * 0.5f), new Vector3(roomSize.x, roomSize.y, 0.2f), mat);
            MakeWall("WallSouth", new Vector3(0f, roomSize.y * 0.5f, -roomSize.z * 0.5f), new Vector3(roomSize.x, roomSize.y, 0.2f), mat);
            MakeWall("WallEast", new Vector3(roomSize.x * 0.5f, roomSize.y * 0.5f, 0f), new Vector3(0.2f, roomSize.y, roomSize.z), mat);
            MakeWall("WallWest", new Vector3(-roomSize.x * 0.5f, roomSize.y * 0.5f, 0f), new Vector3(0.2f, roomSize.y, roomSize.z), mat);

            // Test props
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "TestCube";
            cube.transform.position = new Vector3(1.2f, 0.5f, 1.0f);
            cube.GetComponent<Renderer>().sharedMaterial = mat;
        }
    }

    void MakeWall(string name, Vector3 pos, Vector3 size, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;
    }
}
