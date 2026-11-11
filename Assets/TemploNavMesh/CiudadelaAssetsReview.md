# Ciudadela Maya — paquete para revisión

## Entrega de esta etapa

- Escena nueva: `Assets/Scenes/CiudadelaMaya_AssetsPreview.unity`.
- Prefab raíz independiente: `Assets/TemploNavMesh/Prefabs/CiudadelaMaya_DesdeFotos_Revision.prefab`.
- El montaje utiliza un Terrain de 300 × 260 m separado del TerrainData del blockout aprobado, piezas P&W y vegetación ZNS3D.
- La escena conserva la lógica aprobada: plaza de inicio, eje ceremonial, terrazas elevadas, patios este y oeste, galerías con marcos, maze corto y túnel subterráneo hacia el patio posterior.
- `Single_Group_Tall_Corner_1A` no se usa en este montaje. Su asset fuente no se eliminó.

## Control geométrico realizado

- Se midieron los 192 prefabs del paquete P&W para revisar dimensiones, centros, renderers y colliders.
- Las plataformas principales se alinean en una retícula de 8 m y conservan la escala original de cada prefab.
- Cada losa tiene ahora un collider plano adicional exactamente sobre la cota de la cara superior. Así el jugador no queda 5–10 cm hundido respecto de algunas colisiones originales del paquete; las losas colindantes se tocan sobre la retícula.
- Los tramos de escalera se visualizan con `Basic_Stairs_1A`. Sus colliders originales se desactivan en favor de una superficie continua de pendiente suave, para que un posterior bake de NavMesh no dependa de colisiones irregulares entre piezas.
- El recorrido a la bóveda tiene dos tramos visibles de escaleras, muros guía laterales, rampa de bajada, losa de piso y rampa de salida. Tras alinear ambos extremos de rampa, el cambio máximo observado es 0,15 m entre muestras de 0,40 m; no hay huecos ni saltos mayores de 0,18 m en esos puntos.
- La bóveda se amplió a una sala circular de 14,4 m de diámetro con piso circular a Y=−3,08 m, muros segmentados en piedra, cúpula y dos portales opuestos. El terreno forma una cobertura baja sobre la cúpula y mantiene abiertas las dos rampas de acceso.
- La sala circular tiene ocho columnas P&W radiales, cuatro antorchas y cuatro luces cálidas en tiempo real. El paso libre aproximado en el centro queda en 7,5 m; la altura libre muestreada va de 2,63 a 2,98 m.
- Los colliders planos de piso, muros curvos, cúpula y portales se revisaron por raycast: piso 13/13, techo 13/13 y muros 6/6; no se detectaron huecos en la muestra. El recorrido completo de las dos rampas y la bóveda tiene cambio máximo de 0,15 m entre muestras de 0,40 m y enlaza con el patio posterior.
- Bajo la terraza elevada del templo se añadieron otras doce pilas portantes, cada una formada por tres módulos de columna P&W. La auditoría de sus bounds confirma apoyo sobre el terreno a Y=0 y contacto con el intradós a Y=11,88 m en las doce pilas, sin incidencias y fuera de las aberturas de escalera y portal.
- Se probaron con raycast 13 puntos de piso: inicio, avenida, patio central, terrazas oeste/este, galerías oeste/este, patios altos oeste/este, terraza del templo, maze, bóveda y patio posterior. Cada lectura coincide con su cota prevista con delta 0,00 m.
- Se comprobó la altura del terreno bajo seis puntos de rutas laterales y salida: cinco dan Y=0,00 m y el punto de salida más al norte Y=0,03 m. En esas posiciones las estructuras laterales ya no quedan enterradas por el terreno.
- Para dar más cuerpo a los espacios se añadieron 19 instancias decorativas estructurales P&W (pilares, barandas, postes y baranda de mirador), además de los arcos de bóveda. Se usan 61 árboles, 180 matas y 28 rocas en las laderas.
- Se generaron ocho materiales URP/Lit para los materiales de follaje, troncos y roca de ZNS3D, con mapas originales, recorte alfa y doble cara para hojas. Los materiales fuente del paquete se conservan intactos.

## Aún requiere tu aprobación visual

Este archivo es una **propuesta de composición con activos**, no una versión final ni un NavMesh terminado. La lectura geométrica es una comprobación de colliders por raycasts, no una prueba con el controlador del jugador. La captura adjunta usa materiales temporales de compatibilidad porque la captura batch no renderiza todas las variantes del shader URP; por eso puede mostrar materiales distintos a la escena. Abre la escena en Unity para revisar la iluminación y los materiales reales. Antes de integrar en `TemploNavMesh`, queda recorrer con el controlador las escaleras, anchos de pasillos, encuentros de muros, lectura del maze y salida del túnel.

## Revisión en Unity

1. Abre `Assets/Scenes/CiudadelaMaya_AssetsPreview.unity`.
2. Selecciona `Ciudadela_Maya_PW_Revision_de_Assets` en la jerarquía y encuadra la selección.
3. Revisa primero la composición desde arriba; luego coloca la cámara al frente de la escalinata y recorre el maze, las galerías laterales y el túnel.
4. Aprueba o marca cambios antes de importar el prefab a `TemploNavMesh`.

## Corrección arquitectónica aprobada — 10 de noviembre de 2026

Se aplicaron los ocho grupos de corrección sobre la escena de revisión y su prefab independiente. `TemploNavMesh` no se modificó durante esta pasada.

| Grupo | Cambio aplicado | Verificación |
|---|---|---|
| Columnas del patio | Se añadieron seis continuaciones portantes debajo de las columnas decorativas, desde terreno Y=0 hasta el intradós del piso Y=3.88 m. | 6/6 en contacto con terreno e intradós. |
| Terrazas y galerías elevadas | Se añadieron diez pilas bajo patios y galerías; cuatro basamentos laterales ahora bajan hasta terreno en dos hiladas completas. | 10/10 pilas conectadas; 4/4 basamentos van de Y=0 a Y=8.18 m. |
| Reversos de escaleras | Se conservaron los macizos estructurales con caras de cierre detrás y a los lados de cada tramo. | 23/23 escaleras principales con malla y collider coincidentes; el acceso de la bóveda tiene macizo propio. |
| Uniones de escaleras y rampas | Se añadió un descanso superior de 4 × 1.6 m y se alineó la rampa de descenso con el final de la escalera. La rampa de salida ahora termina en el borde del patio posterior. | Recorrido central muestreado cada 0.40 m: cero muestras sin piso, cambio máximo 0.15 m, ninguna transición mayor a 0.18 m. |
| Columnas cerca de pasos | Las nuevas pilas del patio quedan fuera del eje central de 32 m; las de las galerías se ubicaron fuera de los accesos laterales y del radio de la bóveda. | Cotas y posiciones verificadas en la auditoría de montaje; el recorrido no muestra pérdida de piso. |
| Juntas de muros | Los muros de las cuatro galerías/patios altos se construyen en dos hiladas de 4.09 m que se tocan en la junta, con esquinas solapadas por el módulo de muro. | Rangos de bounds cubren terreno a coronación; inspeccionar acabado de juntas desde el editor antes de integrar. |
| Terreno y bases | Las pilas nuevas usan la altura real del Terrain en cada coordenada; los cuatro basamentos revisados parten de Y=0. | Cota de terreno verificada en las diez posiciones: Y=0.00 m. |
| Piezas que parecían fallas | Se conservaron las seis columnas superiores porque ahora tienen continuación estructural debajo. `Single_Group_Tall_Corner_1A` sigue excluido del montaje. | Confirmado en jerarquía e informe de montaje. |

### Resultado de auditoría

- Los 13 puntos de control de pisos coinciden con la cota prevista, delta 0.00 m.
- La bóveda conserva piso y techo en 13/13 muestras, ocho columnas entre piso y anillo, seis de seis muros detectados, cuatro antorchas y cuatro luces activas; no se detectaron huecos en las muestras de piso/techo.
- Las doce pilas del templo siguen en contacto con terreno y con el intradós Y=11.88 m.
- No se calculó ni integró NavMesh. La revisión no sustituye un recorrido en Play Mode con el controlador del personaje; tampoco certifica visualmente todas las juntas, materiales, sombras ni colisiones de gameplay.

Informes actualizados: `CiudadelaAssetPlacementAudit.txt` y `CiudadelaWalkingSurfaceAudit.txt`.

## Revisión ampliada previa a integración — 10 de noviembre de 2026

- Se reforzaron las seis columnas del patio con un plinto P&W visible de 0,42 m sobre cada losa. Una auditoría por nombre y bounds enlaza cada fuste con su plinto y pila hasta terreno: 6/6 continuas, sin incidencias.
- Se volvió a ejecutar la auditoría de pisos usando exclusivamente los colliders de las superficies transitables. Los 13 puntos de control coinciden con la cota prevista, incluido el inicio a Y=0,18 m. La plaza había dado una lectura falsa porque el rayo chocaba con el `CharacterController` y porque el resultado no se filtraba a colliders de piso.
- La escena contiene una Aventurera de revisión con `CharacterController` de 2,04 m × 0,34 m, cámara siguiendo al jugador y un punto inicial despejado sobre Y=0. No se integró el prefab en `TemploNavMesh`.
- Se repitieron las comprobaciones de bóveda, rampas, apoyos de terraza y respaldos: bóveda con piso/techo 13/13, columnas radiales 8/8, recorrido geométrico del túnel sin muestras perdidas y transición máxima muestreada de 0,15 m; apoyos bajo terraza 12/12 y terrazas 10/10; respaldos cerrados 23/23.
- **Pendiente para aprobación:** no se completó un recorrido en Play Mode con el personaje. La apertura/compilación del editor Unity se cerró durante esta pasada. La malla de navegación de esta escena sigue sin bake; por eso no se certifica navegación real ni paso del jugador por escaleras, maze y bóveda. Tampoco se certifican con esta auditoría todas las oclusiones, sombras o juntas a cualquier ángulo.
- Resultado: mejora geométrica y prefab guardados; no declarar listo para integración a `TemploNavMesh` hasta completar inspección visual en una sesión estable y recorrido físico con el controlador. El NavMesh se hornea después, en la escena objetivo, si se aprueba el montaje.
