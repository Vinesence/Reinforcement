using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace Reinforcement

{
    [Transaction(TransactionMode.Manual)]

    public class DecorColumns : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            if (RevitAPI.UiApplication == null)
            {
                RevitAPI.Initialize(commandData);
            }
            UIApplication uiapp = RevitAPI.UiApplication;
            UIDocument uidoc = RevitAPI.UiDocument;
            Document doc = RevitAPI.Document;
            Selection selection = uidoc.Selection;
            var activeView = doc.ActiveView;
            var columnsList = selection.PickElementsByRectangle(new ColumnSelectionFilter(activeView.GenLevel.Id))
                .Cast<FamilyInstance>()
                .ToList();

            Options opt = new Options()
            {
                ComputeReferences = true,
                View = activeView
            };

            List<Grid> gridList = new FilteredElementCollector(doc, activeView.Id)
                 .OfClass(typeof(Grid))
                 .ToElements()
                 .Cast<Grid>()
                 .ToList(); //get all grids on activeView

            if (gridList.Count == 0)
            {
                MessageBox.Show("На виде должны быть оси!");
                return Result.Failed;
            }


            List<Grid> XGridList = gridList
                .Where(x => Math.Abs(x.get_Geometry(opt).Select(n => n as Line).First().Direction.X) == 1)
                .ToList();
            List<Grid> YGridList = gridList
                .Where(x => Math.Abs(x.get_Geometry(opt).Select(n => n as Line).First().Direction.Y) == 1)
                .ToList();


            /*List<FamilyInstance> columnsList = new FilteredElementCollector(doc, activeView.Id)
                .OfCategory(BuiltInCategory.OST_StructuralColumns)
                .ToElements()
                .Cast<FamilyInstance>()
                .Where(x => x.LevelId == activeView.GenLevel.Id)
                .ToList();*/
            Options options = new Options()
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true,
            };

            try
            {
                using (Transaction t = new Transaction(doc, "Образмерка несущих колонн"))
                {
                    t.Start();
                    foreach (var column in columnsList)
                    {
                        //selection.SetElementIds(new List<ElementId>  { column.Id });
                        var geometryInstance = column.get_Geometry(options);
                        Solid solid = null;
                        if (column.HasModifiedGeometry())
                        {
                            solid = geometryInstance.OfType<Solid>().First(x => x.Volume > 0);
                        }
                        else
                        {
                            solid = geometryInstance.OfType<GeometryInstance>().First().GetSymbolGeometry().OfType<Solid>().First(x => x.Volume > 0);
                        }
                        var faceArray = solid.Faces;
                        ReferenceArray faceX = new ReferenceArray(); ReferenceArray faceY = new ReferenceArray();
                        foreach (PlanarFace face in faceArray)
                        {
                            if (face.FaceNormal.Z == 0)
                            {                         
                                if (Math.Abs(face.FaceNormal.X) == 1)
                                {
                                    faceY.Append(face.Reference);
                                }
                                if (Math.Abs(face.FaceNormal.Y) == 1)
                                {
                                    faceX.Append(face.Reference);
                                }
                            }
                        }
                        if (faceX.Size == 0 || faceY.Size == 0) { continue; }
                        var levelElevation = activeView.GenLevel.ProjectElevation;
                        var boundingBox = column.get_BoundingBox(activeView);
                        var bbMax = boundingBox.Max;
                        var bbMin = boundingBox.Min;
                        var offset = RevitAPI.ToFoot(5 * activeView.Scale);
                        var p1 = new XYZ(bbMin.X, bbMin.Y-offset, levelElevation);
                        var p2 = new XYZ(bbMax.X, bbMin.Y-offset, levelElevation);
                        var p3 = new XYZ(bbMin.X - offset, bbMin.Y, levelElevation);
                        var p4 = new XYZ(bbMin.X - offset, bbMax.Y, levelElevation);




                        // Строим линию для размера
                        Line lineDim = Line.CreateBound(p1, p2);
                        doc.Create.NewDimension(activeView, lineDim, faceY);
                        lineDim = Line.CreateBound(p3, p4);
                        var dim = doc.Create.NewDimension(activeView, lineDim, faceX);
                        selection.SetElementIds(new List<ElementId> { dim.Id});
                    }
                    t.Commit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return Result.Failed;
            }
            return Result.Succeeded;
        }
        private class ColumnSelectionFilter : ISelectionFilter
        {
            private readonly ElementId _Id;
            public ColumnSelectionFilter(ElementId id)
            {
                _Id = id;
            }
            public bool AllowElement(Element elem)
            {
                if (elem.LevelId == _Id && elem.Category.GetHashCode() == (int)BuiltInCategory.OST_StructuralColumns)
                {
                    return true;
                }
                throw new NotImplementedException();
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                throw new NotImplementedException();
            }
        }
    }
}
