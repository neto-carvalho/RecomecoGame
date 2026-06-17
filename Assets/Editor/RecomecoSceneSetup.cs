#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class RecomecoSceneSetup
{
    const string MenuRoot = "Recomeco/";

    [MenuItem(MenuRoot + "Criar InventoryPanel + slots (usa objeto com InventoryUI)")]
    static void CreateInventoryPanelAndSlots()
    {
        var invUI = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponent<InventoryUI>()
            : null;
        if (invUI == null)
            invUI = Object.FindFirstObjectByType<InventoryUI>();

        if (invUI == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não achei InventoryUI.\n\n1) Selecione o GameObject que tem o script InventoryUI (ex.: sob o Canvas)\n   ou\n2) Garanta que existe um na cena aberta.",
                "OK");
            return;
        }

        if (invUI.inventoryPanel != null)
        {
            if (!EditorUtility.DisplayDialog("Recomeco",
                    "InventoryPanel já está atribuído. Criar outro mesmo assim?",
                    "Sim", "Cancelar"))
                return;
        }

        var parent = invUI.transform as RectTransform;
        if (parent == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "InventoryUI precisa estar em um objeto com RectTransform (filho do Canvas).", "OK");
            return;
        }

        var existing = parent.Find("InventoryPanel");
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Recomeco",
                    "Já existe um filho chamado InventoryPanel. Apagar e recriar?",
                    "Sim", "Não"))
                return;
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        var panel = new GameObject("InventoryPanel");
        Undo.RegisterCreatedObjectUndo(panel, "Create InventoryPanel");
        panel.transform.SetParent(parent, false);

        var rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(420, 280);
        rt.anchoredPosition = Vector2.zero;

        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.12f, 0.12f, 0.15f, 0.96f);

        var grid = panel.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(80, 80);
        grid.spacing = new Vector2(6, 6);
        grid.padding = new RectOffset(12, 12, 12, 12);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;
        grid.childAlignment = TextAnchor.UpperLeft;

        for (int i = 0; i < 12; i++)
            CreateSlot(panel.transform, i);

        panel.SetActive(false);

        Undo.RecordObject(invUI, "Assign Inventory Panel");
        invUI.inventoryPanel = panel;
        EditorUtility.SetDirty(invUI);

        EditorUtility.DisplayDialog("Recomeco",
            "Pronto.\n\n• O painel InventoryPanel foi criado como filho do objeto com InventoryUI.\n• O campo Inventory Panel foi preenchido.\n• Tab ou I abre o inventário.\n\nSalve a cena (Ctrl+S).",
            "OK");
    }

    static void CreateSlot(Transform parent, int index)
    {
        string name = index == 0 ? "Slot" : $"Slot ({index})";
        var slot = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(slot, "Create Slot");
        slot.transform.SetParent(parent, false);

        var rt = slot.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 80);

        var bg = slot.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.35f, 1f);

        slot.AddComponent<SlotUI>();

        var iconGo = new GameObject("Icon");
        iconGo.transform.SetParent(slot.transform, false);
        var iconRt = iconGo.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.5f);
        iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot = new Vector2(0.5f, 0.5f);
        iconRt.sizeDelta = new Vector2(56, 56);
        iconRt.anchoredPosition = Vector2.zero;
        var iconImg = iconGo.AddComponent<Image>();
        iconImg.enabled = false;

        var qtyGo = new GameObject("Quantity");
        qtyGo.transform.SetParent(slot.transform, false);
        var qtyRt = qtyGo.AddComponent<RectTransform>();
        qtyRt.anchorMin = new Vector2(1f, 0f);
        qtyRt.anchorMax = new Vector2(1f, 0f);
        qtyRt.pivot = new Vector2(1f, 0f);
        qtyRt.anchoredPosition = new Vector2(-4, 4);
        qtyRt.sizeDelta = new Vector2(30, 20);
        var tmp = qtyGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 14;
        tmp.alignment = TextAlignmentOptions.BottomRight;
        tmp.color = Color.white;
    }

    [MenuItem(MenuRoot + "Configurar InteractionUI no InteractionText")]
    static void ConfigureInteractionUI()
    {
        var texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
        TextMeshProUGUI tmp = null;
        foreach (var t in texts)
        {
            if (t.gameObject.name.IndexOf("Interaction", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                tmp = t;
                break;
            }
        }

        if (tmp == null && Selection.activeGameObject != null)
            tmp = Selection.activeGameObject.GetComponent<TextMeshProUGUI>();

        if (tmp == null)
        {
            EditorUtility.DisplayDialog("Recomeco",
                "Não achei um TextMeshProUGUI com \"Interaction\" no nome.\n\nRenomeie seu texto para InteractionText ou selecione o objeto com o TMP e rode o menu de novo.",
                "OK");
            return;
        }

        var go = tmp.gameObject;
        var inter = go.GetComponent<InteractionUI>();
        if (inter == null)
            inter = Undo.AddComponent<InteractionUI>(go);

        Undo.RecordObject(inter, "Wire InteractionUI");
        inter.interactionTextObject = go;
        inter.interactionText = tmp;
        EditorUtility.SetDirty(inter);

        go.SetActive(false);

        EditorUtility.DisplayDialog("Recomeco",
            "InteractionUI configurado neste mesmo objeto:\n• " + go.name + "\n\nEle começa desligado (mensagem só ao coletar/vender). Salve a cena.",
            "OK");
    }

    [MenuItem(MenuRoot + "Criar FerroVelho (zona de venda)")]
    static void CreateFerroVelho()
    {
        var go = new GameObject("FerroVelho");
        Undo.RegisterCreatedObjectUndo(go, "Create FerroVelho");
        if (SceneView.lastActiveSceneView != null)
            go.transform.position = SceneView.lastActiveSceneView.camera.transform.position + SceneView.lastActiveSceneView.camera.transform.forward * 5f;
        else
            go.transform.position = Vector3.zero;

        var sell = Undo.AddComponent<SellItems>(go);
        sell.itemName = "Latinha";
        sell.pricePerUnit = 2;
        sell.sellDistance = 4f;
        sell.messageNear = "Aperte E para vender no ferro velho";

        EditorUtility.DisplayDialog("Recomeco",
            "Zona de venda criada nesta cena.\n\nPara cena separada (2+ cenas): use Recomeco → Cenas → Criar cena FerroVelho e Portal para FerroVelho.\n\nMova o objeto e ajuste Sell Distance.",
            "OK");
        Selection.activeGameObject = go;
    }

    [MenuItem(MenuRoot + "Mover EventSystem para a raiz da cena")]
    static void MoveEventSystemToRoot()
    {
        var es = Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "Não há EventSystem na cena.", "OK");
            return;
        }

        if (es.transform.parent == null)
        {
            EditorUtility.DisplayDialog("Recomeco", "EventSystem já está na raiz.", "OK");
            return;
        }

        Undo.SetTransformParent(es.transform, null, "Move EventSystem to root");
        EditorUtility.DisplayDialog("Recomeco", "EventSystem movido para a raiz da Hierarchy.", "OK");
    }
}
#endif
