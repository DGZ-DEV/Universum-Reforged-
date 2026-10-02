# Universum — port a RimWorld 1.6

Port del framework **Universum** (v2.4.0, de **sindre0830**, licencia MIT) a RimWorld 1.6.
Universum es la dependencia de RimNauts 2: sin él, RimNauts 2 no carga.

## Estado

- Compila con **0 errores y 0 avisos**.
- Carga con **Player.log limpio**: ninguna excepción.
- Inicializa sus **13 utilidades, 25 biomas, 384 terrenos, 232 genes** y sus assets.
- **51 parches de Harmony** aplicados, ninguno autodesactivado.
- El planeta y sus capas se dibujan, y convive con los cinco DLC activos
  (Royalty, Ideology, Biotech, Anomaly y Odyssey).

## Flujo de trabajo

### 1. Reunir el material

Cloné la fuente con `git clone --depth 1` —para que el primer commit fuese el original
prístino— y descargué además el release publicado, que es la referencia de «qué hace hoy».
Leí la licencia antes que el código: es MIT, así que permite modificar y redistribuir
conservando el aviso de copyright y el texto de la licencia.

### 2. Poner a compilar

El `csproj` original es del formato antiguo de MSBuild, que además lleva un espacio de
nombres XML incrustado que rompe muchos lectores. Lo convertí a estilo SDK, `net48`, con
`Microsoft.NETFramework.ReferenceAssemblies`. Reapunté las referencias: las del juego a
ruta absoluta, Harmony a la carpeta de `Mods` en vez del taller de Steam, y conservé el
`csproj` original renombrado como `.csproj.original`.

### 3. Censar antes de tocar

Agrupé los errores por archivo, por código y por causa, y lo guardé en un documento.
Conviene saber esto desde el principio: **el compilador de C# informa por fases**. Un error
de declaración aborta el análisis de los cuerpos de los métodos, así que el contador **sube**
al arreglar cosas. En este port pasó de 6 a 86, y no era un retroceso: eran errores que
estaban detrás.

### 4. Construir un oráculo de API

Escribí un pequeño ejecutable `net48` que carga el `Assembly-CSharp` real del juego y
responde consultas: ¿existe este tipo? ¿este miembro? ¿de qué tipo es? ¿cuántas sobrecargas
tiene? ¿dónde vive este miembro? Sustituye a la deducción, y desde luego a buscar cadenas
dentro del `dll`, que da **falsos negativos** con nombres cortos y comunes (`Material`,
`Normal`, `Rare`, `FadeRough` existen, y esa comprobación no los encontraba).

### 5. Espejos de reflexión

En 1.6 muchos miembros de vanilla dejaron de ser públicos pero **siguen existiendo**. Para
esos escribí un espejo por tipo:

- **Campos**: `AccessTools.FieldRefAccess` devuelve **referencia**, así que la misma llamada
  sirve para leer, escribir y pasar el valor a un parámetro `ref`.
- **Propiedades y métodos privados**: `MethodInfo` + `Invoke`.

### 6. Los tres patrones de 1.6

Casi todo el trabajo cayó en tres sacos:

1. **`int` → `PlanetTile`**: el índice de casilla del mundo dejó de ser un entero y pasó a
   ser una estructura, y eso cambia la **firma** de muchos métodos de vanilla.
2. **Renombrados de familia**: `Traveling` → `Travelling`, `TransportPods` → `Transporters`,
   `ActiveDropPod` → `ActiveTransporter`, `WorldLayer_*` → `WorldDrawLayer_*`.
3. **Privatizaciones masivas**: miembros que siguen ahí pero ya no son accesibles.

### 7. Blindar y autodiagnosticar

Todo el parcheo va dentro de un `try/catch`. Antes, un solo parche malo se llevaba por
delante el constructor entero del mod —ajustes, definiciones y assets—, y **desde fuera
parecía que todo funcionaba**. Añadí además una línea de registro al arrancar que dice
cuántos parches se aplicaron, cuáles se autodesactivaron y cuáles se quedaron sin objetivo.

Y una regla que me ahorró varias rondas al final: **cuando el error no nombra al culpable,
se arregla el error antes que el código**. Cambiar `e.Message` por `e.ToString()` fue lo que
rompió una racha de cinco hipótesis fallidas.

### 8. Auditar los puntos ciegos

Todo lo que se resuelve **por cadena, por atributo o por firma** es un punto ciego. La lista,
que conviene hacer entera desde el principio:

- Objetivos por cadena de texto.
- Objetivos por atributo, **incluidos los constructores**.
- Tipo del miembro: método, propiedad, campo o constructor.
- **Sobrecargas**: si hay varias, la búsqueda por nombre es ambigua y devuelve nulo.
- Inyecciones de campo `___campo`.
- Firmas de parámetros con tipos del juego.

### 9. Verificar en el juego

Siete pasos, en este orden: compilar · cargar sin excepciones · XML limpios · parches
confirmados por registro · el planeta se dibuja · convivencia con los DLC · y solo entonces
el siguiente mod encima.

Dos cosas que aprendí aquí:

- **Leer `Player.log`, no la pantalla.** Un mod muerto por dentro se ve exactamente igual
  que uno que funciona.
- **El juego es el validador del XML.** El compilador no mira las definiciones; RimWorld
  dice el campo, el archivo y la línea.

## Los fallos que encontré

| Fallo | Causa |
|---|---|
| El mod no cargaba: constructor estático muerto | Un `Postfix` de `TileFinder` declaraba la firma de 1.5 (`int tile`) y en 1.6 es `PlanetTile`. Harmony generaba IL inválido y `PatchAll` abortaba |
| La pantalla de escenarios no avanzaba | `Cache.clear()` reventaba con `NullReferenceException`: `Caching_Handler` es un `GameComponent` y todavía no existía. La fachada ahora comprueba nulos |
| Dos funciones muertas en silencio | El campo `biome` vive en `Tile` y las casillas son `SurfaceTile`: `GetField` no ve los miembros privados de la clase base. Ahora se recorre la jerarquía |
| Un parche no encontraba su método | `AccessTools.Method` con la lista de parámetros de 1.5. `Type.GetMethod` exige coincidencia **exacta** |
| Otro parche, sobrecargado | `GenCelestial.CelestialSunGlow` tiene dos sobrecargas y la búsqueda por nombre devuelve nulo |
| Otro, sobre un constructor | Harmony no puede resolver un parche de constructor por atributo. Se parchea a mano, con guarda de nulo |
| `<holdSnow>` en un `TerrainDef` | En 1.6 ese campo no existe en ningún tipo |
| `<placingDraggableDimensions>` en dos `ThingDef` | Tampoco existe |
| Un parche XML apuntaba al def viejo | `TravelingTransportPods` → `TravellingTransporters` |

## Pendiente

`SectionLayer_FinalizeMesh` queda retirado con aviso. Harmony indicó el destino correcto
(`Verse.MapDrawLayer::FinalizeMesh(MeshParts)`), pero el cuerpo del parche depende de
`SectionLayer.map`, `SectionLayer.subMeshes` y de un campo `Section` inyectado, y
`MapDrawLayer` no expone esos datos. Reapuntarlo exige **reescribir el cuerpo**, no cambiar
el objetivo, así que queda pendiente y documentado.

## Créditos y licencia

Universum es obra de **sindre0830** y se distribuye bajo **licencia MIT**. Este port
conserva su aviso de copyright y su licencia. El código original está en
<https://github.com/RimNauts/Universum>.
