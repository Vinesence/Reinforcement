﻿using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

namespace Reinforcement
{
    [Transaction(TransactionMode.Manual)]

    public class RcShpilkaCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;
            //Create window instance
            var notification = new TransparentNotificationWindow("Выберите поперечное\nсечение стержня", uidoc, 0);
            try
            {
                Selection sel = uidoc.Selection;
                ISelectionFilter selFilter = new SelectionFilter();
                notification.Show();
                IList<Reference> sec = sel.PickObjects(ObjectType.Element, selFilter,"Выберите поперечное сечение стержня");
                notification.Close();

                if (sec.Count == 0)
                {
                    MessageBox.Show("Ничего не выбрано");
                    return Result.Failed;
                }
                IList<XYZ> Pt = new List<XYZ>();
                Options options = new Options();
                options.View = uidoc.ActiveView;
                double diameter = 0;
                double diameterBend = 0;
                foreach (Reference secObj in sec)
                {
                    Element element = doc.GetElement(secObj);
                    LocationPoint locationPts = element.Location as LocationPoint;
                    diameter = element.LookupParameter("• Диаметр").AsDouble();
                    diameterBend = diameter;
                    if (diameterBend < RevitAPI.ToFoot(2.5 * 6))
                    {
                        diameterBend = RevitAPI.ToFoot(2.5 * 6);
                    }
                    Pt.Add(locationPts.Point);
                }
                XYZ xyz =  Pt.ElementAt(0).Subtract(Pt.ElementAt(1));
                //округляем до 10 знаков, а то может быть погрешность и detail line won't create            
                xyz = new XYZ
                    (
                        Math.Round(xyz.X, 10),
                        Math.Round(xyz.Y, 10),
                        Math.Round(xyz.Z, 10)
                    );
                double move = diameter / 2 ;
                XYZ subst1 = null, subst2 = null;
                if (xyz.X < 0)
                {
                    subst1 = new XYZ(move, 0, 0);
                    subst2 = new XYZ(-move, 0, 0);
                }
                else if (xyz.X > 0)
                {
                    subst1 = new XYZ(-move, 0, 0);
                    subst2 = new XYZ(move, 0, 0);
                }
                else if (xyz.Y < 0)
                {
                    subst1 = new XYZ(0, move, 0);
                    subst2 = new XYZ(0, -move, 0);
                }
                else if (xyz.Y > 0)
                {
                    subst1 = new XYZ(0, -move, 0);
                    subst2 = new XYZ(0, move, 0);
                }
                else if (xyz.Z != 0)
                {
                    subst1 = new XYZ(0, 0, move);
                    subst2 = new XYZ(0, 0, -move);
                }
                Line line = Line.CreateBound(Pt.ElementAt(0).Subtract(subst1), Pt.ElementAt(1).Subtract(subst2));
                // Retrieve elements from database
                FilteredElementCollector col = new FilteredElementCollector(doc);
                IList<Element> symbols = col.OfClass(typeof(FamilySymbol)).WhereElementIsElementType().ToElements();
                foreach (var elem in symbols)
                {
                    ElementType elemType = elem as ElementType;
                    if (elemType.FamilyName == FamName && elemType.Name == exampleName)
                    {
                        FamilySymbol symbol = elem as FamilySymbol;
                        using (Transaction t = new Transaction(doc, "Создание шпильки"))
                        {
                            t.Start();                       
                            var detailLine = doc.Create.NewDetailCurve(uidoc.ActiveView, line);
                            var baseLine = detailLine.GeometryCurve as Line;
                            int lengthEnds = 40;
                            if (baseLine.Length < RevitAPI.ToFoot(110))
                            {
                                lengthEnds -= 10;
                            }
                            FamilyInstance familyInstance = doc.Create.NewFamilyInstance(baseLine, symbol, uidoc.ActiveView);
                            familyInstance.LookupParameter("Объемный вид").Set(1);
                            familyInstance.LookupParameter("Вид с торца").Set(0);
                            familyInstance.LookupParameter("Заливка").Set(0);
                            familyInstance.LookupParameter("Радиус основы").Set(diameterBend / 2);
                            familyInstance.LookupParameter("• Диаметр").Set(RevitAPI.ToFoot(6));
                            familyInstance.LookupParameter("Длина отгиба").Set(RevitAPI.ToFoot(lengthEnds));
                            doc.Delete(detailLine.Id);
                            t.Commit();
                        }
                        break;
                    }
                }
                /*
                using (Transaction t = new Transaction(doc, "действие"))
                {
                    t.Start();
                    //Тут пишем основной код
                    t.Commit();
                }
                */
            }
            catch (Exception ex)
            {
                //Код в случае ошибки
                return Result.Failed;
            }
            finally
            {
                if (notification != null)
                {
                    notification.Close();
                }
            }
            return Result.Succeeded;          
        }

        public class SelectionFilter  : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                FamilyInstance instance = element as FamilyInstance;
                if (instance.Symbol.FamilyName.ToLower().Contains("точка"))
                {
                    return true;
                }
                return false;
            }

            public bool AllowReference(Reference reference, XYZ point)
            {
                return false;
            }

        }

        public static string FamName { get; set; } = "ЕС_А-23_Шпилька";
        public static string exampleName { get; set; } = "А240";

    }
}
