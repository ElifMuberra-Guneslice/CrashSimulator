using System.Collections.Generic;
using CrashSimulator.UI;
using CrashSimulator.Vehicle;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace CrashSimulator.Editor
{
    public static class PremiumMenuBuilder
    {
        public const string ScenePath="Assets/Scenes/MainMenu.unity";
        [MenuItem("Crash Simulator/Phase 3.5/Build Main Menu")]
        public static void Build()
        {
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            PremiumUiFactory.EnsureLibrary();
            var menuFonts=EnsureMainMenuFonts();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var theme=PremiumUiFactory.Theme;
            CreateWorld(theme,out var preview);
            var canvas=CreateCanvas();
            var safe=PremiumUiFactory.Rect("Safe Area",canvas.transform);
            PremiumUiFactory.Stretch(safe,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            safe.gameObject.AddComponent<SafeAreaPanel>();
            var home=Screen(canvas.transform,"Home");
            home.SetAsFirstSibling();
            var garage=Screen(safe,"Garage");
            var settings=Screen(safe,"Settings");
            var loading=Screen(safe,"Loading");
            var menu=canvas.AddComponent<PremiumMenuController>();
            BuildHome(home,menu,theme,menuFonts);
            BuildGarage(garage,menu,preview,theme,out var spec,out var selection);
            BuildSettings(settings,menu,theme);
            BuildLoading(loading,theme);
            menu.Configure(home.gameObject,garage.gameObject,settings.gameObject,loading.gameObject,
                spec,selection,AssetDatabase.LoadAssetAtPath<VehicleSettings>("Assets/Settings/Vehicles/PrototypeCar.asset"));
            home.gameObject.SetActive(true);garage.gameObject.SetActive(false);
            settings.gameObject.SetActive(false);loading.gameObject.SetActive(false);
            if(Object.FindFirstObjectByType<EventSystem>()==null)
            {
                var events=new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            EditorSceneManager.SaveScene(scene,ScenePath);
            RegisterBuildScenes();
            AssetDatabase.SaveAssets();
            Debug.Log("PHASE35_MENU_BUILT");
        }
        private static void CreateWorld(PremiumUiTheme theme,out VehiclePreviewTurntable turntable)
        {
            var cameraObject=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            cameraObject.tag="MainCamera";
            var camera=cameraObject.GetComponent<Camera>();
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=theme.background;
            camera.fieldOfView=42;camera.nearClipPlane=.1f;camera.farClipPlane=90;
            camera.transform.position=new Vector3(5.2f,2.8f,-7.5f);
            camera.transform.LookAt(new Vector3(0,1.1f,0));
            var light=new GameObject("Studio Key Light",typeof(Light)).GetComponent<Light>();
            light.type=LightType.Directional;light.intensity=1.25f;
            light.transform.rotation=Quaternion.Euler(45,-35,0);
            var fill=new GameObject("Studio Fill Light",typeof(Light)).GetComponent<Light>();
            fill.type=LightType.Point;fill.intensity=1.5f;fill.range=12;
            fill.transform.position=new Vector3(-3,4,-2);
            var pedestal=GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pedestal.name="Preview Turntable Base";
            pedestal.transform.SetPositionAndRotation(new Vector3(0,-.18f,0),Quaternion.identity);
            pedestal.transform.localScale=new Vector3(3.2f,.18f,3.2f);
            Object.DestroyImmediate(pedestal.GetComponent<Collider>());
            const string materialPath="Assets/Materials/Environment/MenuTurntable.mat";
            var baseMaterial=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if(baseMaterial==null)
            {
                baseMaterial=new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
                baseMaterial.SetColor("_BaseColor",theme.surface);
                AssetDatabase.CreateAsset(baseMaterial,materialPath);
            }
            pedestal.GetComponent<MeshRenderer>().sharedMaterial=baseMaterial;
            var car=(GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Vehicles/PrototypeCar.prefab"));
            car.name="Vehicle Preview";
            car.transform.SetPositionAndRotation(new Vector3(0,.12f,0),Quaternion.Euler(0,-25,0));
            var vehicle=car.GetComponent<VehicleController>();vehicle.enabled=false;
            car.GetComponent<Rigidbody>().isKinematic=true;
            car.GetComponent<Rigidbody>().useGravity=false;
            foreach(var wheel in car.GetComponentsInChildren<WheelCollider>())wheel.enabled=false;
            foreach(var collider in car.GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var behaviour in car.GetComponentsInChildren<MonoBehaviour>(true))
                behaviour.enabled=false;
            turntable=car.AddComponent<VehiclePreviewTurntable>();
        }
        private static GameObject CreateCanvas()
        {
            var go=new GameObject("Premium Main Menu UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            go.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            var scaler=go.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            return go;
        }
        private static RectTransform Screen(Transform parent,string name)
        {
            var rect=PremiumUiFactory.Rect(name,parent);
            return PremiumUiFactory.Stretch(rect,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        }
        private static Text TextAt(Transform parent,string name,string value,int size,TextAnchor align,Color color,
            Vector2 anchor,Vector2 pivot,Vector2 position,Vector2 dimensions)
        {
            var text=PremiumUiFactory.Label(parent,name,value,size,align,color);
            PremiumUiFactory.Place(text.rectTransform,anchor,pivot,position,dimensions);
            return text;
        }
        private static void BuildHome(RectTransform home,PremiumMenuController menu,PremiumUiTheme theme,MainMenuFonts fonts)
        {
            var background=PremiumUiFactory.Rect("Published Garage Photograph",home);
            PremiumUiFactory.Stretch(background,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            var photo=background.gameObject.AddComponent<Image>();
            photo.sprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_CrashSimulator/UI/Images/MainMenuGarageBackground.jpg");
            photo.color=Color.white;photo.raycastTarget=false;
            photo.material=EnsureReferencePhotoMaterial();
            var cover=background.gameObject.AddComponent<AspectRatioFitter>();
            cover.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;cover.aspectRatio=1.5f;
            var scrim=PremiumUiFactory.Rect("Reference Gradient Scrim",home);
            PremiumUiFactory.Stretch(scrim,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            scrim.gameObject.AddComponent<CanvasRenderer>();
            scrim.gameObject.AddComponent<MainMenuGradientOverlay>().raycastTarget=false;
            home=Screen(home,"Safe Menu Content");
            home.gameObject.AddComponent<SafeAreaPanel>();

            var status=MenuTextAt(home,"Status","CRASH TEST SYSTEMS  <color=#37C99B>•</color>  ONLINE",11,fonts.ui,FontStyles.Bold,
                new Color32(142,154,181,255),new Vector2(64,-64),new Vector2(420,20),1.8f);
            status.richText=true;
            MenuIconButton(home,"Sound",new Vector2(-122,-48),menu.ToggleSound,MenuIconType.Sound);
            MenuIconButton(home,"Settings",new Vector2(-64,-48),menu.ShowSettings,MenuIconType.Settings);

            MenuTextAt(home,"Logo Crash","CRASH",94,fonts.bold,FontStyles.Normal,
                new Color32(242,245,250,255),new Vector2(200,-186),new Vector2(430,76),2f);
            var simulator=MenuTextAt(home,"Logo Simulator","SIMULATOR",47,fonts.medium,FontStyles.Normal,
                new Color32(185,196,217,255),new Vector2(200,-272),new Vector2(430,45),12f);
            simulator.fontWeight=FontWeight.Medium;
            var tagline=MenuTextAt(home,"Tagline","DRIVE. <color=#FF7A1A>CRASH.</color> ANALYZE.",19,
                fonts.bold,FontStyles.Normal,new Color32(203,211,224,255),new Vector2(200,-351),
                new Vector2(430,30),2f);
            tagline.richText=true;

            var navigationRail=PremiumUiFactory.Rect("Navigation Rail",home);
            MenuPlace(navigationRail,new Vector2(0,1),new Vector2(0,1),new Vector2(200,-432),new Vector2(1,267));
            navigationRail.gameObject.AddComponent<Image>().color=new Color32(42,53,80,220);

            MenuNavigationButton(home,"Free Drive","FREE DRIVE","Drive freely. Test the limits.",
                424,fonts,menu.StartFreeDrive,true);
            MenuNavigationButton(home,"Crash Lab","CRASH LAB",null,495,fonts,menu.StartCrashLab,false);
            MenuNavigationButton(home,"Garage","GARAGE",null,566,fonts,menu.ShowGarage,false);
            MenuNavigationButton(home,"Settings Navigation","SETTINGS",null,637,fonts,menu.ShowSettings,false);

            var facilityNumber=MenuTextAt(home,"Facility Number","01",32,fonts.medium,FontStyles.Normal,
                new Color32(46,139,255,255),new Vector2(-194,114),new Vector2(31,43),1.4f,
                new Vector2(1,0),new Vector2(1,1));
            facilityNumber.fontWeight=FontWeight.Medium;
            MenuTextAt(home,"Facility Name","VEHICLE DYNAMICS\nSANDBOX",10,fonts.ui,FontStyles.Normal,
                new Color32(200,208,223,255),new Vector2(-64,109),new Vector2(121,34),1.4f,
                new Vector2(1,0),new Vector2(1,1));
            var footerLine=PremiumUiFactory.Rect("Footer Rule",home);
            footerLine.anchorMin=new Vector2(0,0);footerLine.anchorMax=new Vector2(1,0);
            footerLine.offsetMin=new Vector2(150,38);footerLine.offsetMax=new Vector2(-190,39);
            footerLine.gameObject.AddComponent<Image>().color=new Color32(42,53,80,190);
            MenuTextAt(home,"Build","BUILD 0.1.0",10,fonts.ui,FontStyles.Bold,
                new Color32(115,128,153,255),new Vector2(64,46),new Vector2(90,18),1.7f,
                new Vector2(0,0),new Vector2(0,1));
            MenuTextAt(home,"Test Facility","TEST FACILITY 01",10,fonts.ui,FontStyles.Bold,
                new Color32(115,128,153,255),new Vector2(-64,46),new Vector2(125,18),1.7f,
                new Vector2(1,0),new Vector2(1,1));
            var content=PremiumUiFactory.Rect("Menu Content",home);
            MenuPlace(content,new Vector2(.104f,.805f),new Vector2(0,1),Vector2.zero,new Vector2(430,490));
            foreach(var childName in new[]{"Logo Crash","Logo Simulator","Tagline","Navigation Rail","Free Drive","Crash Lab","Garage","Settings Navigation"})
            {
                var child=(RectTransform)home.Find(childName);var position=child.anchoredPosition;
                child.SetParent(content,false);child.anchoredPosition=position-new Vector2(200,-211);
            }
            home.gameObject.AddComponent<MainMenuResponsiveLayout>().Configure(content,navigationRail,
                new[]{(RectTransform)content.Find("Free Drive"),(RectTransform)content.Find("Crash Lab"),
                    (RectTransform)content.Find("Garage"),(RectTransform)content.Find("Settings Navigation")},
                new[]{(RectTransform)home.Find("Sound"),(RectTransform)home.Find("Settings")});
        }

        private static Material EnsureReferencePhotoMaterial()
        {
            const string path="Assets/_CrashSimulator/UI/Images/MainMenuPhoto.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null)
            {
                var shader=Shader.Find("CrashSimulator/UI/ReferencePhoto");
                if(shader==null)throw new System.InvalidOperationException("Reference photo shader is missing.");
                material=new Material(shader);AssetDatabase.CreateAsset(material,path);
            }
            material.SetFloat("_Grayscale",.7f);material.SetFloat("_Contrast",1.12f);material.SetFloat("_Brightness",.6f);
            return material;
        }

        private struct MainMenuFonts { public TMP_FontAsset bold,medium,ui; }
        private static MainMenuFonts EnsureMainMenuFonts()
        {
            const string folder="Assets/Settings/UI/TMP";
            if(!AssetDatabase.IsValidFolder(folder))AssetDatabase.CreateFolder("Assets/Settings/UI","TMP");
            return new MainMenuFonts
            {
                bold=GetOrCreateTmpFont("Assets/_CrashSimulator/UI/Fonts/Rajdhani-Bold.ttf",$"{folder}/Rajdhani-Bold SDF.asset"),
                medium=GetOrCreateTmpFont("Assets/_CrashSimulator/UI/Fonts/Rajdhani-Medium.ttf",$"{folder}/Rajdhani-Medium SDF.asset"),
                ui=GetOrCreateTmpFont("Assets/_CrashSimulator/UI/Fonts/Inter-Variable.ttf",$"{folder}/Inter SDF.asset")
            };
        }
        private static TMP_FontAsset GetOrCreateTmpFont(string source,string destination)
        {
            var asset=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destination);
            if(asset!=null)
            { asset.boldSpacing=0;EditorUtility.SetDirty(asset);return asset; }
            if(AssetDatabase.FindAssets("t:TMP_Settings").Length==0
                || Shader.Find("TextMeshPro/Mobile/Distance Field")==null)
                throw new System.InvalidOperationException(
                    "Import TMP Essentials first: Window > TextMeshPro > Import TMP Essential Resources.");
            var sourceFont=AssetDatabase.LoadAssetAtPath<Font>(source);
            if(sourceFont==null)throw new System.InvalidOperationException($"Missing menu font: {source}");
            asset=TMP_FontAsset.CreateFontAsset(sourceFont);
            if(asset==null)throw new System.InvalidOperationException($"Could not create TMP font from {source}");
            asset.name=System.IO.Path.GetFileNameWithoutExtension(destination);
            AssetDatabase.CreateAsset(asset,destination);
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0],asset);
            AssetDatabase.AddObjectToAsset(asset.material,asset);
            AssetDatabase.SaveAssets();
            return asset;
        }
        private static TMP_Text MenuTextAt(Transform parent,string name,string value,float size,TMP_FontAsset font,
            FontStyles style,Color color,Vector2 position,Vector2 dimensions,float spacing,
            Vector2? anchor=null,Vector2? pivot=null,TextAlignmentOptions alignment=TextAlignmentOptions.TopLeft)
        {
            var rect=PremiumUiFactory.Rect(name,parent);
            var a=anchor??new Vector2(0,1);var p=pivot??new Vector2(0,1);
            MenuPlace(rect,a,p,position,dimensions);
            var text=rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font=font;text.fontSize=size;text.fontStyle=style;text.color=color;text.text=value;
            text.characterSpacing=spacing*90f/Mathf.Max(1f,size);text.alignment=alignment;
            text.textWrappingMode=TextWrappingModes.NoWrap;
            text.raycastTarget=false;text.margin=Vector4.zero;
            return text;
        }
        private static void MenuNavigationButton(Transform parent,string name,string title,string subtitle,
            float topY,MainMenuFonts fonts,UnityEngine.Events.UnityAction action,bool selected)
        {
            var rect=PremiumUiFactory.Rect(name,parent);
            MenuPlace(rect,new Vector2(0,1),new Vector2(0,1),
                new Vector2(200,-topY),new Vector2(430,61));
            var hit=rect.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,0);hit.raycastTarget=true;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=hit;
            button.transition=Selectable.Transition.None;button.navigation=new Navigation{mode=Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(button.onClick,action);
            var titleText=MenuTextAt(rect,title,title,26,fonts.medium,FontStyles.Bold,
                selected?new Color32(242,245,250,255):new Color32(171,181,199,255),
                new Vector2(26,0),new Vector2(370,39),1f,new Vector2(0,.5f),new Vector2(0,.5f),
                TextAlignmentOptions.MidlineLeft);
            titleText.fontWeight=FontWeight.SemiBold;
            if(selected)titleText.rectTransform.anchoredPosition+=new Vector2(0,9);
            TMP_Text subtitleText=null;
            if(!string.IsNullOrEmpty(subtitle))subtitleText=MenuTextAt(rect,"Subtitle",subtitle,12,fonts.ui,
                FontStyles.Normal,new Color32(142,154,181,255),new Vector2(26,-23),new Vector2(370,20),0,
                new Vector2(0,.5f),new Vector2(0,.5f),TextAlignmentOptions.MidlineLeft);
            if(subtitleText!=null)subtitleText.fontWeight=FontWeight.SemiBold;
            if(selected)titleText.rectTransform.anchoredPosition+=new Vector2(0,9);
            var markerRect=PremiumUiFactory.Rect("Selected Indicator",rect);
            MenuPlace(markerRect,new Vector2(0,.5f),new Vector2(0,.5f),new Vector2(0,0),new Vector2(3,46));
            var marker=markerRect.gameObject.AddComponent<Image>();marker.color=new Color32(46,139,255,255);
            marker.gameObject.SetActive(selected);
            rect.gameObject.AddComponent<MenuNavigationHover>().Configure(titleText,subtitleText,marker,selected);
        }
        private static void MenuIconButton(Transform parent,string name,Vector2 topRightPosition,
            UnityEngine.Events.UnityAction action,MenuIconType type)
        {
            var rect=PremiumUiFactory.Rect(name,parent);
            MenuPlace(rect,new Vector2(1,1),new Vector2(1,1),topRightPosition,new Vector2(48,48));
            var hit=rect.gameObject.AddComponent<Image>();hit.color=Color.clear;hit.raycastTarget=true;
            var plateRect=PremiumUiFactory.Rect("Plate",rect);
            MenuPlace(plateRect,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(48,48));
            var plate=plateRect.gameObject.AddComponent<Image>();plate.color=new Color32(12,16,25,204);plate.raycastTarget=false;
            var outline=plateRect.gameObject.AddComponent<Outline>();outline.effectColor=new Color32(42,53,80,255);
            outline.effectDistance=new Vector2(1,-1);outline.useGraphicAlpha=false;
            var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=plate;
            button.navigation=new Navigation{mode=Navigation.Mode.None};
            UnityEventTools.AddPersistentListener(button.onClick,action);
            string path="Assets/_CrashSimulator/UI/Icons/"+type+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new System.InvalidOperationException("Missing reference icon: "+path);
            if(importer.textureType!=TextureImporterType.Sprite)
            {
                importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
                importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
            }
            var icon=PremiumUiFactory.Rect("Icon",rect);
            MenuPlace(icon,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20));
            var image=icon.gameObject.AddComponent<Image>();image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.raycastTarget=false;
        }
        private static RectTransform MenuPlace(RectTransform rect,Vector2 anchor,Vector2 pivot,
            Vector2 position,Vector2 size)
        {
            rect.anchorMin=rect.anchorMax=anchor;rect.pivot=pivot;
            rect.anchoredPosition=position;rect.sizeDelta=size;return rect;
        }
        private static void BuildGarage(RectTransform garage,PremiumMenuController menu,VehiclePreviewTurntable preview,
            PremiumUiTheme theme,out Text spec,out Text selection)
        {
            var rail=PremiumUiFactory.Panel(garage,"Vehicle Details",PremiumUiKind.GlassPanel,
                new Vector2(0,.5f),new Vector2(0,.5f),Vector2.zero,new Vector2(585,900));
            TextAt(rail,"Heading","GARAGE",48,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(48,-70),new Vector2(490,70));
            TextAt(rail,"Subheading","SELECT YOUR TEST VEHICLE",18,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(48,-143),new Vector2(490,42));
            spec=TextAt(rail,"Vehicle Specifications","PROTOTYPE",22,TextAnchor.UpperLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(48,-236),new Vector2(480,350));
            selection=TextAt(rail,"Selection State","SELECTED  /  PROTOTYPE",19,TextAnchor.MiddleLeft,theme.success,
                Vector2.zero,Vector2.zero,new Vector2(48,225),new Vector2(470,42));
            PremiumUiFactory.Button(rail,"Select Vehicle","SELECT VEHICLE",PremiumUiKind.PrimaryButton,
                Vector2.zero,Vector2.zero,new Vector2(48,130),new Vector2(400,68),menu.SelectPrototype);
            PremiumUiFactory.Button(rail,"Back","←  BACK",PremiumUiKind.SecondaryButton,
                Vector2.zero,Vector2.zero,new Vector2(48,45),new Vector2(230,64),menu.ShowHome);
            TextAt(garage,"Rotation Hint","DRAG TO ROTATE",18,TextAnchor.MiddleCenter,theme.secondaryText,
                new Vector2(.73f,0),new Vector2(.5f,0),new Vector2(0,45),new Vector2(300,36));
            var drag=PremiumUiFactory.Rect("Vehicle Rotation Area",garage);
            drag.anchorMin=new Vector2(.4f,.12f);drag.anchorMax=new Vector2(1,.88f);
            drag.offsetMin=drag.offsetMax=Vector2.zero;
            var image=drag.gameObject.AddComponent<Image>();image.color=new Color(0,0,0,0);image.raycastTarget=true;
            drag.gameObject.AddComponent<VehiclePreviewDragTarget>().Configure(preview);
        }
        private static void BuildSettings(RectTransform screen,PremiumMenuController menu,PremiumUiTheme theme)
        {
            var shade=PremiumUiFactory.Rect("Dim Background",screen);
            PremiumUiFactory.Stretch(shade,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            shade.gameObject.AddComponent<Image>().color=new Color(0,0,0,.68f);
            var panel=PremiumUiFactory.Panel(screen,"Settings Panel",PremiumUiKind.ModalPanel,
                new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(920,765));
            TextAt(panel,"Settings Title","SETTINGS",43,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-28),new Vector2(780,66));
            TextAt(panel,"Graphics Heading","GRAPHICS",20,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-110),new Vector2(410,38));
            var settings=panel.gameObject.AddComponent<PremiumSettingsScreen>();
            var low=PremiumUiFactory.Button(panel,"Low Quality","LOW",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-155),new Vector2(116,54),settings.SetLow);
            var mid=PremiumUiFactory.Button(panel,"Medium Quality","MED",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(168,-155),new Vector2(116,54),settings.SetMedium);
            var high=PremiumUiFactory.Button(panel,"High Quality","HIGH",PremiumUiKind.SecondaryButton,
                new Vector2(0,1),new Vector2(0,1),new Vector2(294,-155),new Vector2(116,54),settings.SetHigh);
            var lowMarker=Marker(low.transform,theme.blue);
            var midMarker=Marker(mid.transform,theme.blue);
            var highMarker=Marker(high.transform,theme.blue);
            var graphics=TextAt(panel,"Graphics Value","MEDIUM",18,TextAnchor.MiddleLeft,theme.secondaryText,
                new Vector2(0,1),new Vector2(0,1),new Vector2(42,-214),new Vector2(390,34));
            var steering=SettingSlider(panel,theme,"Steering Sensitivity",new Vector2(42,-262),.5f,1.5f,1);
            var opacity=SettingSlider(panel,theme,"Button Opacity",new Vector2(42,-354),.4f,1,.7f);
            var handed=SettingToggle(panel,theme,"Swapped Control Layout",new Vector2(42,-454));
            TextAt(panel,"Audio Heading","AUDIO",20,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(480,-110),new Vector2(385,38));
            var master=SettingSlider(panel,theme,"Master Volume",new Vector2(480,-165),0,1,1);
            var effects=SettingSlider(panel,theme,"Effects Volume",new Vector2(480,-257),0,1,1);
            TextAt(panel,"Gameplay Heading","GAMEPLAY",20,TextAnchor.MiddleLeft,theme.blue,
                new Vector2(0,1),new Vector2(0,1),new Vector2(480,-360),new Vector2(385,38));
            var shake=SettingToggle(panel,theme,"Camera Shake",new Vector2(480,-414));
            var motion=SettingToggle(panel,theme,"Reduced UI Motion",new Vector2(480,-490));
            var mph=SettingToggle(panel,theme,"Speed in mph",new Vector2(480,-566));
            PremiumUiFactory.Button(panel,"Back","←  BACK",PremiumUiKind.SecondaryButton,
                Vector2.zero,Vector2.zero,new Vector2(42,35),new Vector2(195,62),menu.ShowHome);
            settings.Configure(steering,opacity,master,effects,handed,shake,motion,mph,graphics,
                lowMarker,midMarker,highMarker);
            lowMarker.SetActive(false);midMarker.SetActive(true);highMarker.SetActive(false);
        }
        private static GameObject Marker(Transform parent,Color color)
        {
            var rect=PremiumUiFactory.Rect("Selected Marker",parent);
            PremiumUiFactory.Place(rect,new Vector2(.5f,0),new Vector2(.5f,0),new Vector2(0,3),new Vector2(74,4));
            rect.gameObject.AddComponent<Image>().color=color;return rect.gameObject;
        }
        private static Slider SettingSlider(Transform parent,PremiumUiTheme theme,string label,Vector2 top,
            float min,float max,float value)
        {
            TextAt(parent,label+" Label",label,19,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),top,new Vector2(405,32));
            return PremiumUiFactory.Slider(parent,label,new Vector2(0,1),new Vector2(0,1),
                top+new Vector2(0,-38),new Vector2(385,42),min,max,value);
        }
        private static Toggle SettingToggle(Transform parent,PremiumUiTheme theme,string label,Vector2 top)
        {
            TextAt(parent,label+" Label",label,19,TextAnchor.MiddleLeft,theme.primaryText,
                new Vector2(0,1),new Vector2(0,1),top,new Vector2(310,50));
            return PremiumUiFactory.Toggle(parent,label,new Vector2(0,1),new Vector2(0,1),
                top+new Vector2(320,0),false);
        }
        private static void BuildLoading(RectTransform screen,PremiumUiTheme theme)
        {
            var bg=PremiumUiFactory.Rect("Background",screen);
            PremiumUiFactory.Stretch(bg,Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
            bg.gameObject.AddComponent<Image>().color=theme.background;
            TextAt(screen,"Loading Title","PREPARING SIMULATION",38,TextAnchor.MiddleCenter,theme.primaryText,
                new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(850,86));
            TextAt(screen,"Loading Hint","INITIALIZING TEST TRACK",19,TextAnchor.MiddleCenter,theme.blue,
                new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(0,-56),new Vector2(700,46));
        }
        public static void RegisterBuildScenes()
        {
            var scenes=new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath,true),
                new EditorBuildSettingsScene("Assets/Scenes/TestTrack.unity",true)
            };
            foreach(var existing in EditorBuildSettings.scenes)
                if(existing.path!=ScenePath && existing.path!="Assets/Scenes/TestTrack.unity")scenes.Add(existing);
            EditorBuildSettings.scenes=scenes.ToArray();
        }
    }
}
