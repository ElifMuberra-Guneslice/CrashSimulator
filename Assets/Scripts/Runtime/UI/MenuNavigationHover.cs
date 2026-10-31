using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
namespace CrashSimulator.UI
{
    public sealed class MenuNavigationHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text subtitle;
        [SerializeField] private Image indicator;
        [SerializeField] private bool selected;

        public void Configure(TMP_Text titleText, TMP_Text subtitleText, Image marker, bool isSelected)
        {
            title = titleText;
            subtitle = subtitleText;
            indicator = marker;
            selected = isSelected;
            SetState(selected);
        }

        public void OnPointerEnter(PointerEventData eventData) => SetState(true);
        public void OnPointerExit(PointerEventData eventData) => SetState(selected);

        private void SetState(bool active)
        {
            if (title != null) title.color = active
                ? new Color32(242, 245, 250, 255)
                : new Color32(171, 181, 199, 255);
            if (indicator != null) indicator.gameObject.SetActive(active);
        }
    }
}
