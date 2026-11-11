using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Builds a clean, color-coded level-design blockout before any P&W modules are placed.</summary>
public static class CreateCitadelBlockout
{
    const string ScenePath = "Assets/Scenes/CiudadelaMaya_Blockout.unity";
    const string TerrainPath = "Assets/TemploNavMesh/Environment/CiudadelaBlockoutTerrain.asset";
    const string RootName = "BLOCKOUT_Ciudadela_Maya_220x180";
    static Transform root;
    static Material ground, stone, high, route, mazeMat, tunnelMat, templeMat, green;

    [MenuItem("Tools/NavMesh Tutorial/Build clean Maya Citadel blockout")]
    public static void BuildInEditor()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            EditorUtility.DisplayDialog("Abre la escena Blockout", "El blockout solo se crea en " + ScenePath, "OK");
            return;
        }
        Build();
    }

    public static void BatchBuild()
    {
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    static void Build()
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (!go.GetComponent<Camera>() && !go.GetComponent<Light>()) Undo.DestroyObjectImmediate(go);
        root = new GameObject(RootName).transform;
        root.position = Vector3.zero;
        ground = Mat("Blockout_Terreno", new Color(.34f,.39f,.30f));
        stone = Mat("Blockout_Piedra_baja", new Color(.55f,.48f,.37f));
        high = Mat("Blockout_Terrazas", new Color(.68f,.57f,.38f));
        route = Mat("Blockout_Rutas_y_escaleras", new Color(.79f,.68f,.43f));
        mazeMat = Mat("Blockout_Maze", new Color(.63f,.43f,.30f));
        tunnelMat = Mat("Blockout_Subterraneo", new Color(.24f,.25f,.24f));
        templeMat = Mat("Blockout_Templo_y_mirador", new Color(.81f,.72f,.53f));
        green = Mat("Blockout_Zonas_vegetacion", new Color(.28f,.39f,.25f));
        var terrain = CreateTerrain();
        var layout = Group("01_Recorrido_y_plazas", root);
        BuildPlazasAndTerraces(layout);
        BuildStairsAndSideRoutes(layout);
        BuildMaze(layout);
        BuildTunnel(layout);
        BuildTemple(layout);
        BuildVegetationZones(layout);
        ConfigureCamera();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        File.WriteAllText("Assets/TemploNavMesh/CiudadelaMaya_Preproduccion.md", DesignBrief());
        AssetDatabase.ImportAsset("Assets/TemploNavMesh/CiudadelaMaya_Preproduccion.md");
        Selection.activeGameObject = root.gameObject;
        Debug.Log("Clean level-design blockout built: " + ScenePath + " terrain=" + terrain.name);
    }

    static Terrain CreateTerrain()
    {
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainPath);
        if (data == null)
        {
            data = new TerrainData { heightmapResolution = 513, size = new Vector3(300, 65, 260) };
            AssetDatabase.CreateAsset(data, TerrainPath);
        }
        data.heightmapResolution = 513;
        data.size = new Vector3(300,65,260);
        var h = new float[513,513];
        for (int z=0; z<513; z++)
        for (int x=0; x<513; x++)
        {
            float wx=-150f+x*300f/512f, wz=-110f+z*260f/512f;
            float side=Mathf.Max(0, (Mathf.Abs(wx)-112f)*.72f);
            float back=Mathf.Max(0, (wz-62f)*.64f);
            float front=Mathf.Max(0, (-wz-89f)*.52f);
            float ridges=(Mathf.Sin(wx*.12f+wz*.06f)+Mathf.Sin(wz*.16f-wx*.04f)) *
                Mathf.Clamp01((Mathf.Abs(wx)-55f)/45f)*1.15f;
            // Cut a short, broad trench through the terrain for the lower passage.
            float channel=Mathf.Clamp01((7.2f-Mathf.Abs(wx-25f))/2.2f) *
                Mathf.Clamp01((wz+67f)/8f) * Mathf.Clamp01((74f-wz)/9f) * 8f;
            float y=Mathf.Max(0, side+back+front+ridges)-channel;
            h[z,x]=Mathf.Clamp01((y+10f)/65f);
        }
        data.SetHeights(0,0,h);
        var layers = new[] {
            AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TemploNavMesh/Environment/TemploMayaRock.terrainlayer"),
            AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TemploNavMesh/Environment/TemploMayaSoil.terrainlayer"),
            AssetDatabase.LoadAssetAtPath<TerrainLayer>("Assets/TemploNavMesh/Environment/TemploMayaGrass.terrainlayer")
        };
        if (Array.TrueForAll(layers, x => x != null))
        {
            data.terrainLayers = layers;
            data.alphamapResolution = 256;
            var a = new float[256,256,3];
            for(int z=0;z<256;z++) for(int x=0;x<256;x++)
            {
                float wx=-150f+x*300f/255f, wz=-110f+z*260f/255f;
                float slope=Mathf.Clamp01((Mathf.Abs(wx)-90f)/45f + Mathf.Max(0,(wz-55f)/55f) + Mathf.Max(0,(-wz-82f)/40f));
                a[z,x,0]=Mathf.Lerp(.12f,.76f,slope); a[z,x,1]=Mathf.Lerp(.08f,.2f,slope); a[z,x,2]=1f-a[z,x,0]-a[z,x,1];
            }
            data.SetAlphamaps(0,0,a);
        }
        var t=Terrain.CreateTerrainGameObject(data);
        t.name="TERRENO_REAL_Explanada_Y0_y_laderas_esculpidas";
        t.transform.position=new Vector3(-150,-10,-110);
        t.transform.SetParent(root,false); t.isStatic=true;
        return t.GetComponent<Terrain>();
    }

    static void BuildPlazasAndTerraces(Transform p)
    {
        var g=Group("Plataformas_blockout_temporales_no_assets_PW",p);
        Box(g,"00_Plaza_inicio",0,0,-70,48,.5f,25,stone);
        Box(g,"01_Avenida_de_ingreso",0,0,-49,18,.5f,19,route);
        Box(g,"02_Gran_patío_central",0,4,5,72,.5f,45,high);
        Box(g,"03_Terraza_oeste",-61,4,8,42,.5f,48,high);
        Box(g,"04_Terraza_este",61,4,8,42,.5f,48,high);
        Box(g,"05_Galeria_superior_oeste",-55,8,42,48,.5f,16,high);
        Box(g,"06_Galeria_superior_este",55,8,42,48,.5f,16,high);
        Box(g,"07_Patio_elevado_oeste",-45,8,62,36,.5f,22,high);
        Box(g,"08_Patio_elevado_este",45,8,62,36,.5f,22,high);
        Box(g,"09_Terraza_del_templo",0,12,66,56,.5f,32,templeMat);
        Box(g,"10_Patio_posterior",25,0,91,48,.5f,20,stone);
    }

    static void BuildStairsAndSideRoutes(Transform p)
    {
        var g=Group("Escalinatas_rampas_y_galerias_conectadas",p);
        StairFlight(g,"Escalinata_mayor_inicio_a_patío",0,-47,0,-18,28,15,0,4);
        StairFlight(g,"Escalera_patío_a_galeria_alta",0,27,0,43,23,9,4,8);
        StairFlight(g,"Escalera_galeria_a_mirador",0,50,0,55,30,8,8,12);
        StairFlight(g,"Escalera_oeste_a_patío",-39,-8,-39,1,15,8,0,4);
        StairFlight(g,"Escalera_este_a_patío",39,-8,39,1,15,8,0,4);
        StairFlight(g,"Escalera_oeste_superior",-51,31,-51,43,15,6,4,8);
        StairFlight(g,"Escalera_este_superior",51,31,51,43,15,6,4,8);
        // Side routes use repeated frames as open gallery bays, with clear walking width.
        for(int side=-1;side<=1;side+=2)
        {
            for(int i=0;i<6;i++)
            {
                float z=-39+i*13;
                Frame(g,"Marco_lateral_"+side+"_"+i,side*53,0,z,4.4f,6.8f,stone);
                Box(g,"Piso_galeria_lateral_"+side+"_"+i,side*53,0,z+6,9,.35f,13,route);
            }
        }
    }

    static void BuildMaze(Transform p)
    {
        var g=Group("Maze_facil_en_terraza_oeste",p);
        Box(g,"Piso_maze",-61,4,8,32,.36f,31,stone);
        // Compact 5-turn route, broad passages and direct exit at the north-east corner.
        Wall(g,"Maze_sur_oeste",-70,4, -6,2,2.6f,18);
        Wall(g,"Maze_sur_este",-53,4,-6,2,2.6f,18);
        Wall(g,"Maze_oeste",-77,4,7,2,2.6f,24);
        Wall(g,"Maze_este_bajo",-45,4,0,2,2.6f,12);
        Wall(g,"Maze_este_alto",-45,4,15,2,2.6f,9);
        Wall(g,"Maze_fila1_izq",-68,4,4,14,2.6f,2);
        Wall(g,"Maze_fila1_der",-51,4,4,8,2.6f,2);
        Wall(g,"Maze_fila2",-62,4,12,22,2.6f,2);
        Wall(g,"Maze_fila3",-69,4,20,12,2.6f,2);
        Sign(g,"Entrada maze → salida al patio",-61,5, -9,mazeMat);
    }

    static void BuildTunnel(Transform p)
    {
        var g=Group("Tunel_corto_bajo_terraza_hacia_patío_posterior",p);
        // 7 m wide traversable corridor cut into the terrain; top is segmented as a structural ceiling.
        Box(g,"Suelo_tunel",25,-7,-9,7,.5f,118,tunnelMat);
        Box(g,"Muro_tunel_oeste",20.8f,-4,-9,.5f,6,118,tunnelMat);
        Box(g,"Muro_tunel_este",29.2f,-4,-9,.5f,6,118,tunnelMat);
        for(int z=-62;z<=45;z+=12) Box(g,"Dovela_techo_tunel_"+z,25,-.8f,z,7,.6f,12,tunnelMat);
        Frame(g,"Portal_entrada_subterranea",25,-7,-68,7,5,tunnelMat);
        Frame(g,"Portal_salida_patio_posterior",25,-7,52,7,5,tunnelMat);
        Box(g,"Camino_posterior_del_tunel",25,0,69,7,.3f,25,route);
    }

    static void BuildTemple(Transform p)
    {
        var g=Group("Volumen_templo_y_mirador_blockout",p);
        Box(g,"Basamento_del_templo",0,12,68,28,2.2f,16,stone);
        for(int side=-1;side<=1;side+=2)
        {
            Box(g,"Muro_lateral_templo_"+side,side*10,15,68,3,5,12,templeMat);
            Box(g,"Columna_frontal_templo_"+side,side*6,16,60,2,7,2,templeMat);
        }
        Box(g,"Dintel_y_techumbre_silueta",0,19.2f,68,24,1.2f,5,templeMat);
        Sign(g,"FINAL: TEMPLO + MIRADOR",0,20,79,templeMat);
    }

    static void BuildVegetationZones(Transform p)
    {
        var g=Group("Manchas_de_vegetacion_a_rellenar_despues",p);
        foreach(var v in new[]{new Vector3(-103,0,-55),new Vector3(103,0,-55),new Vector3(-101,0,3),new Vector3(101,0,3),new Vector3(-103,0,57),new Vector3(103,0,57),new Vector3(-55,0,90),new Vector3(95,0,94)})
            Box(g,"Zona_vegetacion",v.x,0,v.z,18,.08f,23,green);
    }

    static void StairFlight(Transform p,string n,float x0,float z0,float x1,float z1,float width,int count,float y0,float y1)
    {
        var dx=x1-x0; var dz=z1-z0; var len=Mathf.Sqrt(dx*dx+dz*dz);
        for(int i=0;i<count;i++)
        {
            float t=(i+.5f)/count, y=Mathf.Lerp(y0,y1,t), z=z0+dz*t;
            var step=Box(p,n+"_peldaño_"+(i+1),x0+dx*t,y-.11f,z,width,.22f,len/count+.12f,route);
            step.transform.rotation=Quaternion.Euler(0,Mathf.Atan2(dx,dz)*Mathf.Rad2Deg,0);
        }
        Box(p,n+"_descanso_superior",x1,y1,z1,width,.35f,5,high);
    }

    static void Frame(Transform p,string n,float x,float y,float z,float width,float height,Material m)
    {
        Box(p,n+"_poste_izq",x-width/2,y+height/2,z,.8f,height,.8f,m);
        Box(p,n+"_poste_der",x+width/2,y+height/2,z,.8f,height,.8f,m);
        Box(p,n+"_dintel",x,y+height,z,width+1,.8f,.9f,m);
    }
    static void Wall(Transform p,string n,float x,float y,float z,float w,float h,float d)=>Box(p,n,x,y+h/2,z,w,h,d,mazeMat);
    static GameObject Box(Transform p,string n,float x,float y,float z,float w,float h,float d,Material m)
    {
        var go=GameObject.CreatePrimitive(PrimitiveType.Cube); go.name=n; go.transform.SetParent(p,false);
        go.transform.localPosition=new Vector3(x,y,z); go.transform.localScale=new Vector3(w,h,d);
        go.GetComponent<Renderer>().sharedMaterial=m; go.isStatic=true; return go;
    }
    static void Sign(Transform p,string n,float x,float y,float z,Material m)
    {
        var go=new GameObject(n); go.transform.SetParent(p,false); go.transform.localPosition=new Vector3(x,y,z);
        var tm=go.AddComponent<TextMesh>(); tm.text=n; tm.characterSize=1.2f; tm.fontSize=64; tm.anchor=TextAnchor.MiddleCenter; tm.color=m.color;
    }
    static Material Mat(string n,Color c)
    {
        var path="Assets/TemploNavMesh/Materials/"+n+".mat";
        var saved=AssetDatabase.LoadAssetAtPath<Material>(path);
        if(saved!=null){saved.color=c;EditorUtility.SetDirty(saved);return saved;}
        var sh=Shader.Find("Universal Render Pipeline/Lit"); if(sh==null) sh=Shader.Find("Standard");
        var m=new Material(sh){name=n,color=c}; AssetDatabase.CreateAsset(m,path); return m;
    }
    static Transform Group(string n,Transform p){var g=new GameObject(n).transform;g.SetParent(p,false);return g;}
    static void ConfigureCamera()
    {
        var cam=SceneManager.GetActiveScene().GetRootGameObjects();
        Camera c=null; Light sun=null;
        foreach(var go in cam){if(c==null)c=go.GetComponentInChildren<Camera>();if(sun==null)sun=go.GetComponentInChildren<Light>();}
        if(c){c.transform.position=new Vector3(0,145,-230);c.transform.rotation=Quaternion.LookRotation(new Vector3(0,4,0)-c.transform.position);c.fieldOfView=55;c.farClipPlane=700;}
        if(sun){sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.intensity=1.2f;sun.shadows=LightShadows.Soft;}
    }
    static string DesignBrief()=>"# Blockout de preproducción — Ciudadela Maya\n\n"+
        "Escena limpia e independiente. No contiene prefabs de P&W: todos los volúmenes son temporales y codificados por color.\n\n"+
        "## Recorrido aprobado\nPlaza inicial → escalinata ceremonial → gran patio central → terrazas oeste/este → galerías superiores y patios elevados → templo y mirador. Rutas laterales abiertas con marcos. Maze fácil en la terraza oeste. Túnel corto bajo la terraza que enlaza la plaza inicial con el patio posterior.\n\n"+
        "## Criterios del blockout\nTerreno Unity de 300×260 m con explanada central nominal en Y=0 y laderas esculpidas en el perímetro. Terrazas a Y=4, 8 y 12 m. Escaleras representadas por peldaños repetidos y descansos. Mantener el maze corto y legible. No integrar NavMesh ni reemplazar geometría temporal por assets hasta revisar proporciones y recorrido.\n\n"+
        "## Siguiente fase\nRevisar esta escena; inventariar módulos P&W por dimensiones y conectores; seleccionar equivalentes para pisos, muros, arcos, columnas y escaleras; reemplazar el blockout por módulos alineados, conservando el terreno, niveles y recorrido aprobados.";
}
