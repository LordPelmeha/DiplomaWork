using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Validation;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Шаг валидации мира после генерации.
    /// </summary>
    public sealed class ValidationStep : IWorldGenStep
    {
        public string Key => "Validation";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var result = WorldValidator.Validate(world);

            if (!result.IsValid)
            {
                Debug.LogError($"World validation failed: {string.Join("; ", result.Errors)}");
                // Не выбрасываем исключение, чтобы можно было увидеть мир в редакторе
                // Но логируем ошибку для отладки
            }

            if (result.Warnings.Count > 0)
            {
                Debug.LogWarning($"World validation warnings: {string.Join("; ", result.Warnings)}");
            }

            Debug.Log($"World validation: {result}");
        }
    }
}
