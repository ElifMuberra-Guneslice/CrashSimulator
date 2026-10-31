using UnityEngine;

namespace CrashSimulator.UI
{
    public sealed class ResponsiveCrashLabPanels : MonoBehaviour
    {
        [SerializeField] private GameObject vehiclePanel,setupPanel,vehicleTab,setupTab;
        private Vector2Int lastSize;
        private bool compact,open;
        public void Configure(GameObject vehicle,GameObject setup,GameObject vehicleButton,GameObject setupButton)
        { vehiclePanel=vehicle;setupPanel=setup;vehicleTab=vehicleButton;setupTab=setupButton;Apply(true); }
        public void ToggleVehiclePanel()=>Toggle(vehiclePanel);
        public void ToggleSetupPanel()=>Toggle(setupPanel);
        private void Toggle(GameObject selected)
        {
            if(selected==null)return;
            bool next=!selected.activeSelf;
            if(vehiclePanel!=null)vehiclePanel.SetActive(false);
            if(setupPanel!=null)setupPanel.SetActive(false);
            selected.SetActive(next);open=next;
        }
        private void Update()
        {
            var size=new Vector2Int(Screen.width,Screen.height);
            if(size==lastSize)return;
            lastSize=size;Apply(false);
        }
        private void Apply(bool first)
        {
            lastSize=new Vector2Int(Screen.width,Screen.height);
            compact=lastSize.y>0 && ((float)lastSize.x/lastSize.y<1.48f || lastSize.y<600);
            if(vehicleTab!=null)vehicleTab.SetActive(compact);
            if(setupTab!=null)setupTab.SetActive(compact);
            if(!compact)
            {
                if(vehiclePanel!=null)vehiclePanel.SetActive(true);
                if(setupPanel!=null)setupPanel.SetActive(true);
                open=false;
            }
            else if(first)
            {
                if(vehiclePanel!=null)vehiclePanel.SetActive(false);
                if(setupPanel!=null)setupPanel.SetActive(false);
                open=false;
            }
            else if(!open)
            { if(vehiclePanel!=null)vehiclePanel.SetActive(false);if(setupPanel!=null)setupPanel.SetActive(false); }
        }
    }
}
