using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Reinforcement
{
    [Transaction(TransactionMode.Manual)]

    public class SetWorkPlane3D : IExternalCommand
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
            Selection sel = uidoc.Selection;

            View3D view = doc.ActiveView as View3D;
            ViewOrientation3D orientation = view.GetOrientation();
            XYZ normal = orientation.ForwardDirection.Normalize();

            BoundingBoxXYZ box = view.GetSectionBox();
            XYZ center = (box.Min + box.Max) / 2;

            Plane plane = Plane.CreateByNormalAndOrigin(normal, center);

            try
            {
                using (Transaction t = new Transaction(doc, "MEDplugin"))
                {
                    t.Start();
                    SketchPlane sp = SketchPlane.Create(doc, plane);
                    view.SketchPlane = sp;
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
    }
}
