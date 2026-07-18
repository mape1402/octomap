namespace OctoMap.Tests
{
    public class ProjectStructureTests
    {
        [Fact]
        public void OctoMap_Assembly_Is_Available()
        {
            Assert.Equal("OctoMap", typeof(OctoMapMarker).Assembly.GetName().Name);
        }

        [Fact]
        public void Dynabee_Backend_Uses_Dynabee_Abstractions()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "OctoMap",
                "Generation",
                "Dynabee",
                "DynabeeMappingGenerationBackend.cs"));
            var source = File.ReadAllText(sourcePath);

            Assert.DoesNotContain("DynaBeeBuilder", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Activator.CreateInstance", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ILGenerator", source, StringComparison.Ordinal);
            Assert.DoesNotContain("OpCodes", source, StringComparison.Ordinal);
            Assert.Contains("IDynaBeeAssemblyBuilderFactory", source, StringComparison.Ordinal);
            Assert.Contains("EmitsBody", source, StringComparison.Ordinal);
            Assert.Contains("CreateInstance", source, StringComparison.Ordinal);
        }

        [Fact]
        public void CompiledMap_Does_Not_Expose_Dynabee_Contracts()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "OctoMap",
                "Generation",
                "CompiledMap.cs"));
            var source = File.ReadAllText(sourcePath);

            Assert.DoesNotContain("DynaBee", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IDynaBeeBoundMethodInvoker", source, StringComparison.Ordinal);
            Assert.Contains("ICompiledMapInvoker", source, StringComparison.Ordinal);
        }

        [Fact]
        public void Projection_Builder_Does_Not_Reference_Dynabee()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "src",
                "OctoMap",
                "Projection",
                "OctoProjectionBuilder.cs"));
            var source = File.ReadAllText(sourcePath);

            Assert.DoesNotContain("DynaBee", source, StringComparison.Ordinal);
            Assert.DoesNotContain("Dynabee", source, StringComparison.Ordinal);
            Assert.DoesNotContain("IMappingGenerationBackend", source, StringComparison.Ordinal);
        }
    }
}
