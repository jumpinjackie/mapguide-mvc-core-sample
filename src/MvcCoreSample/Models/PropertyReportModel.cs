namespace MvcCoreSample.Models
{
    public class PropertyReportInputModel : MapGuideCommandModel
    {
        public int Height { get; set; }

        public int Width { get; set; }

        public double CenterX { get; set; }

        public double CenterY { get; set; }

        public string Selection { get; set; }

        public double Scale { get; set; }
    }

    public class PropertyReportModel : MapGuideCommandModel
    {
        public bool NoProperties { get; set; }

        public string Owner { get; set; }

        public string Address { get; set; }

        public string BillingAddress { get; set; }

        public string[] Description { get; set; }

        public (string ImageUrl, int width, int height) Image { get; set; }
    }
}