using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Asset based preview of the approved Maya citadel blockout. Keeps terrain and route, excludes the rejected corner prefab.</summary>
public static class BuildCitadelAssetPreview
{
    const string ScenePath="Assets/Scenes/CiudadelaMaya_AssetsPreview.unity";
    const string PrefabOutput="Assets/TemploNavMesh/Prefabs/CiudadelaMaya_DesdeFotos_Revision.prefab";
    const string PrefabRoot="Assets/(P&W)Temple_Edition/Prefabs/";
    const string Floor="High_Detailed/Platforms/HD_Platform_6A.prefab";
    const string Wall="High_Detailed/Walls/HD_Wall_2A.prefab";
    const string Arch="High_Detailed/Archs/HD_Arch_1A.prefab";
    const string TempleSteps="Basic/Platforms/Basic_Stairs_1A.prefab";
    const string SmallArch="Basic/Walls/Basic_Arch_1A.prefab";
    const string MazeWall="Unique/Unique_Wall_B.prefab";
    const string Tunnel="High_Detailed/Archs/HD_Tunnel_1A.prefab";
    const string TunnelRamp="High_Detailed/Archs/HD_Tunnel_Ramp_1B.prefab";
    const string Temple="Unique/Structures/Entrance_1A.prefab";
    const string Torch="High_Detailed/Small_Objects/Torch_Post_1A.prefab";
    const string Bowl="High_Detailed/Small_Objects/Bowl_1A.prefab";
    const string PillarA="Basic/Walls/Basic_Pillar_1A.prefab";
    const string PillarB="Basic/Walls/Basic_Pillar_1B.prefab";
    const string PillarC="Basic/Walls/Basic_Pillar_1C.prefab";
    const string Railing1="High_Detailed/Railings/HD_Railing_1A.prefab";
    const string Railing2="High_Detailed/Railings/HD_Railing_2A.prefab";
    const string Railing3="High_Detailed/Railings/HD_Railing_3A.prefab";
    const string Railing4="High_Detailed/Railings/HD_Railing_4A.prefab";
    const string FullRailing="Unique/Unique_Full_Railing_1.prefab";
    const string RailingPost1="High_Detailed/Railings/HD_Railing_Post_1A.prefab";
    const string RailingPost2="High_Detailed/Railings/HD_Railing_Post_2A.prefab";
    const string TunnelArchA="High_Detailed/Archs/HD_Tunnel_Arch_1A.prefab";
    const string TunnelArchB="High_Detailed/Archs/HD_Tunnel_Arch_1B.prefab";
    const string Oak="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Oak_Tree.prefab";
    const string Pine="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Pine_Tree.prefab";
    const string Grass1="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_1.prefab";
    const string Grass2="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_2.prefab";
    const string Grass3="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_3.prefab";
    const string Grass4="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Grass_4.prefab";
    const string Rock1="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_1.prefab";
    const string Rock2="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_2.prefab";
    const string Rock3="Assets/ZNS3D/FREE_VEGETATION_PACK/PREFABS/Rock_3.prefab";
    const string TerrainDataPath="Assets/TemploNavMesh/Environment/CiudadelaBlockoutTerrain.asset";
    const string PreviewTerrainPath="Assets/TemploNavMesh/Environment/CiudadelaAssetsTerrain.asset";
    const string CircularVaultFloorMeshPath="Assets/TemploNavMesh/Environment/CircularVaultFloor.asset";
    const string CircularVaultDomeMeshPath="Assets/TemploNavMesh/Environment/CircularVaultDome.asset";
    const string AuditPath="Assets/TemploNavMesh/CiudadelaAssetPlacementAudit.txt";
    static Transform root, architecture;
    static readonly StringBuilder audit=new StringBuilder();
    static readonly System.Collections.Generic.Dictionary<Material,Material> foliageMaterialCache=new System.Collections.Generic.Dictionary<Material,Material>();
    static int floorCount,wallCount,archCount,stairCount,tunnelCount,treeCount,grassCount,rockCount,rampCount,accentCount,stairSupportCount,stairLandingCount,vaultColumnCount,vaultTorchCount,vaultRealtimeLightCount,templeUndercroftPierCount,raisedTerracePierCount;

    [MenuItem("Tools/NavMesh Tutorial/Build P&W Maya Citadel asset preview")]
    public static void BuildInEditor()
    {
        if(SceneManager.GetActiveScene().path!=ScenePath){EditorUtility.DisplayDialog("Escena de preview","Abre primero "+ScenePath,"OK");return;}
        Build();
    }
    public static void BatchBuild()
    {
        try{var s=EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);EditorSceneManager.SaveScene(s,ScenePath);Build();EditorApplication.Exit(0);}
        catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}
    }
    static void Build()
    {
        audit.Clear();floorCount=wallCount=archCount=stairCount=tunnelCount=treeCount=grassCount=rockCount=rampCount=accentCount=stairSupportCount=stairLandingCount=vaultColumnCount=vaultTorchCount=vaultRealtimeLightCount=templeUndercroftPierCount=raisedTerracePierCount=0;
        foliageMaterialCache.Clear();
        var required=new[]{Floor,Wall,Arch,TempleSteps,SmallArch,MazeWall,Tunnel,TunnelRamp,Temple,Torch,Bowl,PillarA,PillarB,PillarC,Railing1,Railing2,Railing3,Railing4,FullRailing,RailingPost1,RailingPost2,TunnelArchA,TunnelArchB,"High_Detailed/Archs/HD_Arch_2A.prefab"};
        var missing=required.Select(p=>PrefabRoot+p).Concat(new[]{Oak,Pine,Grass1,Grass2,Grass3,Grass4,Rock1,Rock2,Rock3}).Where(p=>AssetDatabase.LoadAssetAtPath<GameObject>(p)==null).ToArray();
        if(missing.Length>0)throw new InvalidOperationException("P&W assets missing:\n"+string.Join("\n",missing));
        foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects())if(!go.GetComponent<Camera>()&&!go.GetComponent<Light>())Undo.DestroyObjectImmediate(go);
        root=new GameObject("Ciudadela_Maya_PW_Revision_de_Assets").transform;
        var terrain=AddTerrain(root);
        architecture=Group("01_Arquitectura_PW_alineada_a_blockout",root);
        BuildFloors();BuildRetainingWalls();BuildStairs();BuildSideFrames();BuildMaze();BuildTunnels();BuildTemple();BuildTempleUndercroftSupports();BuildDetails();BuildStructuralAccents(terrain);
        BuildVegetation(terrain);
        ConfigureCamera();
        AddReviewPlayer();
        audit.Insert(0,"AUDITORÍA DE MONTAJE — Ciudadela Maya P&W\n\n");
        audit.AppendLine("Conteos: losas="+floorCount+", módulos de muro="+wallCount+", arcos="+archCount+", escaleras="+stairCount+", rampas de túnel="+rampCount+", módulos de túnel="+tunnelCount+", acentos P&W nuevos="+accentCount+", árboles="+treeCount+", pastos="+grassCount+", rocas="+rockCount);
        audit.AppendLine("Apoyos macizos bajo escaleras="+stairSupportCount+", descansos intermedios de accesos laterales="+stairLandingCount+".");
        audit.AppendLine("Pilas adicionales bajo terrazas elevadas="+raisedTerracePierCount+"; cada pila llega del terreno al intradós y deja libre el eje ceremonial.");
        audit.AppendLine("Bóveda: columnas portantes="+vaultColumnCount+", antorchas="+vaultTorchCount+", luces puntuales realtime="+vaultRealtimeLightCount+".");
        audit.AppendLine("Basamento bajo la terraza del templo: pilares portantes="+templeUndercroftPierCount+"; tres módulos P&W por pilar, desde Y=0.00 hasta el intradós Y=11.88 m.");
        audit.AppendLine("Escena de preview separada; no incluye NavMesh.");
        audit.AppendLine("Single_Group_Tall_Corner_1A excluido deliberadamente.");
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        PrefabUtility.SaveAsPrefabAsset(root.gameObject,PrefabOutput);
        AssetDatabase.SaveAssets();
        File.WriteAllText(AuditPath,audit.ToString());AssetDatabase.ImportAsset(AuditPath);
        Selection.activeGameObject=root.gameObject;
    }
    static Terrain AddTerrain(Transform parent)
    {
        if(!AssetDatabase.LoadAssetAtPath<TerrainData>(PreviewTerrainPath))
            if(!AssetDatabase.CopyAsset(TerrainDataPath,PreviewTerrainPath))throw new InvalidOperationException("No pude copiar el TerrainData del blockout.");
        var d=AssetDatabase.LoadAssetAtPath<TerrainData>(PreviewTerrainPath);if(!d)throw new InvalidOperationException("No existe copia del terreno para el preview.");
        // Keep the approved landscape, replace only its overlong trench with a short, graded cut under the back terraces.
        var heights=new float[513,513];
        for(int z=0;z<513;z++)for(int x=0;x<513;x++)
        {
            float wx=-150+x*300f/512f,wz=-110+z*260f/512f;
            float ax=Mathf.Max(0,(Mathf.Abs(wx)-124)/26f),side=Smooth01(ax)*34f;
            // The rear climb begins beyond the rear courtyard so it cannot bury that floor or its exits.
            float rear=Smooth01(Mathf.Max(0,(wz-105)/40f))*24f;
            float front=Smooth01(Mathf.Max(0,(-wz-89)/30f))*18f;
            // Keep the lateral walkways and their frame structures on the open valley floor.
            float ridge=Mathf.Exp(-Mathf.Pow((Mathf.Abs(wx)-137)/14f,2))*23f;
            float secondary=Mathf.Exp(-Mathf.Pow((wz-140)/18f,2))*Mathf.Exp(-Mathf.Pow(wx/100f,2))*20f;
            float rough=(Mathf.Sin(wx*.105f+wz*.063f)+Mathf.Sin(wz*.19f-wx*.04f)*.55f)*Mathf.Clamp01((Mathf.Abs(wx)-120)/22f)*1.2f;
            float y=Mathf.Max(0,side+rear+front+ridge+secondary+rough);
            float vaultDistance=Vector2.Distance(new Vector2(wx,wz),new Vector2(25f,62f));
            float vaultCover=.15f*(1f-Smooth01(Mathf.InverseLerp(8f,9.5f,vaultDistance)));
            y=Mathf.Max(y,vaultCover);
            if(Mathf.Abs(wx-25f)<3.3f && ((wz>=41f&&wz<=55f)||(wz>=69f&&wz<=83f)))
            {
                float passageY=wz<55f?Mathf.Lerp(.18f,-3.08f,Mathf.InverseLerp(41f,55f,wz)):Mathf.Lerp(-3.08f,.18f,Mathf.InverseLerp(69f,83f,wz));
                float cut=Mathf.Clamp01((3.3f-Mathf.Abs(wx-25f))/.65f);
                y=Mathf.Lerp(y,passageY-.8f,cut);
            }
            heights[z,x]=Mathf.Clamp01((y+10f)/65f);
        }
        d.SetHeights(0,0,heights);EditorUtility.SetDirty(d);
        var t=Terrain.CreateTerrainGameObject(d);t.name="TERRENO_ESCULPIDO_BLOCKOUT_CONSERVADO";t.transform.position=new Vector3(-150,-10,-110);t.transform.SetParent(parent,false);t.isStatic=true;return t.GetComponent<Terrain>();
    }
    static void BuildFloors()
    {
        var group=Group("Losas_PW_modulo_8m_superficie_alineada",architecture);
        TileRect(group,"Plaza_inicio",-24,24,-82,-58,.18f);
        TileRect(group,"Avenida",-8,8,-58,-42,.18f);
        TileRect(group,"Patio_central",-36,36,-19,29,4.18f);
        TileRect(group,"Terraza_oeste",-81,-41,-16,32,4.18f);
        TileRect(group,"Terraza_este",41,81,-16,32,4.18f);
        TileRect(group,"Galeria_superior_oeste",-80,-32,34,50,8.18f);
        TileRect(group,"Galeria_superior_este",32,80,34,50,8.18f);
        TileRect(group,"Patio_alto_oeste",-64,-32,50,74,8.18f);
        TileRect(group,"Patio_alto_este",32,64,50,74,8.18f);
        TileRect(group,"Galeria_superior_central",-16,16,34,50,8.18f);
        // Open the central stairwell so the rising flight can pass through the upper-gallery slab
        // without the player's head colliding with its underside. Side ledges remain supported,
        // and a level landing begins exactly where the stair flight tops out (z=45.27).
        foreach(float side in new[]{-1f,1f})FloorStrip(group,"Hombro_apertura_escalera_galeria_central_"+side,side*5f,43.635f,8.18f,6f,3.27f);
        foreach(float side in new[]{-1f,1f})FloorStrip(group,"Descanso_salida_escalera_galeria_central_"+side,side*4f,49.27f,8.18f,8f,8f);
        TileRect(group,"Galeria_transversal_superior",-32,32,42,50,8.18f);
        TileRect(group,"Terraza_templo",-28,28,50,82,12.18f);
        TileRect(group,"Patio_posterior",1,49,82,98,.18f);
        // In-fill connecting the central stair approach to the first terrace.
        TileRect(group,"Descanso_superior_escalinata",-16,16,-27,-19,4.18f);
        // The main ascent tops out at z=-31.13. A full 8 m tile from -35 to -27
        // creates a 1.4 m vertical collision lip over the final stair run. Use a
        // 4 m landing beginning at -31 so the walking ramp and upper terrace meet.
        FloorStrip(group,"Rellano_de_llegada_escalinata_mayor",0f,-29f,4.18f,32f,4f);
        TileRect(group,"Aproximación_al_patío_de_galería_superior",-16,16,29,37,4.18f);
        foreach(int side in new[]{-1,1})
            PlaceTop("Basic/Platforms/6x6x.25_Floor_A.prefab",group,"Umbral_escalera_lateral_entrada_"+side,new Vector3(side*61,.18f,-21.27f),Vector3.one,Quaternion.identity);
    }
    static void TileRect(Transform parent,string name,float minX,float maxX,float minZ,float maxZ,float top)
    {
        int nx=Mathf.RoundToInt((maxX-minX)/8f),nz=Mathf.RoundToInt((maxZ-minZ)/8f);
        if(Mathf.Abs(nx*8-(maxX-minX))>.01f||Mathf.Abs(nz*8-(maxZ-minZ))>.01f)throw new InvalidOperationException("Retícula 8m no cierra: "+name);
        var g=Group(name,parent);
        for(int z=0;z<nz;z++)for(int x=0;x<nx;x++)
        {
            float wx=minX+4+x*8,wz=minZ+4+z*8;
            if(ShouldOmitFloor(name,wx,wz))continue;
            PlaceTop(Floor,g,name+"_losa_"+x+"_"+z,new Vector3(wx,top,wz),Vector3.one,Quaternion.identity);
            floorCount++;
        }
    }
    static bool ShouldOmitFloor(string zone,float x,float z)
    {
        if((zone=="Terraza_oeste"||zone=="Terraza_este") &&
           (Mathf.Abs(x+(zone=="Terraza_oeste"?61f:-61f))<10f) && z==-12f)return true;
        if((zone=="Terraza_oeste"||zone=="Terraza_este") &&
           (Mathf.Abs(x+(zone=="Terraza_oeste"?55f:-55f))<10f) && z==28f)return true;
        if((zone=="Galeria_superior_oeste"||zone=="Galeria_superior_este") &&
           (Mathf.Abs(x+(zone=="Galeria_superior_oeste"?55f:-55f))<10f) && z==38f)return true;
        // The stair crosses the slab at both rows: z=38 for the lower gallery flight,
        // and z=46 for the upper flight. The upper opening is finished with side ledges
        // and a landing that starts at the stair's top edge.
        if(zone=="Galeria_superior_central" && Mathf.Abs(x)<=12f && z==38f)return true;
        if(zone=="Galeria_superior_central" && Mathf.Abs(x)==4f && z==46f)return true;
        if(zone=="Galeria_transversal_superior" && Mathf.Abs(x)<=12f)return true;
        if(zone=="Terraza_templo" && Mathf.Abs(x)<12f && (z==54f||z==62f))return true;
        return false;
    }
    static void BuildRetainingWalls()
    {
        // 2.2 m P&W stone walls form continuous raised terrace faces. Leave stair and route openings.
        Retain("Contencion_patío_central",-36,36,-19,29,0,4.18f,new[]{0f},new[]{0f,25f},16f,6f);
        Retain("Contencion_oeste",-81,-41,-16,32,0,4.18f,new[]{-61f},Array.Empty<float>(),8f);
        Retain("Contencion_este",41,81,-16,32,0,4.18f,new[]{61f},Array.Empty<float>(),8f);
        // These upper platforms had retaining walls starting at Y=4 m with no lower storey beneath them.
        // Continue the masonry to grade in two aligned courses so the wall bases no longer appear suspended.
        Retain("Contencion_galeria_oeste",-80,-32,34,50,0,8.18f,new[]{-55f},new[]{-55f},8f);
        Retain("Contencion_galeria_este",32,80,34,50,0,8.18f,new[]{55f},new[]{55f},8f);
        Retain("Contencion_patio_alto_oeste",-64,-32,50,74,0,8.18f,new[]{-48f},Array.Empty<float>(),8f);
        Retain("Contencion_patio_alto_este",32,64,50,74,0,8.18f,new[]{48f},Array.Empty<float>(),8f);
        Retain("Contencion_terraza_templo",-28,28,50,82,8,12.18f,new[]{0f},Array.Empty<float>(),12f);
    }
    static void Retain(string name,float l,float r,float f,float b,float low,float top,float[] frontOpen,float[] backOpen,float openingHalfWidth,float backOpeningHalfWidth=-1f)
    {
        var g=Group(name,architecture);
        const float wallModuleHeight=4f;
        int tiers=Mathf.Max(1,Mathf.RoundToInt((top-low)/wallModuleHeight));
        float tierHeight=(top-low)/tiers;
        for(float x=l+1.1f;x<r;x+=2.2f)
        {
            bool open=frontOpen.Any(o=>Mathf.Abs(x-o)<openingHalfWidth);
            bool openBack=backOpen.Any(o=>Mathf.Abs(x-o)<(backOpeningHalfWidth>0?backOpeningHalfWidth:openingHalfWidth));
            for(int tier=0;tier<tiers;tier++)
            {
                float centerY=low+(tier+.5f)*tierHeight;
                if(!open)PlaceCenter(Wall,g,name+"_frente_hilada"+tier+"_"+x,new Vector3(x,centerY,f),new Vector3(1f,tierHeight/wallModuleHeight,1f),Quaternion.identity);
                if(!openBack)PlaceCenter(Wall,g,name+"_fondo_hilada"+tier+"_"+x,new Vector3(x,centerY,b),new Vector3(1f,tierHeight/wallModuleHeight,1f),Quaternion.identity);
            }
            wallCount+=((open?0:1)+(openBack?0:1))*tiers;
        }
        for(float z=f+1.1f;z<b;z+=2.2f)
        {
            for(int tier=0;tier<tiers;tier++)
            {
                float centerY=low+(tier+.5f)*tierHeight;var scale=new Vector3(1f,tierHeight/wallModuleHeight,1f);
                PlaceCenter(Wall,g,name+"_oeste_hilada"+tier+"_"+z,new Vector3(l,centerY,z),scale,Quaternion.Euler(0,90,0));
                PlaceCenter(Wall,g,name+"_este_hilada"+tier+"_"+z,new Vector3(r,centerY,z),scale,Quaternion.Euler(0,90,0));
            }
            wallCount+=2*tiers;
        }
        audit.AppendLine(name+" | basamento continuo Y="+low.ToString("0.00")+" a Y="+top.ToString("0.00")+" | hiladas="+tiers+" | altura por hilada="+tierHeight.ToString("0.00")+" m");
    }
    static void BuildStairs()
    {
        var g=Group("Escaleras_PW_con_llegadas_a_nivel",architecture);
        var supports=Group("Apoyos_macizos_y_muros_de_escalera",architecture);
        // The monumental stairs remain single continuous flights, now with matched 2 m visual modules and solid stone backing.
        var mainXs=new[]{-12f,-8f,-4f,0f,4f,8f,12f};
        foreach(float x in mainXs)PlaceFlight(g,supports,"Escalinata_ceremonial_principal_ala_"+x,new Vector3(x,2.18f,-37.4f),false,false);
        AddStairEdgeWalls(supports,"Muro_lateral_escalinata_principal",-14.6f,-37.4f,.18f,12.54f,4f);
        AddStairEdgeWalls(supports,"Muro_lateral_escalinata_principal",14.6f,-37.4f,.18f,12.54f,4f);

        // Four secondary side stairs gain a level 2 m landing; their top/bottom aprons close the terrace joints.
        PlaceFlight(g,supports,"Escalera_oeste_a_terraza",new Vector3(-61,2.18f,-16.5f),true,true);
        PlaceFlight(g,supports,"Escalera_este_a_terraza",new Vector3(61,2.18f,-16.5f),true,true);
        PlaceFlight(g,supports,"Escalera_oeste_terraza_a_galeria",new Vector3(-55,6.18f,31f),true,true);
        PlaceFlight(g,supports,"Escalera_este_terraza_a_galeria",new Vector3(55,6.18f,31f),true,true);

        var upperXs=new[]{-12f,-8f,-4f,0f,4f,8f,12f};
        foreach(float x in upperXs)PlaceFlight(g,supports,"Escalera_patío_a_galería_superior_ala_"+x,new Vector3(x,6.18f,39f),false,false);
        AddStairEdgeWalls(supports,"Muro_lateral_escalera_galería_superior",-14.6f,39f,4.18f,12.54f,4f);
        AddStairEdgeWalls(supports,"Muro_lateral_escalera_galería_superior",14.6f,39f,4.18f,12.54f,4f);

        var lookoutXs=new[]{-8f,-4f,0f,4f,8f};
        foreach(float x in lookoutXs)PlaceFlight(g,supports,"Escalera_galería_a_mirador_ala_"+x,new Vector3(x,10.18f,55f),false,false);
        AddStairEdgeWalls(supports,"Muro_lateral_escalera_mirador",-10.6f,55f,8.18f,12.54f,4f);
        AddStairEdgeWalls(supports,"Muro_lateral_escalera_mirador",10.6f,55f,8.18f,12.54f,4f);

        // Aprons meet the upper terrace tiles at the edges of the east/west approach flights.
        foreach(int side in new[]{-1,1})
        {
            PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",g,"Remate_superior_escalera_lateral_"+side,new Vector3(side*61,4.18f,-8.9f),new Vector3(1f,1f,.28f),Quaternion.identity);floorCount++;
            PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",g,"Remate_superior_galería_lateral_"+side,new Vector3(side*55,8.18f,40f),new Vector3(1f,1f,.5f),Quaternion.identity);floorCount++;
        }
        CloseSideStairOpenings(g);
        var infill=Group("Remates_4x8_junto_a_escalinata_superior",g);
        foreach(float x in new[]{-14f,14f})foreach(float z in new[]{38f,46f})
        {PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",infill,"Remate_galeria_"+x+"_"+z,new Vector3(x,8.18f,z),Vector3.one,Quaternion.identity);floorCount++;}
        // Close the narrow side strips beside the central stair slots and continue the top landing into the temple apron.
        foreach(float z in new[]{38f,46f})foreach(float side in new[]{-1f,1f})FloorStrip(g,"Remate_borde_escalera_central_"+side+"_"+z,side*15.6f,z,8.18f,.8f,8f);
        foreach(float side in new[]{-1f,1f})foreach(float z in new[]{54f,62f})FloorStrip(g,"Remate_borde_escalera_mirador_"+side+"_"+z,side*11f,z,12.18f,2f,8f);
        foreach(float x in new[]{-6.6667f,0f,6.6667f})FloorStrip(g,"Descanso_superior_escalera_mirador_"+x,x,63.635f,12.18f,6.6667f,4.73f);
    }
    static void CloseSideStairOpenings(Transform parent)
    {
        // The grid intentionally omits 16 m bays for 4 m stairs; fill the remaining stone shoulders to the parapets.
        FloorStrip(parent,"Hombro_oeste_escalera_inferior_A",-64f,-12f,.18f,2f,8f);
        FloorStrip(parent,"Hombro_oeste_escalera_inferior_B",-55f,-12f,.18f,8f,8f);
        FloorStrip(parent,"Hombro_oeste_escalera_inferior_C",-50f,-12f,.18f,2f,8f);
        FloorStrip(parent,"Hombro_este_escalera_inferior_A",50f,-12f,.18f,2f,8f);
        FloorStrip(parent,"Hombro_este_escalera_inferior_B",55f,-12f,.18f,8f,8f);
        FloorStrip(parent,"Hombro_este_escalera_inferior_C",64f,-12f,.18f,2f,8f);
        FloorStrip(parent,"Hombro_oeste_escalera_galería_A",-61f,28f,4.18f,8f,8f);
        FloorStrip(parent,"Hombro_oeste_escalera_galería_B",-51f,28f,4.18f,4f,8f);
        FloorStrip(parent,"Hombro_este_escalera_galería_A",51f,28f,4.18f,4f,8f);
        FloorStrip(parent,"Hombro_este_escalera_galería_B",61f,28f,4.18f,8f,8f);
        FloorStrip(parent,"Hombro_oeste_galería_superior_A",-60.5f,38f,8.18f,7f,8f);
        FloorStrip(parent,"Hombro_oeste_galería_superior_B",-50.5f,38f,8.18f,5f,8f);
        FloorStrip(parent,"Hombro_este_galería_superior_A",50.5f,38f,8.18f,5f,8f);
        FloorStrip(parent,"Hombro_este_galería_superior_B",60.5f,38f,8.18f,7f,8f);
    }
    static void FloorStrip(Transform parent,string name,float x,float z,float topY,float width,float length)
    {
        PlaceTop(Floor,parent,name,new Vector3(x,topY,z),new Vector3(width/8f,1f,length/8f),Quaternion.identity);floorCount++;
    }
    static void PlaceFlight(Transform parent,Transform supportParent,string name,Vector3 center,bool withLanding,bool withSideWalls)
    {
        const float moduleRun=6.27f,landingDepth=1.6f,levelRise=4f,moduleHeight=1.995f;
        float lowY=center.y-levelRise*.5f,run=withLanding?moduleRun*2f+landingDepth:moduleRun*2f;
        float startZ=center.z-run*.5f;
        var flight=new GameObject(name).transform;flight.SetParent(parent,false);
        var profile=new System.Collections.Generic.List<Vector2>();
        if(withLanding)
        {
            float midZ=startZ+moduleRun;
            float landingEnd=midZ+landingDepth;
            DisableAssetColliders(PlaceCenter(TempleSteps,flight,name+"_tramo_inferior",new Vector3(center.x,lowY+moduleHeight*.5f,startZ+moduleRun*.5f),new Vector3(1f,.5f,1f),Quaternion.Euler(0,180,0)));
            PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",flight,name+"_descanso_intermedio",new Vector3(center.x,lowY+2f,midZ+landingDepth*.5f),new Vector3(1f,1f,landingDepth/8f),Quaternion.identity);floorCount++;stairLandingCount++;
            DisableAssetColliders(PlaceCenter(TempleSteps,flight,name+"_tramo_superior",new Vector3(center.x,lowY+3f,landingEnd+moduleRun*.5f),new Vector3(1f,.5f,1f),Quaternion.Euler(0,180,0)));
            profile.Add(new Vector2(startZ,lowY));profile.Add(new Vector2(midZ,lowY+2f));profile.Add(new Vector2(landingEnd,lowY+2f));profile.Add(new Vector2(startZ+run,lowY+4f));
            AddStairRamp(flight,name+"_Superficie_inferior",center.x,startZ,lowY,moduleRun,2f,4f);
            AddStairRamp(flight,name+"_Superficie_superior",center.x,landingEnd,lowY+2f,moduleRun,2f,4f);
        }
        else
        {
            DisableAssetColliders(PlaceCenter(TempleSteps,flight,name+"_tramo_inferior",new Vector3(center.x,lowY+1f,startZ+moduleRun*.5f),new Vector3(1f,.5f,1f),Quaternion.Euler(0,180,0)));
            DisableAssetColliders(PlaceCenter(TempleSteps,flight,name+"_tramo_superior",new Vector3(center.x,lowY+3f,startZ+moduleRun*1.5f),new Vector3(1f,.5f,1f),Quaternion.Euler(0,180,0)));
            profile.Add(new Vector2(startZ,lowY));profile.Add(new Vector2(startZ+run,lowY+4f));
            AddStairRamp(flight,name+"_Superficie_continua",center.x,startZ,lowY,run,4f,4f);
        }
        stairCount+=2;
        BuildStairSupport(supportParent,name+"_macizo_de_apoyo",center.x,4f,profile);
        if(withSideWalls)AddStairSideWalls(supportParent,name+"_barandas_de_piedra",center.x,4f,profile);
    }
    static void DisableAssetColliders(GameObject asset)
    {if(asset)foreach(var c in asset.GetComponentsInChildren<Collider>(true))c.enabled=false;}
    static void AddStairRamp(Transform parent,string name,float x,float startZ,float startY,float run,float rise,float width)
    {
        var walk=new GameObject(name);walk.transform.SetParent(parent,false);
        walk.transform.position=new Vector3(x,startY+rise*.5f,startZ+run*.5f);
        walk.transform.rotation=Quaternion.Euler(-Mathf.Atan2(rise,run)*Mathf.Rad2Deg,0,0);
        walk.AddComponent<BoxCollider>().size=new Vector3(width,.08f,Mathf.Sqrt(run*run+rise*rise));
    }
    static Material StairSupportMaterial()
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot+Wall);
        return asset?asset.GetComponentInChildren<Renderer>(true)?.sharedMaterial:null;
    }
    static void BuildStairSupport(Transform parent,string name,float centerX,float width,System.Collections.Generic.IReadOnlyList<Vector2> profile)
    {
        if(profile==null||profile.Count<2)return;
        float startZ=profile[0].x,bottomY=profile.Min(p=>p.y)-.05f;
        var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.position=new Vector3(centerX,bottomY,startZ);go.isStatic=true;
        var verts=new System.Collections.Generic.List<Vector3>();int n=profile.Count;
        for(int i=0;i<n;i++)
        {
            float z=profile[i].x-startZ,y=profile[i].y-bottomY;
            verts.Add(new Vector3(-width*.5f,y,z)); // top left
            verts.Add(new Vector3(width*.5f,y,z));  // top right
            verts.Add(new Vector3(-width*.5f,0,z)); // bottom left
            verts.Add(new Vector3(width*.5f,0,z));  // bottom right
        }
        var tris=new System.Collections.Generic.List<int>();
        void Quad(int a,int b,int c,int d){tris.Add(a);tris.Add(b);tris.Add(c);tris.Add(a);tris.Add(c);tris.Add(d);}
        for(int i=0;i<n-1;i++)
        {
            int a=i*4,b=(i+1)*4;
            Quad(a,a+4,b+1,a+1);          // sloped/landing top
            Quad(a+2,a+3,b+3,b+2);        // embedded bottom
            Quad(a+2,b+2,b,a);            // left cheek
            Quad(a+3,a+1,b+1,b+3);        // right cheek
        }
        Quad(2,0,1,3);                    // lower end face
        int last=(n-1)*4;Quad(last+2,last+3,last+1,last); // upper end face
        var mesh=new Mesh{name=name+"_mesh"};mesh.SetVertices(verts);mesh.SetTriangles(tris,0);mesh.RecalculateNormals();mesh.RecalculateBounds();
        go.AddComponent<MeshFilter>().sharedMesh=mesh;
        var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=StairSupportMaterial();renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.On;renderer.receiveShadows=true;
        go.AddComponent<MeshCollider>().sharedMesh=mesh;
        stairSupportCount++;audit.AppendLine(name+" | macizo P&W con sección escalonada | ancho="+width.ToString("0.00")+" m | largo="+(profile[profile.Count-1].x-profile[0].x).ToString("0.00")+" m | base enterrada 1.50 m");
    }
    static void AddStairSideWalls(Transform parent,string name,float centerX,float width,System.Collections.Generic.IReadOnlyList<Vector2> profile)
    {
        float start=profile[0].x,end=profile[profile.Count-1].x,run=end-start;
        int segments=Mathf.CeilToInt(run/2.2f);float length=run/segments;
        for(int side=-1;side<=1;side+=2)for(int i=0;i<segments;i++)
        {
            float z=start+(i+.5f)*length,y=ProfileHeight(profile,z);
            PlaceFloor(Wall,parent,name+"_"+side+"_"+i,new Vector3(centerX+side*(width*.5f+.6f),y,z),new Vector3(length/2.2f,.28f,1f),Quaternion.Euler(0,90,0));wallCount++;
        }
    }
    static void AddStairEdgeWalls(Transform parent,string name,float centerX,float centerZ,float startY,float run,float rise)
    {
        float startZ=centerZ-run*.5f;var profile=new[]{new Vector2(startZ,startY),new Vector2(startZ+run,startY+rise)};
        int segments=Mathf.CeilToInt(run/2.2f);float length=run/segments;
        for(int i=0;i<segments;i++)
        {
            float z=startZ+(i+.5f)*length,y=ProfileHeight(profile,z);
            PlaceFloor(Wall,parent,name+"_"+i,new Vector3(centerX,y,z),new Vector3(length/2.2f,.28f,1f),Quaternion.Euler(0,90,0));wallCount++;
        }
    }
    static float ProfileHeight(System.Collections.Generic.IReadOnlyList<Vector2> profile,float z)
    {
        for(int i=1;i<profile.Count;i++)if(z<=profile[i].x)
        {float t=Mathf.InverseLerp(profile[i-1].x,profile[i].x,z);return Mathf.Lerp(profile[i-1].y,profile[i].y,t);}
        return profile[profile.Count-1].y;
    }
    static void BuildSideFrames()
    {
        var g=Group("Galerias_laterales_con_marcos",architecture);
        // A transverse 8 m stone walk ties both outer promenades back into the starting avenue.
        for(float x=-84;x<=84;x+=8)
        {
            if(Mathf.Abs(x)<=12f)continue; // the central avenue already covers this strip.
            PlaceTop(Floor,g,"Conector_transversal_inicio_"+x,new Vector3(x,.18f,-54),Vector3.one,Quaternion.identity);floorCount++;
        }
        // Back crosswalk reaches the recessed tunnel portal; its opening is deliberately left clear.
        for(float x=-84;x<=84;x+=8)
        {
            if(Mathf.Abs(x)<=12f||Mathf.Abs(x-24f)<=4.01f)continue;
            PlaceTop(Floor,g,"Conector_transversal_patío_posterior_"+x,new Vector3(x,.18f,38),Vector3.one,Quaternion.identity);floorCount++;
        }
        foreach(int side in new[]{-1,1})
        for(int i=0;i<13;i++)
        {
            float x=side*91,z=-56+i*8;
            PlaceTop(Floor,g,"Piso_galería_"+side+"_"+i,new Vector3(x,.18f,z),Vector3.one,Quaternion.identity);floorCount++;
            if(i%2==0)
            {
                var arch=(i==6||i==10)?"High_Detailed/Archs/HD_Arch_2A.prefab":SmallArch;
                PlaceFloor(arch,g,"Marco_abierto_"+side+"_"+i,new Vector3(x,.18f,z),Vector3.one,Quaternion.identity);archCount++;
            }
        }
    }
    static void BuildMaze()
    {
        var g=Group("Maze_oeste_fácil_y_abierto",architecture);
        const float wallCenterY=5.18f;
        foreach(float z in new[]{-3f,1.2f,5.4f,9.6f,13.8f,18f,22.2f})
        {
            PlaceCenter(MazeWall,g,"Maze_perímetro_oeste_"+z,new Vector3(-78,wallCenterY,z),Vector3.one,Quaternion.Euler(0,90,0));
            PlaceCenter(MazeWall,g,"Maze_perímetro_este_"+z,new Vector3(-45,wallCenterY,z),Vector3.one,Quaternion.Euler(0,90,0));
        }
        foreach(float x in new[]{-75f,-70.8f,-66.6f,-54f,-49.8f})
        {
            // The west gallery stair rises north from x=-55, z=23.93. The old
            // north-wall module centered at x=-54 blocked this stair at its foot.
            // Keep a clear, wide exit from the easy maze into the upper gallery.
            if(Mathf.Abs(x+54f)<.01f)continue;
            PlaceCenter(MazeWall,g,"Maze_perímetro_norte_"+x,new Vector3(x,wallCenterY,24),Vector3.one,Quaternion.identity);
        }
        PlaceCenter(MazeWall,g,"Maze_entrada_sur_oeste",new Vector3(-71,wallCenterY,-6),Vector3.one,Quaternion.identity);
        PlaceCenter(MazeWall,g,"Maze_entrada_sur_este",new Vector3(-52,wallCenterY,-6),Vector3.one,Quaternion.identity);
        PlaceCenter(MazeWall,g,"Maze_pasillo_interior_1",new Vector3(-69,wallCenterY,1),Vector3.one,Quaternion.identity);
        PlaceCenter(MazeWall,g,"Maze_pasillo_interior_2",new Vector3(-55,wallCenterY,14),Vector3.one,Quaternion.Euler(0,90,0));
        PlaceCenter(MazeWall,g,"Maze_pasillo_interior_3",new Vector3(-66,wallCenterY,20),Vector3.one,Quaternion.identity);
    }
    static void BuildTunnels()
    {
        var g=Group("Tunel_corto_hacia_patio_posterior",architecture);
        // The vault is reached from the east side of the raised courtyard by a supported open stair, then a graded ramp.
        BuildVaultApproach(g);
        // Match the first ramp exactly to the stair landing (z=41.54) and the vault floor edge (z=55).
        const float descentStart=41.54f,descentEnd=55f,descentRun=descentEnd-descentStart,descentCenter=(descentStart+descentEnd)*.5f;
        var down=PlaceCenter(TunnelRamp,g,"Rampa_túnel_descenso_desde_paseo",new Vector3(25,-1.45f,descentCenter),new Vector3(1f,1f,descentRun/14.28f),Quaternion.Euler(0,270,0));
        BuildWalkableRamp(down,"Superficie_caminable_descenso",new Vector3(25,-1.45f,descentCenter),4.3f,descentRun,.18f,-3.08f);
        // The exit ramp terminates at the posterior courtyard edge rather than 1 m inside its floor.
        const float exitStart=69f,exitEnd=82f,exitRun=exitEnd-exitStart,exitCenter=(exitStart+exitEnd)*.5f;
        var up=PlaceCenter(TunnelRamp,g,"Rampa_túnel_salida_al_patío_posterior",new Vector3(25,-1.45f,exitCenter),new Vector3(1f,1f,exitRun/14.28f),Quaternion.Euler(0,90,0));
        BuildWalkableRamp(up,"Superficie_caminable_salida",new Vector3(25,-1.45f,exitCenter),4.3f,exitRun,-3.08f,.18f);
        rampCount=2;
        var floorGroup=Group("Piso_del_tunel",g);
        BuildCircularVault(g,floorGroup);
        PlaceFloor(TunnelArchA,g,"Arco_portal_bóveda_entrada",new Vector3(25,-3.08f,55),new Vector3(1f,.5f,1f),Quaternion.identity);tunnelCount++;
        PlaceFloor(TunnelArchB,g,"Arco_portal_bóveda_salida",new Vector3(25,-3.08f,69),new Vector3(1f,.5f,1f),Quaternion.identity);tunnelCount++;
    }
    static void BuildCircularVault(Transform parent,Transform floorParent)
    {
        const float centerX=25f,centerZ=62f,floorY=-3.08f,radius=7.2f,springY=-.58f,domeRise=.58f,wallInner=6.72f,wallOuter=7.2f;
        var structure=Group("Colisión_estructural_bóveda_circular",parent);
        BuildVaultDisk(floorParent,centerX,centerZ,floorY,radius);
        BuildCircularVaultWalls(parent,centerX,centerZ,floorY,springY,wallInner,wallOuter);
        BuildDomeMesh(structure,centerX,centerZ,springY,radius,domeRise);
        // Eight radial piers carry the circular spring ring; the two tunnel portals remain on the north/south axis.
        var columns=Group("Columnas_portantes_de_bóveda",parent);
        const float columnRadius=5.35f,columnHeight=springY-floorY,columnScale=columnHeight/4.6f;
        for(int i=0;i<8;i++)
        {
            float a=(22.5f+i*45f)*Mathf.Deg2Rad,x=centerX+Mathf.Sin(a)*columnRadius,z=centerZ+Mathf.Cos(a)*columnRadius;
            PlaceFloor(PillarA,columns,"Columna_radial_bóveda_"+(i+1),new Vector3(x,floorY,z),new Vector3(1f,columnScale,1f),Quaternion.Euler(0,22.5f+i*45f,0));vaultColumnCount++;
        }
        var portals=Group("Portales_de_bóveda",parent);
        BuildCircularPortal(portals,"entrada",centerX,centerZ-radius,floorY,2.5f,TunnelArchA);
        BuildCircularPortal(portals,"salida",centerX,centerZ+radius,floorY,2.5f,TunnelArchB);
        var torches=Group("Antorchas_e_iluminación_realtime_bóveda",parent);
        foreach(float degrees in new[]{45f,135f,225f,315f})
        {
            float a=degrees*Mathf.Deg2Rad,x=centerX+Mathf.Sin(a)*6.45f,z=centerZ+Mathf.Cos(a)*6.45f;
            var torch=PlaceFloor(Torch,torches,"Antorcha_bóveda_arco_"+degrees,new Vector3(x,floorY,z),Vector3.one*.42f,Quaternion.Euler(0,degrees,0));vaultTorchCount++;
            var lightObject=new GameObject("Luz_cálida_bóveda");lightObject.transform.SetParent(torch.transform,false);lightObject.transform.localPosition=new Vector3(0f,1.05f,0f);
            var light=lightObject.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1f,.48f,.2f);light.intensity=2.4f;light.range=9f;light.shadows=LightShadows.None;light.renderMode=LightRenderMode.Auto;vaultRealtimeLightCount++;
        }
        floorCount++;
        audit.AppendLine("Bóveda circular | diámetro exterior="+(radius*2f).ToString("0.0")+" m | piso circular a Y="+floorY.ToString("0.00")+" | muro hasta Y="+springY.ToString("0.00")+" | cúpula hasta Y=0.00 | 2 portales axiales.");
    }
    static void BuildCircularVaultWalls(Transform parent,float cx,float cz,float floorY,float springY,float innerRadius,float outerRadius)
    {
        var walls=Group("Mampostería_circular_bóveda_PW",parent);
        const int segments=36;float segmentScale=2f*Mathf.PI*outerRadius/segments/2.2f;
        for(int i=0;i<segments;i++)
        {
            float degrees=(i+.5f)*360f/segments;
            float dNorth=Mathf.Abs(Mathf.DeltaAngle(degrees,0f)),dSouth=Mathf.Abs(Mathf.DeltaAngle(degrees,180f));
            if(dNorth<20f||dSouth<20f)continue;
            float a=degrees*Mathf.Deg2Rad,x=cx+Mathf.Sin(a)*(innerRadius+outerRadius)*.5f,z=cz+Mathf.Cos(a)*(innerRadius+outerRadius)*.5f;
            PlaceFloor(Wall,walls,"Muro_curvo_bóveda_modulo_"+i,new Vector3(x,floorY,z),new Vector3(segmentScale,(springY-floorY)/4f,(outerRadius-innerRadius)/1.2f),Quaternion.Euler(0,degrees,0));wallCount++;
        }
    }
    static void BuildCircularPortal(Transform parent,string name,float cx,float z,float floorY,float height,string prefab)
    {
        var arch=PlaceFloor(prefab,parent,"Arco_PW_portal_circular_"+name,new Vector3(cx,floorY,z),new Vector3(1f,height/5f,1f),Quaternion.identity);DisableAssetColliders(arch);tunnelCount++;
    }
    static void BuildVaultDisk(Transform parent,float cx,float cz,float topY,float radius)
    {
        const int segments=64;const float thickness=.30f;
        var go=new GameObject("Piso_circular_bóveda_PW");go.transform.SetParent(parent,false);go.transform.position=new Vector3(cx,topY,cz);go.isStatic=true;
        var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        vertices.Add(Vector3.zero);uv.Add(new Vector2(.5f,.5f));
        for(int i=0;i<segments;i++){float a=i*2f*Mathf.PI/segments;vertices.Add(new Vector3(Mathf.Sin(a)*radius,0,Mathf.Cos(a)*radius));uv.Add(new Vector2(.5f+Mathf.Sin(a)*radius/8f,.5f+Mathf.Cos(a)*radius/8f));}
        for(int i=0;i<segments;i++){int n=(i+1)%segments;triangles.Add(0);triangles.Add(i+1);triangles.Add(n+1);}
        int bottomCenter=vertices.Count;vertices.Add(new Vector3(0,-thickness,0));uv.Add(new Vector2(.5f,.5f));int bottomRing=vertices.Count;
        for(int i=0;i<segments;i++){float a=i*2f*Mathf.PI/segments;vertices.Add(new Vector3(Mathf.Sin(a)*radius,-thickness,Mathf.Cos(a)*radius));uv.Add(new Vector2(.5f+Mathf.Sin(a)*radius/8f,.5f+Mathf.Cos(a)*radius/8f));}
        for(int i=0;i<segments;i++){int n=(i+1)%segments;triangles.Add(bottomCenter);triangles.Add(bottomRing+n);triangles.Add(bottomRing+i);int a=i+1,b=n+1,c=bottomRing+i,d=bottomRing+n;triangles.Add(a);triangles.Add(c);triangles.Add(d);triangles.Add(a);triangles.Add(d);triangles.Add(b);}
        var mesh=new Mesh{name="Piso_circular_bóveda_mesh"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=PersistMesh(mesh,CircularVaultFloorMeshPath);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=PrefabMaterial(Floor);go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void BuildDomeMesh(Transform parent,float cx,float cz,float springY,float radius,float rise)
    {
        const int around=48,steps=10;var vertices=new System.Collections.Generic.List<Vector3>();var uv=new System.Collections.Generic.List<Vector2>();var triangles=new System.Collections.Generic.List<int>();
        vertices.Add(new Vector3(0,rise,0));uv.Add(new Vector2(.5f,.0f));
        for(int j=1;j<=steps;j++)
        {
            float phi=j*Mathf.PI/(2f*steps),r=radius*Mathf.Sin(phi),y=rise*Mathf.Cos(phi);
            for(int i=0;i<around;i++){float a=i*2f*Mathf.PI/around;vertices.Add(new Vector3(Mathf.Sin(a)*r,y,Mathf.Cos(a)*r));uv.Add(new Vector2(i/(float)around*(2f*Mathf.PI*radius/2.2f),j/(float)steps*(radius*Mathf.PI*.5f/4f)));}
        }
        for(int i=0;i<around;i++){int n=(i+1)%around;AddDoubleTriangle(triangles,0,1+n,1+i);}
        for(int j=0;j<steps-1;j++)for(int i=0;i<around;i++)
        {int n=(i+1)%around,a=1+j*around+i,b=1+j*around+n,c=1+(j+1)*around+n,d=1+(j+1)*around+i;AddDoubleTriangle(triangles,a,b,c);AddDoubleTriangle(triangles,a,c,d);}
        var mesh=new Mesh{name="Cúpula_circular_bóveda_mesh"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateBounds();mesh=PersistMesh(mesh,CircularVaultDomeMeshPath);
        var go=new GameObject("Cúpula_circular_bóveda_PW");go.transform.SetParent(parent,false);go.transform.position=new Vector3(cx,springY,cz);go.isStatic=true;
        go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=PrefabMaterial(Wall);go.AddComponent<MeshCollider>().sharedMesh=mesh;
    }
    static void AddDoubleTriangle(System.Collections.Generic.List<int> triangles,int a,int b,int c){triangles.Add(a);triangles.Add(b);triangles.Add(c);triangles.Add(c);triangles.Add(b);triangles.Add(a);}
    static Mesh PersistMesh(Mesh generated,string path)
    {
        var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if(existing){EditorUtility.CopySerialized(generated,existing);existing.name=generated.name;UnityEngine.Object.DestroyImmediate(generated);EditorUtility.SetDirty(existing);return existing;}
        AssetDatabase.CreateAsset(generated,path);return generated;
    }
    static Material PrefabMaterial(string relative)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot+relative);var renderer=prefab?prefab.GetComponentInChildren<Renderer>(true):null;return renderer?renderer.sharedMaterial:null;
    }
    static void BuildVaultApproach(Transform parent)
    {
        var g=Group("Descenso_abierto_hacia_la_bóveda",parent);
        // Two equal 2 m flights and a stone rest landing bridge the upper patio to the lower portal.
        const float run=5.47f,landingDepth=1.6f,startZ=29f;
        float midZ=startZ+run,landingEnd=midZ+landingDepth,endZ=startZ+run*2f+landingDepth;
        PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",g,"Descanso_superior_descenso_bóveda",new Vector3(25f,4.18f,28.2f),new Vector3(1f,1f,.2f),Quaternion.identity);floorCount++;
        DisableAssetColliders(PlaceCenter(TempleSteps,g,"Escalera_bóveda_tramo_alto",new Vector3(25,3.18f,startZ+run*.5f),new Vector3(1f,.5f,run/6.27f),Quaternion.identity));
        PlaceTop("High_Detailed/Platforms/HD_Platform_3A.prefab",g,"Descanso_intermedio_escalera_bóveda",new Vector3(25,2.18f,midZ+landingDepth*.5f),new Vector3(1f,1f,landingDepth/8f),Quaternion.identity);floorCount++;stairLandingCount++;
        DisableAssetColliders(PlaceCenter(TempleSteps,g,"Escalera_bóveda_tramo_bajo",new Vector3(25,1.18f,landingEnd+run*.5f),new Vector3(1f,.5f,run/6.27f),Quaternion.identity));
        AddStairRamp(g,"Superficie_bóveda_tramo_superior",25,startZ,4.18f,run,-2f,4f);
        AddStairRamp(g,"Superficie_bóveda_tramo_inferior",25,landingEnd,2.18f,run,-2f,4f);
        BuildStairSupport(g,"Apoyo_macizo_escalera_bóveda",25f,4f,new[]{new Vector2(startZ,4.18f),new Vector2(midZ,2.18f),new Vector2(landingEnd,2.18f),new Vector2(endZ,.18f)});
        var support=Group("Muros_laterales_de_contención_del_descenso",g);
        foreach(int side in new[]{-1,1})for(float z=29f;z<=40f;z+=2.2f)
        {PlaceFloor(Wall,support,"Muro_guía_bóveda_"+side+"_"+z,new Vector3(25+side*3f,.18f,z),Vector3.one,Quaternion.Euler(0,90,0));wallCount++;}
    }
    static void BuildWalkableRamp(GameObject visual,string name,Vector3 center,float width,float run,float startY,float endY)
    {
        foreach(var c in visual.GetComponentsInChildren<Collider>(true))c.enabled=false;
        var walk=new GameObject(name);walk.transform.position=center;
        walk.transform.rotation=Quaternion.Euler(-Mathf.Atan2(endY-startY,run)*Mathf.Rad2Deg,0,0);
        walk.AddComponent<BoxCollider>().size=new Vector3(width,.08f,Mathf.Sqrt(run*run+(endY-startY)*(endY-startY)));
        walk.transform.SetParent(visual.transform,true);
    }
    static void BuildTemple()
    {
        var g=Group("Templo_final_y_mirador",architecture);
        PlaceFloor(Temple,g,"Portal_templo_final",new Vector3(0,12.18f,65),Vector3.one,Quaternion.identity);
        PlaceCenter(Arch,g,"Arco_mirador",new Vector3(0,18f,78),new Vector3(1.4f,1,1),Quaternion.identity);archCount++;
        foreach(int side in new[]{-1,1})
        {
            PlaceFloor(Torch,g,"Antorcha_patio_final_"+side,new Vector3(side*20,12.18f,56),Vector3.one,Quaternion.identity);
            PlaceFloor(Bowl,g,"Cuenco_patio_final_"+side,new Vector3(side*24,12.18f,52),Vector3.one,Quaternion.identity);
        }
    }
    static void BuildTempleUndercroftSupports()
    {
        var grid=Group("Pilares_de_apoyo_bajo_terraza_del_templo",architecture);
        const float segmentHeight=3.96f,scaleY=segmentHeight/4.60f;
        // Support the broad upper temple deck on a legible 8–12 m grid, keeping the stairwell and central portal clear.
        foreach(float z in new[]{54f,66f,78f})foreach(float x in (z==54f?new[]{-24f,-16f,16f,20f}:new[]{-20f,-8f,8f,20f}))
        {
            var pier=Group("Pila_portante_"+x+"_"+z,grid);
            for(int tier=0;tier<3;tier++)
                PlaceFloor(PillarA,pier,"Columna_basamento_templo_"+x+"_"+z+"_cuerpo_"+(tier+1),new Vector3(x,tier*segmentHeight,z),new Vector3(1f,scaleY,1f),Quaternion.identity);
            templeUndercroftPierCount++;
        }
    }
    static void BuildDetails()
    {
        var g=Group("Detalles_puntuales_sin_invader_rutas",architecture);
        foreach(int side in new[]{-1,1})
        {
            PlaceFloor(Torch,g,"Antorcha_inicio_"+side,new Vector3(side*17,.18f,-70),Vector3.one,Quaternion.identity);
            PlaceFloor(Bowl,g,"Cuenco_inicio_"+side,new Vector3(side*21,.18f,-74),Vector3.one,Quaternion.identity);
        }
    }
    static void BuildStructuralAccents(Terrain terrain)
    {
        var g=Group("Acentos_arquitectónicos_PW_para_terrazas_y_bóveda",architecture);
        var piers=Group("Pilares_bajo_plataformas_elevadas",architecture);
        // Turn the six patio columns into continuous piers. Each decorative shaft sits on a visible plinth,
        // and a full masonry pier continues below the deck to the terrain so it cannot read as a floating post.
        foreach(int side in new[]{-1,1})
        {
            BuildRaisedTerracePier(terrain,piers,"Pila_patío_central_baja_"+side,side*24f,-3f,3.88f,PillarA);
            BuildRaisedTerracePier(terrain,piers,"Pila_patío_central_media_"+side,side*24f,12f,3.88f,PillarA);
            BuildRaisedTerracePier(terrain,piers,"Pila_patío_central_exterior_"+side,side*30f,23f,3.88f,PillarA);
            BuildColumnPlinth(g,"Plinto_pilar_patío_bajo_"+side,side*24f,-3f,3.88f);
            BuildColumnPlinth(g,"Plinto_pilar_patío_medio_"+side,side*24f,12f,3.88f);
            BuildColumnPlinth(g,"Plinto_pilar_patío_exterior_"+side,side*30f,23f,3.88f);
        }
        // Inner gallery and high-court columns support the large slabs; their positions sit outside the central route.
        foreach(int side in new[]{-1,1})
        {
            BuildRaisedTerracePier(terrain,piers,"Pila_galería_superior_"+side,side*68f,42f,7.88f,PillarA);
            BuildRaisedTerracePier(terrain,piers,"Pila_patio_alto_"+side,side*60f,62f,7.88f,PillarA);
        }
        // Paired masonry supports enrich the central court while remaining outside the 32 m clear processional lane.
        foreach(int side in new[]{-1,1})
        {
            PlaceFloor(PillarA,g,"Pilar_patío_bajo_"+side,new Vector3(side*24,4.18f,-3),Vector3.one,Quaternion.identity);accentCount++;
            PlaceFloor(PillarB,g,"Pilar_patío_medio_"+side,new Vector3(side*24,4.18f,12),Vector3.one,Quaternion.identity);accentCount++;
            PlaceFloor(PillarC,g,"Pilar_patío_exterior_"+side,new Vector3(side*30,4.18f,23),Vector3.one,Quaternion.identity);accentCount++;
        }
        foreach(int side in new[]{-1,1})
        {
            PlaceFloor(Railing1,g,"Baranda_templo_lateral_A_"+side,new Vector3(side*26,12.18f,58),Vector3.one,Quaternion.Euler(0,90,0));accentCount++;
            PlaceFloor(Railing2,g,"Baranda_templo_lateral_B_"+side,new Vector3(side*26,12.18f,66),Vector3.one,Quaternion.Euler(0,90,0));accentCount++;
            PlaceFloor(Railing3,g,"Baranda_templo_lateral_C_"+side,new Vector3(side*26,12.18f,74),Vector3.one,Quaternion.Euler(0,90,0));accentCount++;
            PlaceFloor(RailingPost1,g,"Remate_columna_galería_1_"+side,new Vector3(side*18,8.18f,42),Vector3.one,Quaternion.identity);accentCount++;
            PlaceFloor(RailingPost2,g,"Remate_columna_galería_2_"+side,new Vector3(side*18,8.18f,46),Vector3.one,Quaternion.identity);accentCount++;
            PlaceFloor(Railing4,g,"Baranda_frontal_templo_"+side,new Vector3(side*20,12.18f,54),Vector3.one,Quaternion.identity);accentCount++;
        }
        PlaceFloor(FullRailing,g,"Baranda_mirador_trasero",new Vector3(0,12.18f,80),Vector3.one,Quaternion.identity);accentCount++;
    }
    static void BuildRaisedTerracePier(Terrain terrain,Transform parent,string name,float x,float z,float undersideY,string prefab)
    {
        float groundY=terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y;
        float height=undersideY-groundY;
        if(height<1f)throw new InvalidOperationException(name+" no tiene altura válida para soporte: "+height.ToString("0.00"));
        var group=Group(name,parent);const float moduleNominalHeight=4.6f;const float maxModuleHeight=3.96f;
        float cursor=groundY;int module=0;
        while(cursor<undersideY-.001f)
        {
            float section=Mathf.Min(maxModuleHeight,undersideY-cursor);
            var pillar=PlaceFloor(prefab,group,name+"_cuerpo_"+(++module),new Vector3(x,cursor,z),new Vector3(1f,section/moduleNominalHeight,1f),Quaternion.identity);
            var bounds=BoundsOf(pillar);
            if(Mathf.Abs(bounds.min.y-cursor)>.08f||Mathf.Abs(bounds.max.y-(cursor+section))>.12f)
                throw new InvalidOperationException(name+" perdió contacto en módulo "+module+": Y="+bounds.min.y.ToString("0.00")+"–"+bounds.max.y.ToString("0.00"));
            cursor+=section;
        }
        raisedTerracePierCount++;audit.AppendLine(name+" | terreno Y="+groundY.ToString("0.00")+" | intradós Y="+undersideY.ToString("0.00")+" | módulos="+module+" | continuidad vertical comprobada");
    }
    static void BuildColumnPlinth(Transform parent,string name,float x,float z,float floorTop)
    {
        // A 0.42 m P&W stone course makes the transition from the continuous pier to the visible shaft legible.
        const float plinthHeight=.42f;
        PlaceFloor(PillarA,parent,name,new Vector3(x,floorTop,z),new Vector3(1f,.42f/4.6f,1f),Quaternion.identity);
        audit.AppendLine(name+" | X="+x.ToString("0.00")+" Z="+z.ToString("0.00")+" | losa Y="+floorTop.ToString("0.00")+" | plinto hasta Y="+(floorTop+plinthHeight).ToString("0.00"));
    }
    static void BuildVegetation(Terrain terrain)
    {
        var g=Group("09_Vegetacion_ZNS3D_en_laderas_fuera_de_rutas",root);
        var trees=new[]{Oak,Pine};var grasses=new[]{Grass1,Grass2,Grass3,Grass4};var rocks=new[]{Rock1,Rock2,Rock3};
        int index=0;
        // Two staggered rows follow each slope base; 14 m longitudinal and 12 m transverse spacing closes the old gaps.
        for(int row=0;row<2;row++)for(int i=0;i<12;i++)foreach(float side in new[]{-1f,1f})
        {
            float x=side*(104f+row*12f),z=-78f+i*14f+row*7f;var pos=TerrainPoint(terrain,x,z);
            PlaceNature(trees[(index/2)%trees.Length],g,"Arbol_ladera_"+index,pos,index%4==0?1.35f:1.1f,Quaternion.Euler(0,(index*53)%360,0));treeCount++;index++;
        }
        foreach(var pos2 in new[]{new Vector2(-108,-96),new Vector2(-76,-101),new Vector2(-42,-105),new Vector2(42,-105),new Vector2(76,-101),new Vector2(108,-96),new Vector2(-96,91),new Vector2(-68,103),new Vector2(-34,112),new Vector2(0,116),new Vector2(34,112),new Vector2(68,103),new Vector2(96,91)})
        {var pos=TerrainPoint(terrain,pos2.x,pos2.y);PlaceNature(trees[index%2],g,"Arbol_fondo_"+index,pos,1.1f,Quaternion.Euler(0,(index*41)%360,0));treeCount++;index++;}
        var rng=new System.Random(40817);
        for(int i=0;i<180;i++)
        {
            float side=(i%2==0)?-1f:1f;
            float x=side*(97f+(float)rng.NextDouble()*48f);
            float z=-92f+(float)rng.NextDouble()*194f;
            var pos=TerrainPoint(terrain,x,z);
            PlaceNature(grasses[i%grasses.Length],g,"Maleza_ladera_"+i,pos,.7f+(float)rng.NextDouble()*.55f,Quaternion.Euler(0,(float)rng.NextDouble()*360,0));grassCount++;
        }
        for(int i=0;i<28;i++)
        {
            float side=(i%2==0)?-1f:1f;float x=side*(116f+(i%4)*5f);float z=-92f+(i*13%185);
            var pos=TerrainPoint(terrain,x,z);PlaceNature(rocks[i%rocks.Length],g,"Roca_ladera_"+i,pos,.8f+(i%3)*.25f,Quaternion.Euler(0,(i*37)%360,0));rockCount++;
        }
    }
    static Vector3 TerrainPoint(Terrain terrain,float x,float z)
    {return new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y,z);}
    static void PlaceNature(string path,Transform parent,string name,Vector3 ground,float scale,Quaternion rot)
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);var go=PrefabUtility.InstantiatePrefab(asset,parent) as GameObject;
        if(!go)throw new InvalidOperationException("Cannot instantiate vegetation "+path);
        var authored=go.transform.localScale;go.name=name;go.transform.localScale=authored*scale;go.transform.rotation=rot;
        var rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);go.transform.position+=new Vector3(ground.x-b.center.x,ground.y-b.min.y,ground.z-b.center.z);}else go.transform.position=ground;
        ConvertFoliageMaterials(go);
        SetStatic(go.transform);
    }
    static void ConvertFoliageMaterials(GameObject instance)
    {
        var urp=Shader.Find("Universal Render Pipeline/Lit");
        if(!urp)throw new InvalidOperationException("No se encontró el shader Universal Render Pipeline/Lit para corregir el magenta de ZNS3D.");
        var folder="Assets/TemploNavMesh/Materials/VegetationURP";Directory.CreateDirectory(folder);
        foreach(var renderer in instance.GetComponentsInChildren<Renderer>(true))
        {
            var sources=renderer.sharedMaterials;
            for(int i=0;i<sources.Length;i++)
            {
                var source=sources[i];if(!source)continue;
                if(!foliageMaterialCache.TryGetValue(source,out var converted))
                {
                    var srcPath=AssetDatabase.GetAssetPath(source);var baseName=Path.GetFileName(Path.GetDirectoryName(srcPath))+"_"+source.name+"_URP.mat";
                    var dest=folder+"/"+baseName;converted=AssetDatabase.LoadAssetAtPath<Material>(dest);
                    if(!converted)
                    {
                        var tex=source.HasProperty("_BaseColorMap")?source.GetTexture("_BaseColorMap"):null;if(!tex)tex=source.mainTexture;
                        converted=new Material(urp){name=Path.GetFileNameWithoutExtension(baseName),enableInstancing=true};
                        converted.SetColor("_BaseColor",source.HasProperty("_BaseColor")?source.GetColor("_BaseColor"):Color.white);
                        if(tex)converted.SetTexture("_BaseMap",tex);
                        bool cutout=source.renderQueue>=2450||source.IsKeywordEnabled("_ALPHATEST_ON");
                        converted.SetFloat("_AlphaClip",cutout?1f:0f);converted.SetFloat("_Cutoff",source.HasProperty("_Cutoff")?source.GetFloat("_Cutoff"):0.35f);
                        converted.SetFloat("_Cull",cutout?0f:2f);converted.SetFloat("_Smoothness",0.18f);
                        if(cutout){converted.EnableKeyword("_ALPHATEST_ON");converted.SetOverrideTag("RenderType","TransparentCutout");converted.renderQueue=2450;}
                        AssetDatabase.CreateAsset(converted,dest);
                    }
                    foliageMaterialCache[source]=converted;
                }
                sources[i]=converted;
            }
            renderer.sharedMaterials=sources;
        }
    }
    static GameObject PlaceTop(string rel,Transform p,string n,Vector3 top,Vector3 scale,Quaternion rot)
    {
        var go=Place(rel,p,n,scale,rot,b=>new Vector3(top.x-b.center.x,top.y-b.max.y,top.z-b.center.z));
        // Package colliders sit 5–10 cm below the visible deck. Add an exact, thin walking slab at the authored top datum.
        var bounds=BoundsOf(go);var support=new GameObject(n+"_Piso_transitable_a_cota");support.transform.SetParent(p,false);
        support.transform.SetPositionAndRotation(new Vector3(top.x,top.y-.04f,top.z),rot);
        support.AddComponent<BoxCollider>().size=new Vector3(bounds.size.x,.08f,bounds.size.z);support.isStatic=true;
        return go;
    }
    static GameObject PlaceFloor(string rel,Transform p,string n,Vector3 floor,Vector3 scale,Quaternion rot)
    {
        return Place(rel,p,n,scale,rot,b=>new Vector3(floor.x-b.center.x,floor.y-b.min.y,floor.z-b.center.z));
    }
    static GameObject PlaceCenter(string rel,Transform p,string n,Vector3 center,Vector3 scale,Quaternion rot)
    {
        return Place(rel,p,n,scale,rot,b=>center-b.center);
    }
    static GameObject Place(string rel,Transform p,string n,Vector3 scale,Quaternion rot,Func<Bounds,Vector3> offset)
    {
        var path=PrefabRoot+rel;var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go=PrefabUtility.InstantiatePrefab(asset,p) as GameObject;if(!go)throw new InvalidOperationException("Cannot instantiate "+path);
        go.name=n;
        // P&W often bakes meaningful scale into the prefab root; apply only a multiplier.
        var authoredScale=go.transform.localScale;
        go.transform.localScale=Vector3.Scale(authoredScale,scale);
        go.transform.rotation=rot;
        var b=BoundsOf(go);go.transform.position+=offset(b);go.isStatic=true;
        audit.AppendLine(n+" | "+rel+" | centro colocado "+go.transform.position.ToString("F2")+" | bounds "+b.size.ToString("F2"));
        SetStatic(go.transform);return go;
    }
    static Bounds BoundsOf(GameObject go)
    {
        var rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)throw new InvalidOperationException(go.name+" sin Renderer.");
        var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);return b;
    }
    static Transform Group(string n,Transform p){var t=new GameObject(n).transform;t.SetParent(p,false);return t;}
    static float Smooth01(float t){t=Mathf.Clamp01(t);return t*t*(3f-2f*t);}
    static void SetStatic(Transform t){t.gameObject.isStatic=true;foreach(Transform c in t)SetStatic(c);}
    static void ConfigureCamera()
    {
        Camera c=null;Light sun=null;
        foreach(var go in SceneManager.GetActiveScene().GetRootGameObjects()){if(!c)c=go.GetComponentInChildren<Camera>();if(!sun)sun=go.GetComponentInChildren<Light>();}
        if(c)
        {
            // Frame the entire broad layout from above in the Game view so visual QA
            // can inspect the full composition without orbiting Scene view manually.
            c.transform.position=new Vector3(0,112,-245);
            c.transform.rotation=Quaternion.LookRotation(new Vector3(0,5,8)-c.transform.position);
            c.fieldOfView=64;c.farClipPlane=700;
        }
        if(sun){sun.transform.rotation=Quaternion.Euler(38,-32,0);sun.intensity=1.25f;sun.shadows=LightShadows.Soft;}
    }
    static void AddReviewPlayer()
    {
        var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Adventures_woman/Prefabs/Adventures_woman_01.prefab");
        if(!asset){audit.AppendLine("Jugador de revisión no añadido: prefab de Aventurera ausente.");return;}
        var player=UnityEngine.Object.Instantiate(asset);if(!player){audit.AppendLine("Jugador de revisión no añadido: no se pudo instanciar.");return;}
        player.name="Aventurera_Review_PlayMode";player.tag="Player";player.transform.position=new Vector3(0,.5f,-70f);player.transform.rotation=Quaternion.identity;
        var cc=player.GetComponent<CharacterController>();if(!cc)cc=player.AddComponent<CharacterController>();cc.height=2.04f;cc.radius=.34f;cc.center=new Vector3(0,1.02f,0);cc.slopeLimit=45f;cc.stepOffset=.25f;
        var movement=player.GetComponent<AventureraPlayerController>();if(!movement)movement=player.AddComponent<AventureraPlayerController>();movement.SetController(cc);
        var camera=Camera.main;
        if(camera){var rig=camera.GetComponent<ThirdPersonAdventureCamera>();if(!rig)rig=camera.gameObject.AddComponent<ThirdPersonAdventureCamera>();rig.Target=player.transform;movement.SetCamera(camera.transform);}
        var routeProbe=new GameObject("QA_Recorrido_Fisico");routeProbe.AddComponent<AventureraRouteAudit>();routeProbe.transform.SetPositionAndRotation(player.transform.position,player.transform.rotation);
        var probeLink=routeProbe.AddComponent<CitadelReviewRouteProbe>();probeLink.Initialize(player.transform);routeProbe.SetActive(true);
        audit.AppendLine("Jugador de revisión Play Mode: Aventurera, CharacterController, ThirdPersonAdventureCamera y auditoría física con NavMesh opcional; posición (0,0.5,-70).");
    }
}
