using System;
using System.IO;
using System.Text.Json;
using data_layer;

namespace cli
{
    public class Loader
    {
        private const string Benchmarks = "solomon-vrptw-benchmarks";

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        private readonly string _nr;

        private readonly string _type;
        private readonly string _version;

        public Loader(
            string type = "c",
            string version = "1",
            string nr = "01"
        )
        {
            _type = type;
            _version = version;
            _nr = nr;
        }

        private string Name => $"{_type}{_version}{_nr}";

        public Instance Instance()
        {
            string? path = $"{Benchmarks}/{_type}/{_version}/{Name}.json";
            string? json = File.ReadAllText(path);
            Instance instance = JsonSerializer.Deserialize<Instance>(json, Options)
                                ?? throw new InvalidOperationException();
            return instance;
        }

        public Result Result()
        {
            string? path = $"{Benchmarks}/results/{Name}.json";
            string? json = File.ReadAllText(path);
            Result result = JsonSerializer.Deserialize<Result>(json, Options) ?? throw new InvalidOperationException();
            return result;
        }
    }
}