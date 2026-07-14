using CreateSoil;
using ExcelDataReader;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Reinforcement
{
    public class ExcelReader
    {
        public List<Borehole> Read(string filePath)
        {
            List<Borehole> boreholes =
                new List<Borehole>();

            using (var stream = File.Open(
                filePath,
                FileMode.Open,
                FileAccess.Read))
            using (var reader =
                ExcelReaderFactory.CreateReader(stream))
            {
                List<List<object>> rows =
                    new List<List<object>>();

                while (reader.Read())
                {
                    List<object> row =
                        new List<object>();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row.Add(reader.GetValue(i));
                    }

                    rows.Add(row);
                }

                if (rows.Count < 5)
                    throw new Exception(
                        "Неверный формат Excel.");

                int boreholeCount =
                    rows[0].Count - 1;

                for (int col = 1;
                    col <= boreholeCount;
                    col++)
                {
                    Borehole borehole =
                        new Borehole();

                    borehole.Name =
                        rows[0][col]?.ToString();

                    borehole.X =
                        ParseDouble(rows[1][col]);

                    borehole.Y =
                        ParseDouble(rows[2][col]);

                    borehole.GroundElevation =
                        ParseDouble(rows[3][col]);

                    double currentElevation =
                        borehole.GroundElevation;

                    // строки ИГЭ начинаются с 4-й
                    // последняя строка - Общая глубина
                    for (int row = 4;
                        row < rows.Count - 1;
                        row++)
                    {
                        string igeName =
                            rows[row][0]?.ToString();

                        if (string.IsNullOrWhiteSpace(
                            igeName))
                            continue;

                        object value =
                            rows[row][col];

                        // если слой отсутствует
                        if (value == null ||
                            string.IsNullOrWhiteSpace(
                                value.ToString()))
                        {
                            continue;
                        }

                        double thickness =
                            ParseDouble(value);

                        SoilLayer layer =
                            new SoilLayer();

                        layer.IGE =
                            igeName;

                        layer.Thickness =
                            thickness;

                        layer.TopElevation =
                            currentElevation;

                        currentElevation -=
                            thickness;

                        layer.BottomElevation =
                            currentElevation;

                        borehole.Layers.Add(
                            layer);
                    }

                    boreholes.Add(
                        borehole);
                }
            }

            return boreholes;
        }

        private double ParseDouble(
            object value)
        {
            if (value == null)
                return 0;

            string str =
                value.ToString()
                .Replace(",", ".");

            return double.Parse(
                str,
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}