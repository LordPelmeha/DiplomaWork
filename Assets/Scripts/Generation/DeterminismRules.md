# Правила детерминизма

Этот документ описывает правила, которые необходимо соблюдать для обеспечения детерминированной генерации мира.

## 1. RNG (Генераторы случайных чисел)

### ✅ МОЖНО:
- Использовать `System.Random` через `SeedContext.CreateRng(streamKey)`
- Использовать уникальный ключ потока для каждого шага генерации:
  ```csharp
  var rng = seed.CreateRng("Graph");
  var rng2 = seed.CreateRng("Streets");
  ```
- Использовать `attemptIndex` для повторных попыток:
  ```csharp
  var rng = seed.CreateRng("Buildings", attemptIndex: 0);
  ```

### ❌ НЕЛЬЗЯ:
- **Никогда не используйте `UnityEngine.Random`** в коде генерации
- Не создавайте `new System.Random()` напрямую — используйте `SeedContext`
- Не используйте глобальное состояние RNG

## 2. Коллекции и порядок элементов

### ✅ МОЖНО:
- Использовать `List<T>` с последующей сортировкой
- Использовать `Dictionary<TKey, TValue>` только если порядок не важен
- Сортировать кандидаты перед выбором:
  ```csharp
  candidates.Sort((a, b) => a.Weight.CompareTo(b.Weight));
  ```

### ❌ НЕЛЬЗЯ:
- **Не полагайтесь на порядок `Dictionary`/`HashSet`** при итерации
- Не используйте `foreach` по `Dictionary` для критичного кода
- Не используйте `LINQ.OrderBy()` без явного компаратора

## 3. Плавающая точка

### ✅ МОЖНО:
- Использовать `float` для визуальных эффектов
- Использовать строго одинаковый порядок вычислений

### ❌ НЕЛЬЗЯ:
- **Не используйте `float` для ключевых решений** (выбор пути, валидация)
- Не сравнивайте float на точное равенство
- Не используйте float как ключ в Dictionary

## 4. Cellular Automata

### ✅ МОЖНО:
- Считать в буфер, потом применять
- Использовать фиксированный порядок прохода (слева направо, сверху вниз)

### ❌ НЕЛЬЗЯ:
- **Не обновляйте клетки "на месте"** без буфера
- Не используйте случайный порядок прохода

## 5. MST и выбор рёбер

### ✅ МОЖНО:
- Сортировать рёбра по весу + tie-break по индексам:
  ```csharp
  candidates.Sort((a, b) => {
      int c = a.Weight.CompareTo(b.Weight);
      if (c != 0) return c;
      c = a.NodeA.CompareTo(b.NodeA);
      if (c != 0) return c;
      return a.NodeB.CompareTo(b.NodeB);
  });
  ```

### ❌ НЕЛЬЗЯ:
- **Не выбирайте рёбра с равным весом без tie-break**

## 6. Архитектура

### ✅ МОЖНО:
- Генерация: `Seed + Config → WorldData` (чистая функция)
- Презентация: `WorldData → Tilemap/Instantiate` (отдельный слой)

### ❌ НЕЛЬЗЯ:
- **Не смешивайте генерацию с Unity-специфичным кодом**
- Не вызывайте `Instantiate`, `GetComponent` в шагах генерации
- Не читайте `Time.time`, `Time.frameCount` в генерации

## 7. Валидация и ремонт

### ✅ МОЖНО:
- Использовать детерминированный ремонт с фиксированными правилами
- Повторять генерацию с `attemptIndex`

### ❌ НЕЛЬЗЯ:
- **Не используйте недетерминированные эвристики**
- Не полагайтесь на "случайный успех" без фиксации seed

## Пример правильной структуры

```csharp
public sealed class DistrictGraphStep : IWorldGenStep
{
    public string Key => "Graph";

    public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
    {
        // ✅ Правильно: получаем RNG для этого шага
        var rng = seed.CreateRng(Key);
        
        // ✅ Правильно: используем только данные и RNG
        // ... код генерации ...
    }
}
```

## Нарушения и последствия

| Нарушение | Последствие |
|-----------|-------------|
| `UnityEngine.Random` | Разные результаты на разных платформах |
| Порядок `Dictionary` | Разные миры при одинаковом seed |
| Float в решениях | Недетерминизм из-за округления |
| Обновление без буфера | Зависимость от порядка прохода |

## Проверка детерминизма

Запустите тесты:
```bash
dotnet test
```

Или в Unity Test Runner убедитесь, что:
- `WorldHashTests.ComputeHash_SameSeed_ProducesSameHash` ✅
- `WorldGeneratorMvpTests.Generate_SameSeed_ProducesSameGround` ✅
- `DistrictGraphDeterminismTests.Graph_SameSeed_ProducesSameNodesAndEdges` ✅
- `StreetsDeterminismTests.Streets_SameSeed_ProducesSameRoadLayer` ✅
