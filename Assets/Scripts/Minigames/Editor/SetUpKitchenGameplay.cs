using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// One-click: puts the player chef and the drone "Drop ingredient" pad into the open scene, next to the restaurant
/// model. The gameplay (stations, pantry, orders, freezer screen, drones) then starts by itself on Play.
/// Doesn't save: press Ctrl+S afterwards.
/// </summary>
public static class SetUpKitchenGameplay
{
    private const string ChefPrefab = "Assets/Prefabs/Chef/CozyChefCharacter.prefab";
    // Where the pad stood relative to the walk-in freezer's centre in the original scene (metres, restaurant axes).
    private static readonly Vector3 PadFromFreezer = new Vector3(-5.775f, 0f, 4.75f);

    [MenuItem("Tools/Nutrition Kitchen/Set Up Player + Gameplay")]
    public static void Run()
    {
        GameObject door = GameObject.Find("Entrance_Door_Frame");
        GameObject freezer = GameObject.Find("Walk_In_Freezer");
        if (door == null || freezer == null)
        {
            EditorUtility.DisplayDialog("Set Up Player + Gameplay", "Need the restaurant model (restaurant.obj) in the open scene.", "OK");
            return;
        }
        Transform building = door.transform.parent != null ? door.transform.parent : door.transform;
        Vector3 fwd = Vector3.ProjectOnPlane(building.forward, Vector3.up).normalized; // entrance -> kitchen

        // 1) The chef, standing just inside the entrance and facing into the restaurant.
        if (GameObject.Find("CozyChefCharacter") == null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChefPrefab);
            var chef = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            chef.name = "CozyChefCharacter";
            Bounds d = door.GetComponent<Renderer>().bounds;
            chef.transform.SetPositionAndRotation(new Vector3(d.center.x, d.min.y, d.center.z) + fwd * 2f, Quaternion.LookRotation(fwd));
            Undo.RegisterCreatedObjectUndo(chef, "Add player chef");
        }
        MakeChefPlayer.Run(); // CharacterController + PlayerController + camera (existing tool)

        // 2) The drone drop pad with its sign, outside behind the freezer's back door.
        if (GameObject.Find("Drop ingredient") == null)
        {
            Bounds f = freezer.GetComponent<Renderer>().bounds;
            Quaternion rot = Quaternion.LookRotation(fwd);
            var pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            pad.name = "Drop ingredient";
            pad.transform.SetPositionAndRotation(new Vector3(f.center.x, f.min.y + 0.015f, f.center.z) + rot * PadFromFreezer, rot);
            pad.transform.localScale = new Vector3(3.9237f, 0.2198f, 3.953f);
            var sign = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sign.name = "Cube (1)";
            sign.transform.SetParent(pad.transform, false);
            sign.transform.localPosition = new Vector3(0.3099f, 2.1078f, -0.502f);
            sign.transform.localScale = new Vector3(0.3851f, 4.5978f, 0.061f);
            Undo.RegisterCreatedObjectUndo(pad, "Add drop pad");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
