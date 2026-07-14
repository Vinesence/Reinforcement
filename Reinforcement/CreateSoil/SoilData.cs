using System.Collections.Generic;
using TriangleNet.Geometry;

namespace CreateSoil
{
    public class GeologyVertex : Vertex
    {
        public double TopZ { get; set; }
        public double BottomZ { get; set; }

        public GeologyVertex(
            double x,
            double y,
            double topZ,
            double bottomZ)
            : base(x, y)
        {
            TopZ = topZ;
            BottomZ = bottomZ;
        }
    }
    public class Borehole
    {
        public string Name { get; set; }

        public double X { get; set; }

        public double Y { get; set; }

        public double GroundElevation { get; set; }

        public List<SoilLayer> Layers { get; set; } = new List<SoilLayer>();
    }

    public class SoilLayer
    {
        public string IGE { get; set; }
        public double Thickness { get; set; }
        public double TopElevation { get; set; }
        public double BottomElevation { get; set; }
    }

    public class LayerPoint
    {
        public string IGE { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double TopZ { get; set; }
        public double BottomZ { get; set; }
    }
}