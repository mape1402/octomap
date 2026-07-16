namespace OctoMap.Tests
{
    public class ProjectStructureTests
    {
        [Fact]
        public void OctoMap_Assembly_Is_Available()
        {
            Assert.Equal("OctoMap", typeof(OctoMapMarker).Assembly.GetName().Name);
        }
    }
}
