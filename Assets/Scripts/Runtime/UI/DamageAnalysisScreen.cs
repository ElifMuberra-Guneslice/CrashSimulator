using System.Text;
using CrashSimulator.Damage;
using UnityEngine;
using UnityEngine.UI;

namespace CrashSimulator.UI
{
    public sealed class DamageAnalysisScreen : MonoBehaviour
    {
        [SerializeField] private DamageReport vehicleA,vehicleB;
        [SerializeField] private Text title,summary,details,history;
        [SerializeField] private GameObject basicPanel,advancedPanel;
        private readonly StringBuilder buffer=new StringBuilder(1024);
        private DamageReport selected;
        private bool advanced;
        public void Configure(DamageReport a,DamageReport b,Text heading,Text basic,Text technical,Text events,
            GameObject basicView,GameObject advancedView)
        { vehicleA=a;vehicleB=b;title=heading;summary=basic;details=technical;history=events;basicPanel=basicView;advancedPanel=advancedView;SelectVehicleA(); }
        public void Show()
        {
            if(vehicleA==null || vehicleB==null)return;
            if(selected==null)selected=vehicleA;
            Refresh();
        }
        public void SelectVehicleA(){selected=vehicleA;Refresh();}
        public void SelectVehicleB(){selected=vehicleB;Refresh();}
        public void ShowBasic(){advanced=false;Refresh();}
        public void ShowAdvanced(){advanced=true;Refresh();}
        private void Refresh()
        {
            if(selected==null)return;
            if(basicPanel!=null)basicPanel.SetActive(!advanced);
            if(advancedPanel!=null)advancedPanel.SetActive(advanced);
            var report=selected.Snapshot();
            if(title!=null)title.text=report.VehicleId.ToUpperInvariant()+"  /  DAMAGE ANALYSIS";
            if(report.ImpactCount==0)
            {
                if(summary!=null)summary.text="NO COLLISION DATA\n\nSpeed  —\nMain region  —\nDamage severity  —";
                if(details!=null)details.text="Impulse  —\nDirection  —\nDetached components  0\nMechanical condition  —";
                if(history!=null)history.text="No impact events recorded.";
                return;
            }
            if(summary!=null)summary.text=$"IMPACT SPEED\n{report.LastImpactSpeedKph:0} km/h\n\nMAIN REGION\n{report.MainRegion}\n\nDAMAGE SEVERITY SCORE\n{report.LastSeverity:0} / 100";
            if(details!=null)details.text=$"IMPULSE     {report.LastImpulse:0} N·s\nDIRECTION  {report.LastDirection:F2}\nDETACHED   {report.DetachedParts}\n\nENGINE       {(report.Mechanical.Engine*100):0}%\nBRAKES       {(report.Mechanical.Brakes*100):0}%\nSTEERING     {report.Mechanical.SteeringBias:+0.0;-0.0;0.0}°\nENGINE FAIL  {(report.Mechanical.EngineFailed?"YES":"NO")}";
            if(history!=null)
            {
                buffer.Clear();
                int first=Mathf.Max(0,selected.StoredImpacts-6);
                for(int i=first;i<selected.StoredImpacts;i++)
                {
                    var impact=selected.GetImpact(i);
                    buffer.Append((i+1).ToString("00")).Append("   ").Append(impact.Region)
                        .Append("   ").Append((impact.NormalSpeed*3.6f).ToString("0"))
                        .Append(" km/h   ").Append(impact.Severity.ToString("0")).Append(" score\n");
                }
                history.text=buffer.Length==0?"No impact events recorded.":buffer.ToString();
            }
        }
    }
}
