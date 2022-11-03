namespace MvcCoreSample.Models;

public class GotoPointModel : MapGuideCommandModel
{
    public double X { get; set; }

    public double Y { get; set; }

    public double Scale { get; set; }
}