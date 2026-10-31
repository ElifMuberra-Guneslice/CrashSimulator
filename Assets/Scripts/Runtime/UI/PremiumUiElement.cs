using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public enum PremiumUiKind
    {
        PrimaryButton, SecondaryButton, IconButton, GlassPanel, StatCard,
        VehicleCard, ScenarioCard, ParameterSlider, ToggleControl,
        Speedometer, DamageIndicator, ConfirmationDialog, ToastNotification,
        LoadingIndicator, TabNavigation, ModalPanel, SimulationTimeline
    }

    /// <summary>Shared lightweight visual styling for editor-authored uGUI prefabs and screens.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class PremiumUiElement : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private PremiumUiTheme theme;
        [SerializeField] private PremiumUiKind kind;
        [SerializeField] private Image background;
        [SerializeField] private Text label;
        [SerializeField] private bool useCustomAccent;
        [SerializeField] private Color customAccent;
        private Vector3 restScale;
        private Color idleColor;
        private Outline outline;
        public PremiumUiKind Kind => kind;
        public void SetAccent(Color accent) { useCustomAccent=true;customAccent=accent;Apply(); }
        public void Configure(PremiumUiTheme selectedTheme,PremiumUiKind selectedKind,Image image,Text text)
        {
            theme=selectedTheme;kind=selectedKind;background=image;label=text;
            restScale=transform.localScale;
            Apply();
        }
        private void Awake() { restScale=transform.localScale; Apply(); }
        public void Apply()
        {
            if(theme==null)return;
            if(background!=null)
            {
                Color color = useCustomAccent ? customAccent : kind==PremiumUiKind.PrimaryButton ? theme.blue
                    : kind==PremiumUiKind.SecondaryButton || kind==PremiumUiKind.IconButton ? theme.secondary
                    : kind==PremiumUiKind.DamageIndicator ? theme.crashOrange
                    : kind==PremiumUiKind.ToastNotification ? theme.secondary
                    : theme.secondary;
                // The Figma system uses opaque, flat industrial surfaces (no glass blur).
                color.a=1f;
                idleColor=color;background.color=color;
                background.raycastTarget=kind!=PremiumUiKind.GlassPanel && kind!=PremiumUiKind.StatCard
                    && kind!=PremiumUiKind.Speedometer;
                if(NeedsOutline())
                {
                    outline=background.GetComponent<Outline>();
                    if(outline==null)outline=background.gameObject.AddComponent<Outline>();
                    outline.effectColor=kind==PremiumUiKind.PrimaryButton?theme.blue:theme.surface;
                    outline.effectDistance=new Vector2(1,-1);
                    outline.useGraphicAlpha=false;
                }
            }
            if(label!=null)
            {
                label.color=theme.primaryText;
                label.raycastTarget=false;
            }
        }
        private bool NeedsOutline() => kind!=PremiumUiKind.Speedometer && kind!=PremiumUiKind.DamageIndicator
            && kind!=PremiumUiKind.LoadingIndicator && kind!=PremiumUiKind.ToastNotification;
        public void OnPointerDown(PointerEventData data)
        {
            if(GetComponent<Button>()==null)return;
            if(background!=null)background.color=Color.Lerp(idleColor,Color.white,.18f);
            if(!GameUiSettings.ReducedMotion)transform.localScale=restScale*.97f;
        }
        public void OnPointerUp(PointerEventData data) => Release();
        public void OnPointerExit(PointerEventData data) => Release();
        private void Release()
        {
            if(background!=null)background.color=idleColor;
            transform.localScale=restScale;
        }
        private void OnDisable()=>Release();
    }
}
