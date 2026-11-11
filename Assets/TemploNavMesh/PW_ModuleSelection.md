# Selección preliminar de módulos P&W para la Ciudadela Maya

Este documento traduce el blockout aprobado a familias de assets. Las medidas proceden de una medición dentro de Unity de todos los prefabs P&W: bounds combinados de renderer y collider, desplazamiento del centro frente al pivote, cantidad de colliders y número de hijos. Datos completos: `PW_ModuleDimensions.csv` y `PlatformPrefabSurvey.txt`.

## Mapa de uso propuesto

| Zona del blockout | Módulos candidatos | Criterio de montaje |
|---|---|---|
| Plaza inicial, avenida y patios | `HD_Platform_6A` (8×0.30×8 m, 1 collider), `HD_Platform_3A/4A` (4×0.30×8 m), `8x8x.25_Floor_A` (8×0.50×8 m) | Losas en retícula; definir la cara superior común y comprobar las juntas contra el terreno. Usar la losa 8×8 como módulo de cobertura y las 4×8 para remates. |
| Escalinata ceremonial | `Temple_Stairs_Entrance_1B` (67.19×6.08×13.06 m; 29 colliders), evaluar también `Temple_Stairs_Entrance_1A` (20×4×6.30 m; 17 colliders) | El modelo 1B es muy ancho: reducirlo con escala uniforme/controlada solo después de revisar visualmente la forma de los escalones. Sus bounds tienen centro relativo al pivote (0, 1.04, −1.63), por lo que no se debe posicionar desde el pivote sin compensación. |
| Conexiones a terrazas y descansos | `Stairs_Platform_2A` (24×4.50×18.53 m; 5 colliders), `Stairs_Platform_1A` (13.99×4×16 m; 5 colliders), `Basic_Stairs_1A` (4×3.99×6.27 m) | Probar en cada desnivel real del blockout; los bounds no bastan para saber dónde terminan las huellas. Alinear superficies superior e inferior con medición en Unity. |
| Rutas laterales con marcos | `HD_Arch_1A` (5.76×6.62×1.21 m) o `Basic_Arch_1A` (5.60×6.62×1.20 m) | Separar marcos por bays regulares sobre una galería continua. No poner el marco donde invade el ancho útil de circulación. |
| Maze sencillo en terraza oeste | `Unique_Wall_B` (4.20×2×1.20 m; 2 colliders) y losas de 4×8/8×8 | Muros bajos, pasillos de al menos 3–4 m, ruta con pocos giros y salida visible al patio. Evitar muros altos que conviertan el reto fácil en laberinto cerrado. |
| Túnel corto | `HD_Tunnel_1A/1B` (8×5.25×4.30 m / 8×4.25×4.30 m) y `HD_Tunnel_Ramp_1B` (14.28×4.25×4.30 m) | El túnel se arma por módulos de longitud, con piso continuo, entrada y salida alineadas con el corte del terreno. No usar `Unique_Tunnel_1A` sin revisar: mide 8×10.50×4.30 m y su centro de renderer queda 2.83 m sobre el pivote. |
| Basamentos, muros de terraza y paramentos | `HD_Wall_2A` (2.20×4×1.20 m) y `Basic_Wall_2A` (4×4×1 m) | Elegir longitudes compatibles con la retícula, cubrir cambios de nivel y cerrar encuentros de esquina; cortar los tramos solo en escaleras, rampas o accesos intencionales. |
| Templo y mirador | Componer con arcos, muros, pilares y plataformas individuales; evaluar `Entrance_1A` como referencia de conjunto | Los prefabs compuestos grandes se inspeccionan antes de colocarlos. `Entrance_3A/3B` tienen centros desplazados respecto del pivote; no usarlos como bloques negros sin compensación. |

## Exclusión geométrica explícita

`Single_Group_Tall_Corner_1A` queda **fuera de esta composición**. Medición: 7.23×8.02×7.23 m; centro de bounds (−2.50, 2.21, −0.53) respecto al pivote; 12 renderers y 12 colliders. Su pivote desplazado y forma compuesta justifican no arrastrarlo como pieza suelta ni usarlo para rellenar huecos. La fuente del paquete se conserva intacta.

## Materiales y terreno

P&W aporta `Walls_1.mat` y las texturas `Walls_1.png` / `Walls_1(Bumped).png`; no contiene `TerrainLayer`. El terreno del blockout es Terrain de Unity con las capas disponibles de ZNS3D (roca, suelo y pasto). Al sustituir volúmenes se preserva ese terreno esculpido; las losas de piedra solo cubren plazas y caminos construidos.

## Orden para construir sin huecos ni piezas mal ubicadas

1. Fijar cotas del terreno y superficies terminadas de cada terraza.
2. Montar pisos en retícula y verificar las juntas entre cada módulo.
3. Construir muros de contención y zócalos de apoyo; comprobar que no floten ni atraviesen el piso.
4. Instalar escaleras y rampas; verificar llegada de cada huella y continuidad de collider desde nivel bajo hasta nivel alto.
5. Añadir marcos, pilares, templetes y detalles solo donde no estrechen las rutas.
6. Decorar con vegetación al final, fuera de las rutas y de los barridos de las escaleras.
7. Revisar la escena desde arriba y a nivel de jugador; después de aprobar geometría, preparar NavMesh.

La matriz es una recomendación técnica previa. Las piezas aún no se han montado en el blockout: hay que validar la forma y unión en escena antes de reemplazar los volúmenes temporales.

## Resultado del ajuste en la escena de preview

La composición actual está instanciada en `Assets/Scenes/CiudadelaMaya_AssetsPreview.unity` y guardada como prefab nuevo `Assets/TemploNavMesh/Prefabs/CiudadelaMaya_DesdeFotos_Revision.prefab`. Para escalones visuales se eligió `Basic_Stairs_1A` (4×3.99×6.27 m), en tramos por módulos; sus colliders originales se desactivan y cada tramo recibe un BoxCollider inclinado continuo de 4 m de ancho y 4 m de desnivel en 12.54 m de recorrido. Cada módulo de piso recibe además un collider plano alineado con la cara superior objetivo. La lectura de 13 puntos sobre pisos elevados y bajos coincide con su cota prevista (delta 0,00 m). El recorrido a la bóveda se ajustó en los descansos; el cambio máximo entre muestras contiguas de 0,40 m es 0,13 m. El patio posterior, piso de bóveda (Y≈−3.08 m) y rampas están presentes; el bake del NavMesh aún no se ha hecho.

### Ajustes de detalle de la revisión

Se colocaron 19 instancias estructurales decorativas P&W (pilares, barandas, postes y remates de mirador). La vegetación de ZNS3D suma 61 árboles, 180 matas y 28 rocas, distribuida sobre terreno lateral; se crearon ocho materiales URP/Lit con los mapas de textura del pack para corregir follaje magenta sin modificar los materiales originales. El terreno se ha dejado a cota Y=0 bajo seis puntos revisados de las rutas laterales y del patio posterior (cinco lecturas Y=0.00 m, una Y=0.03 m).
