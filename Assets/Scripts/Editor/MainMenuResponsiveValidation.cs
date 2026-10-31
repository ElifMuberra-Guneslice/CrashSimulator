using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CrashSimulator.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CrashSimulator.Editor
{
    /// <summary>Captures real Play-mode Game Views. Safe-area injection is explicitly a simulation.</summary>
    public static class MainMenuResponsiveValidation
    {
        [Serializable] private sealed class Result
        {
            public int width,height;
            public bool simulatedCutout;
            public Rect safeArea;
            public string screenshot;
            public float minimumButtonPixels;
            public List<string> failures=new();
        }
        [Serializable] private sealed class Report
        {
            public string unityVersion,utc;
            public bool passed;
            public List<Result> results=new();
        }
        private static readonly Vector2Int[] Sizes={new(1920,1080),new(2400,1080),new(2532,1170),new(1280,720),new(1024,768),new(2532,1170)};
        private static Report report;
        private static int index,frames;
        private static double deadline;
        private static DateTime captureStarted;
        private static bool capturing;
        private static Result pending;

        [MenuItem("Crash Simulator/Validation/Capture Main Menu Responsive Matrix")]
        public static void Run()
        {
            if(!EditorApplication.isPlaying || SceneManager.GetActiveScene().name!="MainMenu")
                throw new InvalidOperationException("Run with MainMenu active in Play mode.");
            if(report!=null)throw new InvalidOperationException("A capture matrix is already running.");
            report=new Report{unityVersion=Application.unityVersion,utc=DateTime.UtcNow.ToString("O"),passed=true};
            index=0;SetNext();EditorApplication.update+=Tick;
        }
        private static void SetNext()
        {
            var size=Sizes[index];SetGameViewSize(size.x,size.y);
            SafeAreaPanel.SimulatedSafeArea=index==5?new Rect(120,24,size.x-200,size.y-54):(Rect?)null;
            frames=0;capturing=false;deadline=EditorApplication.timeSinceStartup+30;
        }
        private static void Tick()
        {
            try
            {
                if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play mode ended before the captures completed.");
                if(EditorApplication.timeSinceStartup>deadline)throw new TimeoutException("Game View or screenshot did not reach the requested dimensions.");
                var size=Sizes[index];
                if(Screen.width!=size.x || Screen.height!=size.y)return;
                if(!capturing)
                {
                    if(++frames<15)return;
                    pending=Inspect(size,index==5);
                    captureStarted=DateTime.UtcNow;
                    Directory.CreateDirectory(Path.GetDirectoryName(pending.screenshot));
                    ScreenCapture.CaptureScreenshot(pending.screenshot);
                    capturing=true;return;
                }
                if(!File.Exists(pending.screenshot) || File.GetLastWriteTimeUtc(pending.screenshot)<captureStarted)return;
                var bytes=File.ReadAllBytes(pending.screenshot);
                if(bytes.Length<24)return;
                int width=(bytes[16]<<24)|(bytes[17]<<16)|(bytes[18]<<8)|bytes[19];
                int height=(bytes[20]<<24)|(bytes[21]<<16)|(bytes[22]<<8)|bytes[23];
                if(width!=size.x || height!=size.y)throw new InvalidOperationException("Captured PNG dimensions do not match Game View.");
                report.results.Add(pending);report.passed&=pending.failures.Count==0;
                if(++index==Sizes.Length){Finish();return;}SetNext();
            }
            catch(Exception e){Debug.LogException(e);if(report!=null)report.passed=false;Finish();}
        }
        private static Result Inspect(Vector2Int size,bool simulated)
        {
            Canvas.ForceUpdateCanvases();
            var result=new Result{width=size.x,height=size.y,simulatedCutout=simulated,
                safeArea=SafeAreaPanel.SimulatedSafeArea??Screen.safeArea,minimumButtonPixels=float.MaxValue,
                screenshot=$"Documentation/UI/UnityScreenshots/MainMenu_{size.x}x{size.y}"+(simulated?"_SimulatedSafeArea":"")+".png"};
            var buttons=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude,FindObjectsSortMode.None);
            var rectangles=new List<Rect>();var names=new List<string>();
            foreach(var button in buttons)
            {
                if(!button.interactable)continue;
                var rect=PixelRect((RectTransform)button.transform);
                result.minimumButtonPixels=Mathf.Min(result.minimumButtonPixels,Mathf.Min(rect.width,rect.height));
                if(!Contains(result.safeArea,rect))result.failures.Add(button.name+" extends outside the safe area.");
                if(rect.width<47.5f || rect.height<47.5f)result.failures.Add(button.name+" target is below 48 screen pixels (desktop simulation).");
                for(int i=0;i<rectangles.Count;i++)if(rect.Overlaps(rectangles[i]))result.failures.Add(button.name+" overlaps "+names[i]);
                rectangles.Add(rect);names.Add(button.name);
                var hits=new List<RaycastResult>();
                EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=rect.center},hits);
                if(hits.Count==0 || hits[0].gameObject.GetComponentInParent<Button>()!=button)
                    result.failures.Add(button.name+" center is not reachable by UI raycast.");
            }
            foreach(var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))
            {
                text.ForceMeshUpdate();
                if(text.font==null)result.failures.Add(text.name+" has no font.");
                if(text.isTextOverflowing)result.failures.Add(text.name+" text overflows its rect.");
                if(!Contains(result.safeArea,PixelRect(text.rectTransform)))result.failures.Add(text.name+" text rect is outside the safe area.");
            }
            return result;
        }
        private static Rect PixelRect(RectTransform transform)
        {
            var points=new Vector3[4];transform.GetWorldCorners(points);
            return Rect.MinMaxRect(points[0].x,points[0].y,points[2].x,points[2].y);
        }
        private static bool Contains(Rect outer,Rect inner) => inner.xMin>=outer.xMin-.5f && inner.yMin>=outer.yMin-.5f
            && inner.xMax<=outer.xMax+.5f && inner.yMax<=outer.yMax+.5f;
        private static void Finish()
        {
            EditorApplication.update-=Tick;SafeAreaPanel.SimulatedSafeArea=null;
            if(report!=null)
            {
                Directory.CreateDirectory("Documentation/UI/ValidationEvidence");
                File.WriteAllText("Documentation/UI/ValidationEvidence/MainMenuResponsive.json",JsonUtility.ToJson(report,true));
                Debug.Log("PHASE4_MENU_RESPONSIVE "+(report.passed?"PASSED":"FAILED"));
            }
            report=null;SetGameViewSize(1920,1080);
        }
        private static void SetGameViewSize(int width,int height)
        {
            // Unity has no public Editor Game View sizing API. Validate the reflected members on use.
            var assembly=typeof(UnityEditor.Editor).Assembly;
            var sizesType=assembly.GetType("UnityEditor.GameViewSizes",true);
            var instance=sizesType.GetProperty("instance",BindingFlags.Public|BindingFlags.Static|BindingFlags.FlattenHierarchy).GetValue(null);
            var groupType=sizesType.GetProperty("currentGroupType").GetValue(instance);
            var group=sizesType.GetMethod("GetGroup").Invoke(instance,new[]{groupType});
            var groupClass=group.GetType();
            int count=(int)groupClass.GetMethod("GetBuiltinCount").Invoke(group,null)+(int)groupClass.GetMethod("GetCustomCount").Invoke(group,null);
            int selected=-1;
            for(int i=0;i<count;i++)
            {
                var size=groupClass.GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)size.GetType().GetProperty("width").GetValue(size)==width && (int)size.GetType().GetProperty("height").GetValue(size)==height){selected=i;break;}
            }
            if(selected<0)
            {
                var sizeType=assembly.GetType("UnityEditor.GameViewSize",true);
                var enumType=assembly.GetType("UnityEditor.GameViewSizeType",true);
                var size=Activator.CreateInstance(sizeType,BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,
                    new object[]{Enum.ToObject(enumType,1),width,height,$"UI QA {width}x{height}"},null);
                groupClass.GetMethod("AddCustomSize").Invoke(group,new[]{size});selected=count;
            }
            var view=EditorWindow.GetWindow(assembly.GetType("UnityEditor.GameView",true));
            view.GetType().GetProperty("selectedSizeIndex",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic).SetValue(view,selected);
            view.Focus();view.Repaint();
        }
    }
}
