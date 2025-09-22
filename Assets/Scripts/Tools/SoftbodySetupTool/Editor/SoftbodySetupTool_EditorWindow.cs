using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class SoftbodySetupTool_EditorWindow : EditorWindow
{
    [MenuItem("Tools/Softbody 2D/Softbody Tool (UI Toolkit)")]
    public static void ShowWindow()
    {
        var wnd = GetWindow<SoftbodySetupTool_EditorWindow>();
        wnd.titleContent = new GUIContent("Softbody 2D");
        wnd.minSize = new Vector2(360, 540);
    }

    private VisualTreeAsset _createTabUXML;
    private VisualTreeAsset _tweakTabUXML;
    private StyleSheet _uss;

    private ObjectField _sprite, _profileField, _physicsProfileField;
    private Slider _springJointDampingRatio, _springJointFrequency;
    private Toggle _hasCentralBone;
    private Button _createBtn, _applyBtn;
    private Toolbar _toolbar;
    private VisualElement _createTab, _tweakTab;
    
    private Dictionary<string, VisualElement> _toolTabs = new();
    
    public void CreateGUI()
    {
        _createTabUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Tools/SoftbodySetupTool/UI/SoftbodyToolWindow_CreateTab.uxml");
        _tweakTabUXML = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Scripts/Tools/SoftbodySetupTool/UI/SoftbodyToolWindow_TweakTab.uxml");
        
        _uss  = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Tools/SoftbodySetupTool/UI/SoftbodyToolWindow.uss");

        var createRoot = _createTabUXML.CloneTree();
        createRoot.styleSheets.Add(_uss);
        rootVisualElement.Add(createRoot);
        
        _sprite = createRoot.Q<ObjectField>("sprite");
        _physicsProfileField = createRoot.Q<ObjectField>("physicsProfile");
        _hasCentralBone = createRoot.Q<Toggle>("hasCentralBone");
        _createBtn = createRoot.Q<Button>("createBtn");
        _createBtn.clicked += OnCreateClicked;

        var tweakRoot = _tweakTabUXML.CloneTree();
        tweakRoot.styleSheets.Add(_uss);
        rootVisualElement.Add(tweakRoot);
        
        _profileField = tweakRoot.Q<ObjectField>("profileField");
        _applyBtn = tweakRoot.Q<Button>("applyBtn");
        _applyBtn.clicked += OnApplyClicked;
        
        _toolTabs.Add("Create Softbody 2D", createRoot);
        _toolTabs.Add("Tweak Softbody Pyhsics Profile", tweakRoot);

        BuildTabs();
    }
    
    private void BuildTabs()
    {
        _toolbar = new Toolbar();
        rootVisualElement.Insert(0, _toolbar);

        foreach (var tab in _toolTabs)
        {
            var button = new ToolbarButton(() => ShowTab(tab.Key)) { text = tab.Key };
            _toolbar.Add(button);

            var currentTab = tab.Value;
            currentTab.style.flexGrow = 1;
            rootVisualElement.Add(currentTab);
        }

        ShowTab(_toolTabs.First().Key);
    }

    private void ShowTab(string tabKey)
    {
        foreach (var tab in _toolTabs)
            tab.Value.style.display = tab.Key == tabKey ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void OnApplyClicked()
    {
        var profile = _profileField?.value as SoftbodyPhysicsProfile;
        
        if (!profile)
        {
            EditorUtility.DisplayDialog("Softbody 2D", "Assign a Physics Profile first.", "OK");
            return;
        }

        var targets = Selection.gameObjects
            .Select(go => go.GetComponent<SoftbodyRuntime>())
            .Where(runtime => runtime != null)
            .ToArray();

        if (targets.Length == 0)
        {
            EditorUtility.DisplayDialog("Softbody 2D", "Select at least one Softbody (root with SoftbodyRuntime).", "OK");
            return;
        }

        foreach (var runtime in targets)
            SoftbodyTweakUtility.ApplyProfileTo(runtime, profile);
    }
    
    private void OnCreateClicked()
    {
        var obj = _sprite?.value as GameObject;
        if (!obj)
        {
            EditorUtility.DisplayDialog("Softbody 2D", "Please assign a Sprite (GameObject with SpriteRenderer).", "OK");
            return;
        }

        var sprite = obj.GetComponent<SpriteRenderer>();
        if (!sprite)
        {
            EditorUtility.DisplayDialog("Softbody 2D", "Selected object has no SpriteRenderer.", "OK");
            return;
        }

        var profile = _physicsProfileField != null ? _physicsProfileField.value as SoftbodyPhysicsProfile : null;
        if (!profile)
        {
            EditorUtility.DisplayDialog("Softbody 2D", "Assign a Physics Profile.", "OK");
            return;
        }

        var opts = new SoftbodyToolBuilder.SoftbodyConfig
        {
            Sprite = sprite,
            PhysicsProfile = profile,
            HasCentralBone = _hasCentralBone != null && _hasCentralBone.value
        };

        var root = SoftbodyToolBuilder.CreateSoftBody(opts);
        if (root != null)
        {
            Selection.activeGameObject = root;
            EditorGUIUtility.PingObject(root);
        }
    }
}
