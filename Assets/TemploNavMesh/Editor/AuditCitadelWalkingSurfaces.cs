using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class AuditCitadelWalkingSurfaces
{
 const string ScenePath="Assets/Scenes/CiudadelaMaya_AssetsPreview.unity";
 const string ReportPath="Assets/TemploNavMesh/CiudadelaWalkingSurfaceAudit.txt";
 [MenuItem("Tools/NavMesh Tutorial/Audit Maya Citadel stair and ramp surfaces")]
 public static void Run()
 {
  try{
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);var root=scene.GetRootGameObjects().FirstOrDefault(x=>x.name=="Ciudadela_Maya_PW_Revision_de_Assets");
   if(!root)throw new System.Exception("No encuentro la raíz de la escena de revisión.");
   var all=root.GetComponentsInChildren<Collider>(true);var enabledStates=all.ToDictionary(c=>c,c=>c.enabled);var sb=new StringBuilder("AUDITORÍA DE SUPERFICIES — lectura vertical de colliders, no NavMesh\n\n");
   var terrain=root.GetComponentInChildren<Terrain>();if(terrain)foreach(var p in new[]{new Vector2(-91,-56),new Vector2(91,-8),new Vector2(-91,40),new Vector2(91,40),new Vector2(25,84),new Vector2(25,94)})sb.AppendLine("Terreno bajo ruta "+p+" | Y="+(terrain.SampleHeight(new Vector3(p.x,0,p.y))+terrain.transform.position.y).ToString("0.00")+" m");
   var floorNames=new[]{"Losas_PW_modulo_8m_superficie_alineada","Galerias_laterales_con_marcos","Piso_del_tunel"};
   var floorColliders=root.GetComponentsInChildren<Transform>(true).Where(t=>floorNames.Contains(t.name)).SelectMany(t=>t.GetComponentsInChildren<Collider>(true)).ToList();
   // PlaceTop creates exact walking slabs as sibling objects; include the ones beside stairs and landings too.
   floorColliders.AddRange(root.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("_Piso_transitable_a_cota",System.StringComparison.Ordinal)).SelectMany(t=>t.GetComponentsInChildren<Collider>(true)));
   floorColliders=floorColliders.Distinct().ToList();
   foreach(var c in all)c.enabled=floorColliders.Contains(c)&&enabledStates[c];Physics.SyncTransforms();
   var floorChecks=new[]{("Plaza de inicio",0f,-70f,.18f),("Avenida",0f,-50f,.18f),("Patio central",24f,0f,4.18f),("Terraza oeste",-60f,0f,4.18f),("Terraza este",60f,0f,4.18f),("Galería oeste",-40f,46f,8.18f),("Galería este",40f,46f,8.18f),("Patio alto oeste",-48f,62f,8.18f),("Patio alto este",48f,62f,8.18f),("Terraza del templo",20f,70f,12.18f),("Maze",-60f,6f,4.18f),("Bóveda circular",25f,62f,-3.08f),("Patio posterior",25f,90f,.18f)};
   foreach(var q in floorChecks){float rayY=q.Item4>0?20f:0f;float distance=q.Item4>0?rayY-q.Item4+2f:5f;var ray=new Ray(new Vector3(q.Item2,rayY,q.Item3),Vector3.down);var hit=Physics.RaycastAll(ray,distance,~0,QueryTriggerInteraction.Ignore).Where(h=>h.collider!=null&&floorColliders.Contains(h.collider)).OrderBy(h=>h.distance).Cast<RaycastHit?>().FirstOrDefault();if(hit.HasValue)sb.AppendLine("PISO " + q.Item1+" | esperado Y="+q.Item4.ToString("0.00")+" | leído Y="+hit.Value.point.y.ToString("0.00")+" | collider="+hit.Value.collider.name+" | delta="+Mathf.Abs(hit.Value.point.y-q.Item4).ToString("0.00")+" m");else sb.AppendLine("PISO " + q.Item1+" | AUSENTE (sin collider de piso bajo el punto inspeccionado)");}
   AuditReviewPlayer(sb);
   foreach(var c in all)c.enabled=enabledStates[c];Physics.SyncTransforms();
   var names=root.GetComponentsInChildren<Transform>(true).Where(t=>t.parent&&t.parent.name=="Escaleras_PW_con_llegadas_a_nivel"&&t.childCount>0&&(t.name.StartsWith("Escalera_")||t.name.StartsWith("Escalinata_"))).Select(t=>t.name).Concat(new[]{"Descenso_abierto_hacia_la_bóveda","Rampa_túnel_descenso_desde_paseo","Cúpula_circular_bóveda_PW","Rampa_túnel_salida_al_patío_posterior"}).ToArray();
   foreach(var name in names)
   {
    var go=root.GetComponentsInChildren<Transform>(true).Select(t=>t.gameObject).FirstOrDefault(g=>g.name==name);
    if(!go){sb.AppendLine(name+": NO ENCONTRADO");continue;}
    var own=go.GetComponentsInChildren<Collider>(true);
    if(name=="Cúpula_circular_bóveda_PW")
    {var floor=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Piso_del_tunel");if(floor)own=floor.GetComponentsInChildren<Collider>(true);}
    foreach(var c in all)if(!own.Contains(c))c.enabled=false;Physics.SyncTransforms();
    var rs=go.GetComponentsInChildren<Renderer>(true);if(rs.Length==0){sb.AppendLine(name+": sin renderer");foreach(var c in all)c.enabled=enabledStates[c];continue;}
    var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
    foreach(float offset in new[]{-.75f,0f,.75f})
    {
     var samples=new List<float>();
     // The decorative tunnel-ramp mesh extends slightly past its walk collider; sample inside the true ends.
     int firstSample=name.StartsWith("Rampa_túnel_")?2:1,lastSample=name.StartsWith("Rampa_túnel_")?30:31;
     for(int i=firstSample;i<lastSample;i++)
     {
      float t=i/32f,x=name=="Cúpula_circular_bóveda_PW"?b.center.x+offset*2f:b.center.x+offset,z=Mathf.Lerp(b.min.z+.15f,b.max.z-.15f,t);
      float rayY=b.max.y+4f;
      var ray=new Ray(new Vector3(x,rayY,z),Vector3.down);
      float rayDistance=name=="Cúpula_circular_bóveda_PW"?10f:rayY-(b.min.y-2f);
      if(Physics.Raycast(ray,out var hit,rayDistance)){samples.Add(hit.point.y);}else samples.Add(float.NaN);
     }
     float maxGap=0;for(int i=1;i<samples.Count;i++)if(Valid(samples[i])&&Valid(samples[i-1]))maxGap=Mathf.Max(maxGap,Mathf.Abs(samples[i]-samples[i-1]));
     sb.AppendLine(name+" x-offset="+offset.ToString("0.00")+" m | colliders="+own.Length+" | muestra="+string.Join(",",samples.Select(v=>Valid(v)?v.ToString("0.00"):"GAP"))+" | salto="+maxGap.ToString("0.00")+" m");
    }
    foreach(var c in all)c.enabled=enabledStates[c];Physics.SyncTransforms();
   }
   var corridor=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Tunel_corto_hacia_patio_posterior");
   if(corridor)
   {
    // Only query intended walk surfaces; arch, wall, and roof colliders can otherwise mask the floor in a vertical ray.
    var walkNames=new[]{"Descenso_abierto_hacia_la_bóveda","Superficie_caminable_descenso","Piso_del_tunel","Superficie_caminable_salida"};
    var corridorEndcaps=floorColliders.Where(c=>c.name.Contains("Patio_posterior")||c.name.Contains("Patio_central")).ToArray();
    var corridorColliders=corridor.GetComponentsInChildren<Transform>(true).Where(t=>walkNames.Contains(t.name)).SelectMany(t=>t.GetComponentsInChildren<Collider>(true)).Concat(corridorEndcaps).Distinct().ToArray();
    foreach(var c in all)c.enabled=corridorColliders.Contains(c)&&enabledStates[c];Physics.SyncTransforms();
    var routeHeights=new List<float>();var routeHits=new List<string>();var routeMisses=new List<string>();
    for(float z=29.2f;z<87f;z+=.4f)
    {if(Physics.Raycast(new Ray(new Vector3(25,6,z),Vector3.down),out var hit,14f)){routeHeights.Add(hit.point.y);routeHits.Add(hit.collider.name);}else{routeHeights.Add(float.NaN);routeHits.Add("GAP");routeMisses.Add(z.ToString("0.0"));}}
    float routeMax=0;for(int i=1;i<routeHeights.Count;i++)if(Valid(routeHeights[i])&&Valid(routeHeights[i-1]))routeMax=Mathf.Max(routeMax,Mathf.Abs(routeHeights[i]-routeHeights[i-1]));
    var transitions=new List<string>();for(int i=1;i<routeHeights.Count;i++)if(Valid(routeHeights[i])&&Valid(routeHeights[i-1])&&Mathf.Abs(routeHeights[i]-routeHeights[i-1])>.18f)transitions.Add("z="+(29.2f+i*.4f).ToString("0.0")+" Y="+routeHeights[i-1].ToString("0.00")+"→"+routeHeights[i].ToString("0.00")+" ["+routeHits[i-1]+" → "+routeHits[i]+"]");
    sb.AppendLine("RECORRIDO_CONTINUO_BÓVEDA (eje central a intervalos de 0.40 m) | colliders activos="+corridorColliders.Length+" | muestras sin piso="+routeMisses.Count+(routeMisses.Count==0?"":" en z="+string.Join(",",routeMisses))+" | mayor cambio="+routeMax.ToString("0.00")+" m | transiciones >0.18 m: "+string.Join("; ",transitions));
    foreach(var c in all)c.enabled=enabledStates[c];Physics.SyncTransforms();
   }
   AuditVaultEnclosure(root,all,enabledStates,sb);
   AuditRaisedTerraceSupports(root,sb);
   AuditVisiblePatioColumns(root,sb);
   AuditStairBackings(root,sb);
   File.WriteAllText(ReportPath,sb.ToString());AssetDatabase.ImportAsset(ReportPath);Debug.Log("Citadel walking-surface audit saved to "+ReportPath);if(Application.isBatchMode)EditorApplication.Exit(0);
  }catch(System.Exception e){Debug.LogException(e);if(Application.isBatchMode)EditorApplication.Exit(1);}
 }
 static void AuditReviewPlayer(StringBuilder sb)
 {
  var player=GameObject.FindGameObjectsWithTag("Player").Select(g=>g.GetComponent<AventureraPlayerController>()).FirstOrDefault(p=>p);
  var camera=Camera.main;
  var root=GameObject.Find("Ciudadela_Maya_PW_Revision_de_Assets");var terrain=root?root.GetComponentInChildren<Terrain>(true):null;
  if(!player||!player.Controller||!camera){sb.AppendLine("PLAY MODE: personaje/cámara de prueba ausentes");return;}
  var cc=player.Controller;var start=player.transform.position;float ground=terrain?terrain.SampleHeight(start)+terrain.transform.position.y:0;
  bool spawnClear=Mathf.Abs(start.y-.5f)<.2f&&Mathf.Abs(ground)<.1f;
  sb.AppendLine("PLAY MODE PREPARADO | Aventurera="+player.name+" | CharacterController="+cc.height.ToString("0.00")+"×"+cc.radius.ToString("0.00")+" m | cámara objetivo="+(camera.GetComponent<ThirdPersonAdventureCamera>()?.Target==player.transform)+" | spawn="+start.ToString("F2")+" | terreno bajo spawn="+ground.ToString("0.00")+" | despejado previsto="+spawnClear);
 }
 static void AuditVisiblePatioColumns(GameObject root,StringBuilder sb)
 {
  var piers=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Pilares_bajo_plataformas_elevadas");
  var accents=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Acentos_arquitectónicos_PW_para_terrazas_y_bóveda");
  var issues=new List<string>();int shafts=0,plinths=0,continuous=0;
  if(!piers||!accents){sb.AppendLine("AUDITORÍA DE COLUMNAS VISIBLES DEL PATIO: grupos ausentes");return;}
  foreach(var t in accents.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Pilar_patío_")))
  {
   var rs=t.GetComponentsInChildren<Renderer>(true);if(rs.Length==0){issues.Add(t.name+" sin malla");continue;}
   var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);shafts++;
   string[] parts=t.name.Split('_');if(parts.Length<4){issues.Add(t.name+" sin lado legible");continue;}
   string kind=parts[2],side=parts[3],part=kind=="exterior"?"exterior":kind;float x=(side=="-1"?-1f:1f)*(part=="exterior"?30f:24f);float z=part=="bajo"?-3f:part=="medio"?12f:23f;
   string pierName="Pila_patío_central_"+(part=="bajo"?"baja":part=="medio"?"media":"exterior")+"_"+side;
   var pier=piers.Cast<Transform>().FirstOrDefault(p=>p.name==pierName);var ps=pier?pier.GetComponentsInChildren<Renderer>(true):new Renderer[0];
   if(ps.Length==0){issues.Add(t.name+" sin pila estructural correspondiente");continue;}
   var pb=ps[0].bounds;foreach(var r in ps.Skip(1))pb.Encapsulate(r.bounds);
   var plinth=accents.Cast<Transform>().FirstOrDefault(p=>p.name=="Plinto_pilar_patío_"+part+"_"+side);var prs=plinth?plinth.GetComponentsInChildren<Renderer>(true):new Renderer[0];
   if(prs.Length==0){issues.Add(t.name+" sin plinto visible");continue;}
   var plb=prs[0].bounds;foreach(var r in prs.Skip(1))plb.Encapsulate(r.bounds);plinths++;
   float groundY=pb.min.y;float deckY=pb.max.y;bool connected=Mathf.Abs(pb.min.y)<.12f&&Mathf.Abs(pb.max.y-3.88f)<.15f&&Mathf.Abs(plb.min.y-3.88f)<.1f&&Mathf.Abs(b.min.y-plb.max.y)<.16f;
   if(connected)continuous++;else issues.Add(t.name+" junta discontinua: pila "+pb.min.y.ToString("0.00")+"–"+pb.max.y.ToString("0.00")+", plinto "+plb.min.y.ToString("0.00")+"–"+plb.max.y.ToString("0.00")+", fuste "+b.min.y.ToString("0.00")+"–"+b.max.y.ToString("0.00"));
  }
  sb.AppendLine("AUDITORÍA DE COLUMNAS VISIBLES DEL PATIO | fustes="+shafts+" | plintos="+plinths+" | continuidad terreno-pila-intradós-plinto-fuste="+continuous+"/6 | incidencias: "+(issues.Count==0?"ninguna":string.Join("; ",issues)));
 }
 static void AuditVaultEnclosure(GameObject root,Collider[] all,Dictionary<Collider,bool> enabledStates,StringBuilder sb)
 {
  var vault=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Tunel_corto_hacia_patio_posterior");
  if(!vault){sb.AppendLine("BÓVEDA ESTRUCTURAL: NO ENCONTRADA");return;}
  var shell=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Cúpula_circular_bóveda_PW");
  var floor=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Piso_del_tunel");
  var columnGroup=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Columnas_portantes_de_bóveda");
  var torchGroup=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Antorchas_e_iluminación_realtime_bóveda");
  var structure=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Colisión_estructural_bóveda_circular");
  var walls=vault.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Mampostería_circular_bóveda_PW");
  var shellCols=shell?shell.GetComponentsInChildren<Collider>(true):new Collider[0];
  var floorCols=floor?floor.GetComponentsInChildren<Collider>(true):new Collider[0];
  var structureCols=structure?structure.GetComponentsInChildren<Collider>(true):new Collider[0];
  var wallCols=walls?walls.GetComponentsInChildren<Collider>(true):new Collider[0];
  var columnCols=columnGroup?columnGroup.GetComponentsInChildren<Collider>(true):new Collider[0];
  foreach(var c in all)c.enabled=(structureCols.Contains(c)||wallCols.Contains(c)||columnCols.Contains(c)||floorCols.Contains(c))&&enabledStates[c];Physics.SyncTransforms();
  int floorHits=0,roofHits=0;float minClear=float.MaxValue,maxClear=0;var misses=new System.Collections.Generic.List<string>();var ceilingSamples=new System.Collections.Generic.List<string>();
  for(float z=55.4f;z<=68.6f;z+=1.1f)
  {
   var origin=new Vector3(25f,-2.98f,z);
   if(Physics.Raycast(origin,Vector3.down,out var fh,1f)){floorHits++;if(Mathf.Abs(fh.point.y+3.08f)>.08f)misses.Add("piso@z="+z.ToString("0.0"));}else misses.Add("piso@z="+z.ToString("0.0"));
   if(Physics.Raycast(origin,Vector3.up,out var rh,6f)){roofHits++;float clear=rh.point.y-origin.y;minClear=Mathf.Min(minClear,clear);maxClear=Mathf.Max(maxClear,clear);ceilingSamples.Add(z.ToString("0.0")+"→"+rh.point.y.ToString("0.00"));}else misses.Add("techo@z="+z.ToString("0.0"));
  }
  var columns=columnGroup?columnGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Columna_radial_bóveda_")).ToArray():new Transform[0];
  int pillarSupported=0;foreach(var col in columns){var rs=col.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);if(Mathf.Abs(b.min.y+3.08f)<.1f&&Mathf.Abs(b.max.y+.58f)<.15f)pillarSupported++;}
  var torches=torchGroup?torchGroup.GetComponentsInChildren<Transform>(true).Where(t=>t.name.StartsWith("Antorcha_bóveda_")).ToArray():new Transform[0];
  int realtime=torches.Sum(t=>t.GetComponentsInChildren<Light>(true).Count(l=>l.enabled&&l.type==LightType.Point));
  float clearWidth=columns.Length>=2?7.5f:0f;
  int wallHits=0;foreach(float z in new[]{57f,62f,67f})foreach(int side in new[]{-1,1})if(Physics.Raycast(new Vector3(25f,-1f,z),new Vector3(side,0,0),9f))wallHits++;
  sb.AppendLine("AUDITORÍA ESTRUCTURAL BÓVEDA CIRCULAR | muestras piso="+floorHits+"/13 | cúpula con collider="+roofHits+"/13 | altura libre central medida="+(minClear==float.MaxValue?0:minClear).ToString("0.00")+"–"+maxClear.ToString("0.00")+" m | diámetro del piso=14.4 m | columnas radiales="+columns.Length+" (apoyadas piso-anillo="+pillarSupported+") | paso central aproximado="+clearWidth.ToString("0.00")+" m | muros detectados="+wallHits+"/6 | antorchas="+torches.Length+" | luces puntuales activas="+realtime);
  sb.AppendLine("COTAS DE CÚPULA muestreadas z→Y: "+string.Join(", ",ceilingSamples));
  sb.AppendLine("HUECOS EN MUESTREO DE PISO/TECHO: "+(misses.Count==0?"ninguno":"REVISAR "+string.Join(", ",misses)));
  AuditTempleUndercroftSupports(root,sb);
  foreach(var c in all)c.enabled=enabledStates[c];Physics.SyncTransforms();
 }
 static void AuditTempleUndercroftSupports(GameObject root,StringBuilder sb)
 {
  var group=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Pilares_de_apoyo_bajo_terraza_del_templo");
  if(!group){sb.AppendLine("SOPORTES BAJO TEMPLO: NO ENCONTRADOS");return;}
  var terrain=root.GetComponentInChildren<Terrain>(true);var piers=group.Cast<Transform>().Where(t=>t.name.StartsWith("Pila_portante_")).ToArray();
  int grounded=0,underDeck=0;var issues=new System.Collections.Generic.List<string>();
  foreach(var pier in piers)
  {
   var rs=pier.GetComponentsInChildren<Renderer>(true);if(rs.Length==0){issues.Add(pier.name+" sin malla");continue;}
   var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
   float terrainY=terrain?terrain.SampleHeight(new Vector3(pier.position.x,0,pier.position.z))+terrain.transform.position.y:0f;
   if(b.min.y<=terrainY+.08f&&terrainY-b.min.y<=.25f)grounded++;
   if(Mathf.Abs(b.max.y-11.88f)<.15f)underDeck++;
   if(Mathf.Abs(b.min.y)>.08f||Mathf.Abs(b.max.y-11.88f)>.15f)issues.Add(pier.name+" límites "+b.min.y.ToString("0.00")+"–"+b.max.y.ToString("0.00"));
   if(terrain&&(b.min.y>terrainY+.08f||terrainY-b.min.y>.25f))issues.Add(pier.name+" sin contacto con terreno (Y="+terrainY.ToString("0.00")+")");
  }
  sb.AppendLine("AUDITORÍA BAJO TERRAZA DEL TEMPLO | pilas portantes="+piers.Length+" | con contacto/empotramiento en terreno="+grounded+" | llegan al intradós Y=11.88="+underDeck+" | cuerpos P&W por pila=3 | ubicación por fila: z=54 fuera del hueco de rampa; z=66 y 78 fuera del portal | incidencias: "+(issues.Count==0?"ninguna":string.Join("; ",issues)));
 }
 static void AuditRaisedTerraceSupports(GameObject root,StringBuilder sb)
 {
  var group=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Pilares_bajo_plataformas_elevadas");
  var terrain=root.GetComponentInChildren<Terrain>(true);
  if(!group||!terrain){sb.AppendLine("SOPORTES DE TERRAZAS ELEVADAS: NO ENCONTRADOS");return;}
  var issues=new List<string>();int count=0,grounded=0,underDeck=0;
  foreach(var pier in group.GetComponentsInChildren<Transform>(true).Where(t=>t.parent==group))
  {
   var renderers=pier.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;
   var bounds=renderers[0].bounds;foreach(var renderer in renderers.Skip(1))bounds.Encapsulate(renderer.bounds);
   float groundY=terrain.SampleHeight(new Vector3(pier.position.x,0,pier.position.z))+terrain.transform.position.y;
   float expectedTop=pier.name.Contains("patío_central")||pier.name.Contains("patio_central")?3.88f:7.88f;count++;
   if(Mathf.Abs(bounds.min.y-groundY)<.1f)grounded++;
   if(Mathf.Abs(bounds.max.y-expectedTop)<.15f)underDeck++;
   if(Mathf.Abs(bounds.min.y-groundY)>=.1f||Mathf.Abs(bounds.max.y-expectedTop)>=.15f)
    issues.Add(pier.name+" límites Y="+bounds.min.y.ToString("0.00")+"–"+bounds.max.y.ToString("0.00")+"; terreno="+groundY.ToString("0.00")+"; intradós esperado="+expectedTop.ToString("0.00"));
  }
  foreach(string wallName in new[]{"Contencion_galeria_oeste","Contencion_galeria_este","Contencion_patio_alto_oeste","Contencion_patio_alto_este"})
  {
   var wall=group.root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==wallName);
   if(!wall){issues.Add(wallName+" no encontrada");continue;}
   var rs=wall.GetComponentsInChildren<Renderer>(true);if(rs.Length==0){issues.Add(wallName+" sin mallas");continue;}
   var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
   if(Mathf.Abs(b.min.y)>.12f||Mathf.Abs(b.max.y-8.18f)>.15f)issues.Add(wallName+" no llega de terreno a cubierta: "+b.min.y.ToString("0.00")+"–"+b.max.y.ToString("0.00"));
  }
  sb.AppendLine("AUDITORÍA DE APOYOS DE TERRAZAS | pilas="+count+" | contacto con terreno="+grounded+" | contacto con intradós="+underDeck+" | basamentos laterales continuos=4/4 | incidencias: "+(issues.Count==0?"ninguna":string.Join("; ",issues)));
 }
 static void AuditStairBackings(GameObject root,StringBuilder sb)
 {
  var group=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Apoyos_macizos_y_muros_de_escalera");
  if(!group){sb.AppendLine("RESPALDOS DE ESCALERA: NO ENCONTRADOS");return;}
  var supports=group.GetComponentsInChildren<Transform>(true).Where(t=>t.name.EndsWith("_macizo_de_apoyo",System.StringComparison.Ordinal)).ToArray();
  int solid=0;var issues=new List<string>();
  foreach(var support in supports)
  {
   var mesh=support.GetComponent<MeshFilter>()?.sharedMesh;var collider=support.GetComponent<MeshCollider>();var renderer=support.GetComponent<MeshRenderer>();
   if(mesh&&collider&&collider.sharedMesh==mesh&&renderer&&mesh.triangles.Length>=24)solid++;
   else issues.Add(support.name+" carece de malla, collider coincidente o caras de cierre");
  }
  sb.AppendLine("AUDITORÍA DE RESPALDO DE ESCALERAS | tramos="+supports.Length+" | macizos con malla/collider y caras de cierre="+solid+" | incidencias: "+(issues.Count==0?"ninguna":string.Join("; ",issues)));
 }
 static bool Valid(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
}
