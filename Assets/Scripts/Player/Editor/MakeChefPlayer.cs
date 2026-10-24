using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>One-click: turns CozyChefCharacter in the open scene into the first-person player.</summary>
public static class MakeChefPlayer
{
    [MenuItem("Tools/Character/Make Chef The Player")]
    public static void Run()
    {
        GameObject chef = GameObject.Find("CozyChefCharacter");
        Camera cam = Camera.main;
        if (chef == null || cam == null)
        {
            EditorUtility.DisplayDialog("Make Chef The Player", "Need a 'CozyChefCharacter' object and a Main Camera in the open scene.", "OK");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(chef, "Make Chef The Player");

        // TryGetComponent, not '??': Unity's missing objects aren't real C# nulls.
        if (!chef.TryGetComponent(out CharacterController cc)) cc = Undo.AddComponent<CharacterController>(chef);
        cc.height = 1.9f;                   // model is ~1.9 m tall with the hat, pivot at the feet
        cc.radius = 0.35f;
        cc.center = new Vector3(0f, 0.95f, 0f);
        cc.stepOffset = 0.3f;

        if (chef.GetComponent<PlayerController>() == null) Undo.AddComponent<PlayerController>(chef);

        // Camera at eye height, just in front of the head so the head mesh doesn't block the view.
        Undo.SetTransformParent(cam.transform, chef.transform, "Parent camera");
        cam.transform.localPosition = new Vector3(0f, 1.7f, 0.2f);
        cam.transform.localRotation = Quaternion.identity;

        // Everything stays visible (other players must see the head). Make sure nothing is left hidden.
        foreach (var r in chef.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = ShadowCastingMode.On;

        // The scene had no floor, so the player fell forever. Add a 100x100 m one with a collider.
        if (GameObject.Find("Ground") == null)
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            Undo.RegisterCreatedObjectUndo(ground, "Create Ground");
        }

        // Stand on the floor.
        chef.transform.position = new Vector3(chef.transform.position.x, 0.1f, chef.transform.position.z);

        EditorSceneManagerMarkDirty();
        Selection.activeGameObject = chef;
        EditorUtility.DisplayDialog("Make Chef The Player", "Done. CozyChefCharacter now has CharacterController + PlayerController and the camera. Press Ctrl+S to save, then Play.", "OK");
    }

    private static void EditorSceneManagerMarkDirty() =>
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
}
