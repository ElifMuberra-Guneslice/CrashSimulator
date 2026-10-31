using System.IO;
using CrashSimulator.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    /// <summary>Shared authoring code; all visible screens use the same saved prefab templates.</summary>
    public static class PremiumUiFactory
    {
        private const float FigmaReferenceScale = 1.2f; // Existing layouts were authored at 1600x900.
        public const string ThemePath="Assets/Settings/UI/PremiumTheme.asset";
        public const string PrefabFolder="Assets/Prefabs/UI/Premium";
        public static PremiumUiTheme Theme { get; private set; }
        private static readonly PremiumUiKind[] Kinds =
        {
            PremiumUiKind.PrimaryButton,PremiumUiKind.SecondaryButton,PremiumUiKind.IconButton,
            PremiumUiKind.GlassPanel,PremiumUiKind.StatCard,PremiumUiKind.VehicleCard,
            PremiumUiKind.ScenarioCard,PremiumUiKind.ParameterSlider,PremiumUiKind.ToggleControl,
            PremiumUiKind.Speedometer,PremiumUiKind.DamageIndicator,PremiumUiKind.ConfirmationDialog,
            PremiumUiKind.ToastNotification,PremiumUiKind.LoadingIndicator,PremiumUiKind.TabNavigation,
            PremiumUiKind.ModalPanel,PremiumUiKind.SimulationTimeline
        };
        public static void EnsureLibrary()
        {
            Directory.CreateDirectory("Assets/Settings/UI");
            Directory.CreateDirectory(PrefabFolder);
            AssetDatabase.Refresh();
            Theme=AssetDatabase.LoadAssetAtPath<PremiumUiTheme>(ThemePath);
            if(Theme==null)
            {
                Theme=ScriptableObject.CreateInstance<PremiumUiTheme>();
                AssetDatabase.CreateAsset(Theme,ThemePath);
            }
            foreach(var kind in Kinds)
            {
                string path=$"{PrefabFolder}/{kind}.prefab";
                var existing=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(existing!=null)
                {
                    if(NeedsButton(kind) && existing.GetComponent<Button>()==null)
                    {
                        var contents=PrefabUtility.LoadPrefabContents(path);
                        AddButton(contents);
                        PrefabUtility.SaveAsPrefabAsset(contents,path);
                        PrefabUtility.UnloadPrefabContents(contents);
                    }
                    continue;
                }
                var root=Template(kind);
                PrefabUtility.SaveAsPrefabAsset(root,path);
                Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
        }
        private static GameObject Template(PremiumUiKind kind)
        {
            var root=new GameObject(kind.ToString(),typeof(RectTransform),typeof(Image));
            var rect=(RectTransform)root.transform;
            rect.sizeDelta=new Vector2(220,64);
            var image=root.GetComponent<Image>();
            image.type=Image.Type.Simple;
            if(NeedsButton(kind))AddButton(root);
            Text label=null;
            if(kind!=PremiumUiKind.ParameterSlider && kind!=PremiumUiKind.ToggleControl)
            {
                label=Label(root.transform,"Label",kind.ToString().ToUpperInvariant(),22,TextAnchor.MiddleCenter,Theme.primaryText);
                label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;
                label.rectTransform.offsetMin=new Vector2(10,4);label.rectTransform.offsetMax=new Vector2(-10,-4);
            }
            if(kind==PremiumUiKind.ParameterSlider) BuildSlider(root);
            if(kind==PremiumUiKind.ToggleControl) BuildToggle(root);
            root.AddComponent<PremiumUiElement>().Configure(Theme,kind,image,label);
            return root;
        }
        private static bool NeedsButton(PremiumUiKind kind) => kind==PremiumUiKind.PrimaryButton
            || kind==PremiumUiKind.SecondaryButton || kind==PremiumUiKind.IconButton
            || kind==PremiumUiKind.VehicleCard || kind==PremiumUiKind.ScenarioCard
            || kind==PremiumUiKind.TabNavigation || kind==PremiumUiKind.SimulationTimeline;
        private static void AddButton(GameObject root)
        {
            var control=root.GetComponent<Button>();
            if(control==null)control=root.AddComponent<Button>();
            control.targetGraphic=root.GetComponent<Image>();
            control.transition=Selectable.Transition.None;
            control.navigation=new Navigation{mode=Navigation.Mode.None};
        }
        private static void BuildSlider(GameObject root)
        {
            var slider=root.AddComponent<Slider>();
            slider.transition=Selectable.Transition.None;
            slider.navigation=new Navigation{mode=Navigation.Mode.None};
            slider.minValue=0;slider.maxValue=1;
            var track=Rect("Track",root.transform);
            track.anchorMin=new Vector2(0,.5f);track.anchorMax=new Vector2(1,.5f);
            track.offsetMin=new Vector2(16,-5);track.offsetMax=new Vector2(-16,5);
            track.gameObject.AddComponent<Image>().color=Theme.surface;
            var fill=Rect("Fill",track);
            fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;
            var fillImage=fill.gameObject.AddComponent<Image>();fillImage.color=Theme.blue;
            var handle=Rect("Handle",root.transform);
            handle.anchorMin=handle.anchorMax=new Vector2(.5f,.5f);handle.sizeDelta=new Vector2(30,30);
            var handleImage=handle.gameObject.AddComponent<Image>();handleImage.color=Theme.primaryText;
            slider.fillRect=fill;slider.handleRect=handle;slider.targetGraphic=handleImage;
        }
        private static void BuildToggle(GameObject root)
        {
            var toggle=root.AddComponent<Toggle>();
            toggle.transition=Selectable.Transition.None;
            toggle.navigation=new Navigation{mode=Navigation.Mode.None};
            var check=Rect("Checkmark",root.transform);
            check.anchorMin=check.anchorMax=new Vector2(.5f,.5f);
            check.sizeDelta=new Vector2(38,38);
            var image=check.gameObject.AddComponent<Image>();image.color=Theme.blue;
            toggle.graphic=image;toggle.targetGraphic=root.GetComponent<Image>();
        }
        public static RectTransform Rect(string name,Transform parent)
        {
            var rect=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent,false);return rect;
        }
        public static Text Label(Transform parent,string name,string value,int size,TextAnchor alignment,Color color)
        {
            var rect=Rect(name,parent);
            var text=rect.gameObject.AddComponent<Text>();
            text.font=Theme!=null
                ? (size>=32 ? Theme.displayFont : size>=22 ? Theme.displayFontMedium : Theme.uiFont)
                : null;
            if(text.font==null)text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text=value;text.fontSize=Mathf.RoundToInt(size*FigmaReferenceScale);text.color=color;text.alignment=alignment;
            text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;
            return text;
        }
        public static RectTransform Place(RectTransform rect,Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;
            rect.anchoredPosition=position*FigmaReferenceScale;rect.sizeDelta=size*FigmaReferenceScale;return rect;
        }
        public static RectTransform Stretch(RectTransform rect,Vector2 min,Vector2 max,Vector2 insetMin,Vector2 insetMax)
        {
            rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=insetMin;rect.offsetMax=insetMax;return rect;
        }
        public static GameObject Instance(PremiumUiKind kind,Transform parent,string name)
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabFolder}/{kind}.prefab");
            var result=(GameObject)PrefabUtility.InstantiatePrefab(prefab,parent);
            result.name=name;return result;
        }
        public static Button Button(Transform parent,string name,string caption,PremiumUiKind kind,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size,UnityAction action)
        {
            var go=Instance(kind,parent,name);
            Place((RectTransform)go.transform,anchor,pivot,position,size);
            var text=go.GetComponentInChildren<Text>();
            if(text!=null)text.text=caption;
            var button=go.GetComponent<Button>();
            if(action!=null)UnityEventTools.AddPersistentListener(button.onClick,action);
            return button;
        }
        public static RectTransform Panel(Transform parent,string name,PremiumUiKind kind,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 size)
        {
            var go=Instance(kind,parent,name);
            var rect=Place((RectTransform)go.transform,anchor,pivot,position,size);
            var label=go.GetComponentInChildren<Text>();if(label!=null)Object.DestroyImmediate(label.gameObject);
            go.GetComponent<Image>().raycastTarget=false;
            return rect;
        }
        public static Slider Slider(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,
            Vector2 size,float min,float max,float value)
        {
            var go=Instance(PremiumUiKind.ParameterSlider,parent,name);
            Place((RectTransform)go.transform,anchor,pivot,position,size);
            var slider=go.GetComponent<Slider>();slider.minValue=min;slider.maxValue=max;slider.value=value;
            return slider;
        }
        public static Toggle Toggle(Transform parent,string name,Vector2 anchor,Vector2 pivot,Vector2 position,bool on)
        {
            var go=Instance(PremiumUiKind.ToggleControl,parent,name);
            Place((RectTransform)go.transform,anchor,pivot,position,new Vector2(62,56));
            var toggle=go.GetComponent<Toggle>();toggle.isOn=on;return toggle;
        }
    }
}
