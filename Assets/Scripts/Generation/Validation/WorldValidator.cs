using System;
using System.Collections.Generic;
using Diploma.Generation.Model;

namespace Diploma.Generation.Validation
{
    /// <summary>
    /// Базовая валидация мира: проверка на критические ошибки генерации.
    /// </summary>
    public static class WorldValidator
    {
        /// <summary>
        /// Выполняет полную валидацию мира.
        /// </summary>
        /// <returns>Результат валидации со списком ошибок.</returns>
        public static ValidationResult Validate(WorldData world)
        {
            var result = new ValidationResult();

            if (world == null)
            {
                result.AddError("World is null");
                return result;
            }

            // Проверка размера
            if (world.Size.x <= 0 || world.Size.y <= 0)
            {
                result.AddError($"Invalid world size: {world.Size}");
                return result;
            }

            // Проверка слоёв
            if (world.Ground == null)
                result.AddError("Ground layer is null");
            if (world.Roads == null)
                result.AddError("Roads layer is null");
            if (world.Walls == null)
                result.AddError("Walls layer is null");

            // Проверка графа
            if (world.Graph == null)
            {
                result.AddError("District graph is null");
            }
            else
            {
                ValidateGraph(world.Graph, result);
            }

            // Проверка связности дорог (если есть дороги)
            if (world.Roads != null && !result.HasErrors)
            {
                ConnectivityValidator.ValidateConnectivity(world, result);
            }

            return result;
        }

        private static void ValidateGraph(DistrictGraph graph, ValidationResult result)
        {
            if (graph.Nodes.Count == 0)
            {
                result.AddError("Graph has no nodes");
                return;
            }

            // Проверка узлов на выход за границы
            // (границы будут проверены в контексте мира, если нужно)

            // Проверка рёбер на корректность индексов
            for (int i = 0; i < graph.Edges.Count; i++)
            {
                var edge = graph.Edges[i];
                if (edge.a < 0 || edge.a >= graph.Nodes.Count)
                {
                    result.AddError($"Edge {i} has invalid node index a={edge.a}");
                }
                if (edge.b < 0 || edge.b >= graph.Nodes.Count)
                {
                    result.AddError($"Edge {i} has invalid node index b={edge.b}");
                }
            }

            // Проверка на изолированные узлы (узлы без рёбер)
            var connectedNodes = new HashSet<int>();
            foreach (var edge in graph.Edges)
            {
                connectedNodes.Add(edge.a);
                connectedNodes.Add(edge.b);
            }

            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                if (!connectedNodes.Contains(i))
                {
                    result.AddWarning($"Node {i} at {graph.Nodes[i].position} is isolated (no edges)");
                }
            }
        }
    }

    /// <summary>
    /// Результат валидации.
    /// </summary>
    public sealed class ValidationResult
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        public IReadOnlyList<string> Errors => _errors;
        public IReadOnlyList<string> Warnings => _warnings;
        public bool HasErrors => _errors.Count > 0;
        public bool IsValid => _errors.Count == 0;

        public void AddError(string message)
        {
            _errors.Add(message);
        }

        public void AddWarning(string message)
        {
            _warnings.Add(message);
        }

        public override string ToString()
        {
            if (IsValid)
                return $"Valid ({_warnings.Count} warnings)";

            return $"Invalid ({_errors.Count} errors, {_warnings.Count} warnings)";
        }
    }
}
