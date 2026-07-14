using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateSoil
{
    public class GeologyBuilder
    {
        public Dictionary<string, List<LayerPoint>> Build(List<Borehole> boreholes)
        {
            Dictionary<string, List<LayerPoint>> result = new Dictionary<string, List<LayerPoint>>();

            HashSet<string> allIGE = new HashSet<string>();

            foreach (var borehole in boreholes)
            {
                foreach (var layer in borehole.Layers)
                {
                    allIGE.Add(layer.IGE);
                }
            }

            foreach (string ige in allIGE)
            {
                result.Add(ige, new List<LayerPoint>());
            }

            foreach (string ige in allIGE)
            {
                foreach (var borehole in boreholes)
                {
                    SoilLayer layer = borehole.Layers.FirstOrDefault(x => x.IGE == ige);

                    if (layer != null)
                    {
                        result[ige].Add(new LayerPoint()
                            {
                                IGE = ige,

                                X = borehole.X,
                                Y = borehole.Y,

                                TopZ = layer.TopElevation,

                                BottomZ = layer.BottomElevation
                            });
                    }
                    else
                    {
                        double z = FindZeroThicknessLevel(borehole, ige);

                        result[ige].Add(new LayerPoint()
                            {
                                IGE = ige,

                                X = borehole.X,
                                Y = borehole.Y,

                                TopZ = z,

                                BottomZ = z
                            });
                    }
                }
            }

            return result;
        }

        private double FindZeroThicknessLevel(Borehole borehole, string ige)
        {
            double currentElevation = borehole.GroundElevation;

            foreach (var layer in borehole.Layers)
            {
                if (string.Compare(layer.IGE,ige) >= 0)
                {
                    return currentElevation;
                }

                currentElevation -= layer.Thickness;
            }

            return currentElevation;
        }
    }
}
