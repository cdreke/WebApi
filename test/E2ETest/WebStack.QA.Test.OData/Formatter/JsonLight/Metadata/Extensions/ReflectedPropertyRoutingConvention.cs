﻿using System.Linq;
using System.Web.Http.Controllers;
using System.Web.OData.Routing;
using System.Web.OData.Routing.Conventions;
using Microsoft.OData.Edm;

namespace WebStack.QA.Test.OData.Formatter.JsonLight.Metadata.Extensions
{
    public class ReflectedPropertyRoutingConvention : EntitySetRoutingConvention
    {
        public override string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath.PathTemplate == "~/entityset/key/property" || odataPath.PathTemplate == "~/entityset/key/cast/property")
            {
                var segment = odataPath.Segments.Last() as PropertyAccessPathSegment;
                var property = segment.Property;
                var declareType = property.DeclaringType as IEdmEntityType;
                if (declareType != null)
                {
                    var key = odataPath.Segments[1] as KeyValuePathSegment;
                    controllerContext.RouteData.Values.Add(ODataRouteConstants.Key, key.Value);
                    controllerContext.RouteData.Values.Add("property", property.Name);
                    string prefix = ODataHelper.GetHttpPrefix(controllerContext.Request.Method.ToString());
                    if (string.IsNullOrEmpty(prefix))
                    {
                        return null;
                    }
                    string action = prefix + "Property" + "From" + declareType.Name;
                    return actionMap.Contains(action) ? action : prefix + "Property";
                }
            }

            return null;
        }
    }
}
