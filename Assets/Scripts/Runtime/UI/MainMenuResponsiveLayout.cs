using UnityEngine;

namespace CrashSimulator.UI
{
    /// <summary>Reference composition with larger hit regions on compact landscape displays.</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class MainMenuResponsiveLayout : MonoBehaviour
    {
        [SerializeField] private RectTransform content, rail;
        [SerializeField] private RectTransform[] navigation, utilities;
        private Canvas canvas;
        private Vector2 lastSize;
        private float lastScale;
        public void Configure(RectTransform mainContent, RectTransform navigationRail,
            RectTransform[] actions, RectTransform[] utilityButtons)
        { content=mainContent;rail=navigationRail;navigation=actions;utilities=utilityButtons; }
        private void OnEnable() { canvas=GetComponentInParent<Canvas>();lastSize=Vector2.zero; }
        private void LateUpdate()
        {
            if(canvas==null || content==null)return;
            var size=((RectTransform)transform).rect.size;
            if(size==lastSize && Mathf.Approximately(lastScale,canvas.scaleFactor))return;
            lastSize=size;lastScale=canvas.scaleFactor;
            float minimumPixels=48f;
            if(Application.isMobilePlatform && Screen.dpi>0) minimumPixels=Mathf.Max(48,48*Screen.dpi/160f);
            float target=minimumPixels/Mathf.Max(.01f,canvas.scaleFactor);
            float height=Mathf.Max(61,target),step=Mathf.Max(71,height+10);
            content.anchorMin=content.anchorMax=new Vector2(.104f,.805f);
            content.pivot=new Vector2(0,1);
            // Keep the final action above the footer even when a tall safe-area inset is simulated.
            float contentHeight=213+3*step+height;
            float top=Mathf.Min(size.y*.195f,Mathf.Max(100,size.y-contentHeight-110));
            content.anchoredPosition=new Vector2(0,size.y*.195f-top);
            for(int i=0;i<navigation.Length;i++)
            {
                navigation[i].sizeDelta=new Vector2(430,height);
                navigation[i].anchoredPosition=new Vector2(0,-213-i*step);
            }
            rail.anchoredPosition=new Vector2(0,-213);
            rail.sizeDelta=new Vector2(1,3*step+height);
            float utilitySize=Mathf.Max(48,target),utilityStep=Mathf.Max(58,utilitySize+8);
            for(int i=0;i<utilities.Length;i++)
            {
                utilities[i].sizeDelta=Vector2.one*utilitySize;
                utilities[i].pivot=new Vector2(.5f,.5f);
                utilities[i].anchoredPosition=new Vector2(-88-(utilities.Length-1-i)*utilityStep,-72);
            }
        }
    }
}
