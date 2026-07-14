using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExcelDataReader;
using System.Windows.Controls;
using Reinforcement;
using Reinforcement.CreateSoil;

namespace CreateSoil
{
    [Transaction(TransactionMode.Manual)]

    public class CreateSoil : IExternalCommand
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
            
            try //ловим ошибку
            {
                // Выбор файла
                string filePath = SelectExcelFile();

                if (string.IsNullOrEmpty(filePath))
                    return Result.Cancelled;

                // Чтение Excel
                ExcelReader excelReader = new ExcelReader();
                List<Borehole> boreholes = excelReader.Read(filePath);
                
                // Построение модели
                GeologyBuilder geologyBuilder = new GeologyBuilder();
                var geology = geologyBuilder.Build(boreholes);
                
                // Создание DirectShape
                using (Transaction t = new Transaction(doc, "Создание геологии"))
                {
                    t.Start();

                    DirectShapeBuilder builder = new DirectShapeBuilder();

                    builder.Create(doc, geology);

                    t.Commit();
                }
            }
            catch (Exception ex)
            {
                //Код в случае ошибки
                MessageBox.Show("Чет пошло не так!\n" + ex.Message);
                return Result.Failed;
            }
            return Result.Succeeded;
        }





        private string SelectExcelFile()
        {
            OpenFileDialog dialog =
                new OpenFileDialog();

            dialog.Title =
                "Выберите файл ИГИ";

            dialog.Filter =
                "Excel (*.xlsx;*.xls)|*.xlsx;*.xls";

            dialog.Multiselect = false;

            if (dialog.ShowDialog() ==
                DialogResult.OK)
            {
                return dialog.FileName;
            }

            return null;
        }
    }
}
