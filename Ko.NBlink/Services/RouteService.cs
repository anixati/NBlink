using Ko.NBlink.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Ko.NBlink
{
    public class RoutingService : IRoutingService
    {
        private IServiceScopeFactory _scopeFactory;
        private ConcurrentDictionary<string, RouteInfo> _routeMap = new ConcurrentDictionary<string, RouteInfo>();

        public RoutingService(IServiceScopeFactory scoper)
        {
            _scopeFactory = scoper;
        }

        public ActionResult Execute(ActionContext context)
        {
            if (!_routeMap.TryGetValue(context.Method, out RouteInfo routeInfo))
            {
                throw new Exception("Route not found!");
            }

            var rval = GetActionResult(routeInfo, context);
            if (rval != null)
            {
                if (rval is ActionResult rs)
                {
                    rs.Method = context.Method;
                    rs.MethodId = context.GetMethodId();
                    return rs;
                }
            }
            return null;
        }

        private object GetActionResult(RouteInfo routeInfo, ActionContext context)
        {
            using (var scope = _scopeFactory.CreateScope())
            {
                var controller = scope.ServiceProvider.GetServices<IController>()
                    .FirstOrDefault(x => x.GetType() == routeInfo.Controller);
                if (controller == null)
                {
                    throw new Exception("unable to reolve controller");
                }
                controller.Initialise(scope.ServiceProvider.GetService<IBlinkDispatcher>());

                var paramObjs = GetContextParams(routeInfo, context);
                return routeInfo.Method.Invoke(controller, paramObjs);
            }
        }

        private static object[] GetContextParams(RouteInfo routeInfo, ActionContext context)
        {
            if (routeInfo.ParamCount == 0)
            {
                return new object[] { };
            }

            if (!context.Payload.HasValues)
            {
                throw new Exception($"Invalid Action context!no parameter data");
            }
            var pmObject = (JArray)context.Payload;

            if (routeInfo.IsParamComplexType)
            {
                return GetComplexParam(routeInfo, pmObject.FirstOrDefault());
            }

            var inArgs = pmObject.Children().ToArray();
            var methArgs = routeInfo.Parameters.OrderBy(x => x.Position).ToArray();

            if (inArgs.Length != methArgs.Length)
            {
                throw new Exception($"Invalid Action context! invalid no of args");
            }

            object[] args = new object[methArgs.Length];
            for (var i = 0; i < methArgs.Length; i++)
            {
                var rp = methArgs[i];
                var inObj = inArgs[i];
                args[i] = (inObj != null) ? inObj.ToObject(rp.ParamType) : Type.Missing;
            }
            return args;
        }

        private static object[] GetComplexParam(RouteInfo routeInfo, JToken pmObject)
        {
            var cplxtype = routeInfo.Parameters.First().ParamType;
            var cplxObj = new JsonSerializer().Deserialize(new JTokenReader(pmObject), cplxtype);
            if (cplxObj == null)
            {
                throw new Exception($"Invalid Action context! Unable to deserialize to {cplxtype}");
            }
            return new object[] { cplxObj };
        }

        public void SetupRoutes()
        {
            foreach (var route in GetRoutes(_scopeFactory))
            {
                var key = $"{route.ControllerName}_{route.ActionName}";

                if (_routeMap.ContainsKey(key))
                {
                    throw new Exception($"Method overloading is not supported ! Controller : {route.ControllerName} Method : {route.ActionName}");
                }

                _routeMap[key] = route;
            }
        }

        private static IEnumerable<RouteInfo> GetRoutes(IServiceScopeFactory factory)
        {
            using (var scope = factory.CreateScope())
            {
                foreach (var controller in scope.ServiceProvider.GetServices<IController>())
                {
                    var ctype = controller.GetType();
                    foreach (var mi in ctype
                        .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                        .Where(x => x.ReturnType == typeof(ActionResult) || x.ReturnType.BaseType == typeof(ActionResult)))
                    {
                        yield return RouteInfo.Create(ctype, mi);
                    }
                }
            }
        }

        public List<string> GetBindingRoutes()
        {
            return _routeMap.Where(x => x.Value.IsBinding).Select(x => x.Key).ToList();
        }

        private class RouteInfo
        {
            private RouteInfo(Type controllerType, MethodInfo methodInfo)
            {
                Controller = controllerType;
                Method = methodInfo;
                ControllerName = controllerType.Name.Replace("Controller", "");
                ActionName = Method.Name;
                Parameters = new List<RouteParam>();
            }

            public Type Controller { get; private set; }
            public MethodInfo Method { get; private set; }
            public string ControllerName { get; private set; }
            public string ActionName { get; private set; }
            public bool IsBinding { get; private set; }
            public bool IsParamComplexType { get; private set; }
            public IList<RouteParam> Parameters { get; private set; }
            public int ParamCount => Parameters.Count;
            public bool HasParameters => ParamCount > 0;

            public static RouteInfo Create(Type controllerType, MethodInfo methodInfo)
            {
                var ri = new RouteInfo(controllerType, methodInfo)
                {
                    IsBinding = methodInfo.ReturnType == typeof(BindingResult)
                };
                var plist = methodInfo.GetParameters();
                if (plist != null)
                {
                    ri.Parameters = plist.Select(x => new RouteParam
                    {
                        Name = x.Name.ToLower(),
                        Position = x.Position,
                        ParamType = x.ParameterType,
                    }).ToList();
                    ri.IsParamComplexType = ri.ParamCount == 1 && !ri.Parameters[0].IsPrimitive();
                }
                return ri;
            }

            public RouteParam GetParam(string key)
            {
                return Parameters.FirstOrDefault(x => x.Name == key);
            }

            public override string ToString()
            {
                return $"{ControllerName}_{ActionName} P:{ParamCount}";
            }
        }

        private class RouteParam
        {
            public string Name { get; set; }
            public int Position { get; set; }
            public Type ParamType { get; set; }

            public bool IsPrimitive()
            {
                if (ParamType.IsPrimitive || ParamType == typeof(Decimal) || ParamType == typeof(string))
                {
                    return true;
                }

                return false;
            }
        }
    }
}