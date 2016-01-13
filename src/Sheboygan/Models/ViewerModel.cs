using Microsoft.AspNet.Html.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sheboygan.Models
{
    public class ViewerModel
    {
        public ViewerModel(string mapDefinition, string homeUrl)
        {
            this.MapDefinition = mapDefinition;
            this.HomeUrl = homeUrl;
        }

        public string HomeUrl { get; }
        
        public string MapDefinition { get; }

        public IEnumerable<ToolbarItemModel> Toolbar { get; set; }

        public IEnumerable<MenuItemModel> TaskPaneMenu { get; set; }
    }
    
    public abstract class ToolbarItemModel
    {
        public string Tooltip { get; set; }

        public string CssClass { get; set; }

        public abstract string Render();
    }
    
    public class LinkMenuToolbarItemModel : ToolbarItemModel
    {
        public string Url { get; set; }

        public string Target { get; set; }

        public override string Render() => $"<a href='{Url}' target='{Target}' title='{Tooltip}'><span class='{CssClass}'></span>&nbsp;</a>";
    }

    public class JsMenuToolbarItemModel : ToolbarItemModel
    {
        public string OnClick { get; set; }

        public override string Render() => $"<a href='javascript:{OnClick}' title='{Tooltip}'><span class='{CssClass}'></span>&nbsp;</a>";
    }

    public abstract class MenuItemModel : ToolbarItemModel
    {
        public string Label { get; set; }
    }

    public class LinkMenuItemModel : MenuItemModel
    {
        public string Url { get; set; }

        public string Target { get; set; }

        public override string Render() => $"<a href='{Url}' target='{Target}' title='{Tooltip}'><span class='{CssClass}' ></span>&nbsp;{Label}</a>";
    }
    
    public class JsMenuItemModel : MenuItemModel
    {
        public string OnClick { get; set; }

        public override string Render() => $"<a href='javascript:{OnClick}' title='{Tooltip}'><span class='{CssClass}' ></span>&nbsp;{Label}</a>";
    }
}
