# DiplomaWork — Процедурная генерация города в Unity

Дипломный проект по теме **детерминированной процедурной генерации** 2D города на движке Unity.

## 🎯 Ключевая идея

**Генерация = чистая функция "seed + config → WorldData"**

Всё Unity-специфичное (Tilemap, Instantiate, сцена) вынесено в отдельный слой "построения" (presentation layer). Это позволяет:
- ✅ Гарантировать **воспроизводимость** (один seed → одинаковый мир)
- ✅ **Тестировать** генерацию без Unity
- ✅ **Хешировать** результат для проверки детерминизма
- ✅ **Сериализовать** мир в JSON

---

## 🏗️ Архитектура

```
┌─────────────────────────────────────────────────────────────┐
│                    GENERATION LAYER                         │
│  (чистый C#, без зависимостей от Unity)                     │
│                                                             │
│  SeedContext + WorldGenConfig → WorldGeneratorService       │
│                           ↓                                 │
│  WorldGenPipeline (последовательность шагов):               │
│    1. BaseFillStep — заполнение землёй                      │
│    2. DistrictGraphStep — граф районов (MST + extra edges)  │
│    3. StreetCarvingStep — дороги (L-образные соединения)    │
│    4. BlockLayoutStep — поиск кварталов                     │
│    5. BuildingLayoutStep — генерация зданий                 │
│    6. DecorationPlanStep — план декораций                   │
│    7. ValidationStep — валидация мира                       │
│                           ↓                                 │
│  WorldData (размер, слои тайлов, граф, здания, spawn plan)  │
└─────────────────────────────────────────────────────────────┘
                            ↓
┌─────────────────────────────────────────────────────────────┐
│                  PRESENTATION LAYER                         │
│  (Unity-специфичный код)                                    │
│                                                             │
│  TilemapBuilder — построение Tilemap из WorldData           │
│  PropInstantiator — инстанс префабов из SpawnPlan           │
└─────────────────────────────────────────────────────────────┘
```

---



---

## 🔧 Настройка генерации

### WorldGenConfig

| Параметр | Описание | Значение по умолчанию |
|----------|----------|----------------------|
| **Map Size** | Размер карты в клетках | (128, 128) |
| **District Count** | Количество районов | 8 |
| **Extra Edges** | Дополнительные рёбра графа | 3 |
| **Road Radius** | Радиус дорог | 1 |
| **Lamp Interval** | Интервал между фонарями | 8 |
| **Min Road Segment Length** | Мин. длина отрезка для фонарей | 5 |
| **Tree Spawn Chance** | Шанс спавна дерева | 15% |
| **Max Tree Count** | Макс. количество деревьев | 0 (без лимита) |

### PrefabCatalog

Создайте ассет: **Assets → Create → Diploma → Prefab Catalog**

| ID | Префаб | Описание |
|----|--------|----------|
| 0-99 | Buildings | Здания (Residential, Commercial, Industrial) |
| 1000+ | Trees | Деревья (можно несколько видов) |
| 1001+ | Lamps | Фонари |

---

## 🎮 Использование

### В Editor

1. Откройте сцену `Main.unity`
2. Найдите GameObject **WorldBootstrap**
3. Настройте параметры:
   - **Config**: WorldGenConfig.asset
   - **Seed**: фиксированный seed или включите **Use Random Seed**
   - **Tile Asset Set**: TileAssetSet.asset
   - **Prefab Catalog**: PrefabCatalog.asset
4. Запустите игру или вызовите **Context Menu → Generate**

### Через код

```csharp
var config = Resources.Load<WorldGenConfig>("WorldGenConfig");
var pipeline = new WorldGenPipeline()
    .Add(new BaseFillStep())
    .Add(new DistrictGraphStep())
    .Add(new StreetCarvingStep());

var world = WorldGeneratorService.Generate(seed: 12345, config, pipeline);

// Проверка детерминизма
Debug.Log($"World Hash: {world.Meta.WorldHash}");
```

---

## 🧪 Тестирование

Запустите **Unity Test Runner** (Window → General → Test Runner).

### Ключевые тесты

| Тест | Описание |
|------|----------|
| `WorldHashTests.ComputeHash_SameSeed_ProducesSameHash` | Одинаковый seed → одинаковый hash |
| `WorldHashTests.ComputeHash_DifferentSeeds_ProducesDifferentHash` | Разные seed → разные hash |
| `DistrictGraphDeterminismTests.Graph_SameSeed_ProducesSameNodesAndEdges` | Детерминизм графа |
| `StreetsDeterminismTests.Streets_SameSeed_ProducesSameRoadLayer` | Детерминизм дорог |

---

