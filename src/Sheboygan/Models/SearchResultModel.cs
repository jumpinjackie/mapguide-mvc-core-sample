using OSGeo.MapGuide;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class ParcelFeatureModel
    {
        public int ID { get; set; }

        public string Owner { get; set; }

        public string Address { get; set; }

        public double? X { get; set; }

        public double? Y { get; set; }
    }

    public class SearchResultViewModel : MapGuideViewModel
    {
        public SearchResultViewModel()
        {
            this.Results = new List<ParcelFeatureModel>();
        }

        public void LoadResults(MgLayerBase layer, MgFeatureQueryOptions query)
        {
            MgClassDefinition clsDef = layer.GetClassDefinition();
            MgPropertyDefinitionCollection idProps = clsDef.GetIdentityProperties();
            MgPropertyDefinition idProp = idProps.GetItem(0);
            string idPropName = idProp.Name;
            string geomName = layer.FeatureGeometryName;

            MgFeatureReader reader = layer.SelectFeatures(query);
            MgAgfReaderWriter agfRw = new MgAgfReaderWriter();
            try
            {
                while (reader.ReadNext())
                {
                    var res = new ParcelFeatureModel();
                    res.ID = reader.GetInt32(idPropName);
                    res.Owner = reader.IsNull("RNAME") ? "(unknown)" : reader.GetString("RNAME");
                    res.Address = reader.IsNull("RPROPAD") ? "(unknown)" : reader.GetString("RPROPAD");
                    if (!reader.IsNull(geomName))
                    {
                        MgByteReader agf = reader.GetGeometry(geomName);
                        MgGeometry geom = agfRw.Read(agf);
                        MgPoint center = geom.Centroid;
                        MgCoordinate coord = center.Coordinate;

                        res.X = coord.X;
                        res.Y = coord.Y;
                    }
                    this.Results.Add(res);
                }
            }
            finally
            {
                reader.Close();
            }
        }

        public List<ParcelFeatureModel> Results { get; set; }
    }
}
