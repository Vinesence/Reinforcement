using Autodesk.Revit.DB;
using CreateSoil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriangleNet.Geometry;
using Mesh = TriangleNet.Mesh;

namespace Reinforcement.CreateSoil
{
    public class DirectShapeBuilder
    {
        public void Create(Document doc, Dictionary<string, List<LayerPoint>> geology)
        {
            foreach (var pair in geology)
            {
                if (pair.Value.Count < 3)
                    continue;

                CreateIGEBody(doc, pair.Key, pair.Value);
            }
        }

        private void CreateIGEBody(Document doc, string ige, List<LayerPoint> points)
        {
            Polygon polygon =
        new Polygon();

            foreach (var p in points)
            {
                polygon.Add(new GeologyVertex(p.X, p.Y, p.TopZ, p.BottomZ));
            }

            Mesh mesh = (Mesh)polygon.Triangulate();
            TessellatedShapeBuilder builder = new TessellatedShapeBuilder();

            builder.OpenConnectedFaceSet(true);

            //Обходим треугольники
            foreach (var triangle in mesh.Triangles)
            {
                GeologyVertex v1 =
                    triangle.GetVertex(0)
                    as GeologyVertex;

                GeologyVertex v2 =
                    triangle.GetVertex(1)
                    as GeologyVertex;

                GeologyVertex v3 =
                    triangle.GetVertex(2)
                    as GeologyVertex;
                //Получаем точки верха
                XYZ top1 = ToXYZ(v1.X, v1.Y, v1.TopZ);
                XYZ top2 = ToXYZ(v2.X, v2.Y, v2.TopZ);
                XYZ top3 = ToXYZ(v3.X, v3.Y, v3.TopZ);
                //Получаем точки низа
                XYZ bot1 = ToXYZ(v1.X, v1.Y, v1.BottomZ);
                XYZ bot2 = ToXYZ(v2.X, v2.Y, v2.BottomZ);
                XYZ bot3 = ToXYZ(v3.X, v3.Y, v3.BottomZ);

                //Верхняя грань
                builder.AddFace(new TessellatedFace(new List<XYZ>() { top1, top2, top3 }, ElementId.InvalidElementId));
                //Нижняя грань
                builder.AddFace(new TessellatedFace(new List<XYZ>() { bot1, bot3, bot2 }, ElementId.InvalidElementId));
                //Боковые грани
                /*AddSideFace(builder, top1, top2, bot2, bot1);
                AddSideFace(builder, top2, top3, bot3, bot2);
                AddSideFace(builder, top3, top1, bot1, bot3);*/
            }

            //Закрываем набор граней
            builder.CloseConnectedFaceSet();
            builder.Target = TessellatedShapeBuilderTarget.Mesh;
            builder.Fallback = TessellatedShapeBuilderFallback.Mesh;
            builder.Build();

            //Получаем результат
            TessellatedShapeBuilderResult result = builder.GetBuildResult();

            //Создаем DirectShape
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));

            ds.Name = ige;

            ds.SetShape(result.GetGeometricalObjects());
        }
        private XYZ ToXYZ(double x, double y, double z)
        {
            return new XYZ(UnitUtils.ConvertToInternalUnits(x * 1000, UnitTypeId.Millimeters), 
                           UnitUtils.ConvertToInternalUnits(y * 1000, UnitTypeId.Millimeters), 
                           UnitUtils.ConvertToInternalUnits(z * 1000, UnitTypeId.Millimeters));
        }

        private void AddSideFace(TessellatedShapeBuilder builder, XYZ top1, XYZ top2, XYZ bot2, XYZ bot1)
        {
            builder.AddFace(new TessellatedFace(new List<XYZ>()
        {
                top1, top2, bot2
        },
        ElementId.InvalidElementId));

            builder.AddFace(new TessellatedFace(new List<XYZ>() {top1, bot2, bot1}, ElementId.InvalidElementId));
        }
    }
}
