using Ganss.Xss;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Collections;
using System.Reflection;

namespace EVWebApi.Helpers.Security.Filters
{
    public class XssSanitizationFilter : IActionFilter
    {
        private readonly HtmlSanitizer _sanitizer = new HtmlSanitizer();
        public XssSanitizationFilter()
        {
            _sanitizer.AllowedTags.Clear();
            _sanitizer.AllowedTags.Add("b");
            _sanitizer.AllowedTags.Add("i");
            _sanitizer.AllowedTags.Add("u");
            _sanitizer.AllowedTags.Add("strong");
            _sanitizer.AllowedTags.Add("em");
        }
        public void OnActionExecuting(ActionExecutingContext context)
        {
            try
            {
                foreach (var arg in context.ActionArguments.Values)
                {
                    if (arg == null) continue;
                    SanitizeObject(arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("XSS Filter Error: " + ex);
            }
        }


        private void SanitizeObject(object obj, HashSet<object>? visited = null)
        {
            if (obj == null) return;

            visited ??= new HashSet<object>();
            if (visited.Contains(obj)) return;
            visited.Add(obj);

            var type = obj.GetType();

            // skip system/framework types
            if (type.Namespace == null ||
                type.Namespace.StartsWith("System") ||
                type.Namespace.StartsWith("Microsoft") ||
                type.Namespace.StartsWith("Newtonsoft"))
                return;

            // handle collections
            if (obj is IEnumerable enumerable && !(obj is string))
            {
                foreach (var item in enumerable)
                {
                    SanitizeObject(item, visited);
                }
                return;
            }

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                object? value;
                try
                {
                    value = prop.GetValue(obj);
                }
                catch
                {
                    continue;
                }

                if (value == null) continue;

                if (prop.PropertyType == typeof(string) && prop.CanWrite)
                {
                    try
                    {
                        var clean = _sanitizer.Sanitize(value.ToString())?.Trim();
                        prop.SetValue(obj, clean);
                    }
                    catch { }
                }
                else if (!prop.PropertyType.IsPrimitive &&
                         prop.PropertyType != typeof(DateTime) &&
                         prop.PropertyType != typeof(Guid) &&
                         !prop.PropertyType.IsEnum)
                {
                    SanitizeObject(value, visited);
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        { 

        }
    }
    
    
}
