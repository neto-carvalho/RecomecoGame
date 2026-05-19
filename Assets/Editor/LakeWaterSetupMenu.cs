#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LakeWaterSetupMenu
{
    [MenuItem("Recomeco/Água/Configurar Lake_Water (efeito subaquático)")]
    static void SetupLakeWater()
    {
        var lake = Selection.activeGameObject;
        if (lake == null)
            lake = GameObject.Find("Lake_Water");

        if (lake == null)
        {
            EditorUtility.DisplayDialog(
                "Lago",
                "Seleciona o objeto Lake_Water na Hierarchy ou cria um plano com esse nome.",
                "OK");
            return;
        }

        var zone = lake.GetComponent<LakeWaterZone>();
        if (zone == null)
            zone = lake.AddComponent<LakeWaterZone>();

        var triggerChild = lake.transform.Find("UnderwaterTrigger");
        GameObject triggerGo;
        if (triggerChild == null)
        {
            triggerGo = new GameObject("UnderwaterTrigger");
            triggerGo.transform.SetParent(lake.transform, false);
            triggerGo.transform.localPosition = new Vector3(0f, -2.5f, 0f);
            triggerGo.transform.localRotation = Quaternion.identity;
            triggerGo.transform.localScale = Vector3.one;
        }
        else
        {
            triggerGo = triggerChild.gameObject;
        }

        var box = triggerGo.GetComponent<BoxCollider>();
        if (box == null)
            box = triggerGo.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(10f, 5f, 10f);

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var cam = player.GetComponentInChildren<Camera>();
            if (cam != null && cam.GetComponent<UnderwaterCameraEffect>() == null)
                cam.gameObject.AddComponent<UnderwaterCameraEffect>();
        }

        EditorUtility.DisplayDialog(
            "Lago configurado",
            "Adicionado LakeWaterZone + trigger.\n\n" +
            "1. Ajusta a caixa UnderwaterTrigger para cobrir o buraco do lago.\n" +
            "2. Confirma que Lake_Water está na altura da superfície (Y).\n" +
            "3. Play: ao mergulhar (câmara abaixo do plano), a visão fica azul com névoa.\n\n" +
            "Ativa Fog em Edit → Project Settings → Graphics (URP) ou Lighting se a névoa não aparecer.",
            "OK");

        Selection.activeGameObject = lake;
    }

    [MenuItem("Recomeco/Água/Configurar Lake_Water (efeito subaquático)", true)]
    static bool SetupLakeWaterValidate() =>
        Selection.activeGameObject != null || GameObject.Find("Lake_Water") != null;
}
#endif
