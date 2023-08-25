using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace EDIConverter.util
{
    public class ReflectionHandler
    {
        // creates an instance of given class name
        public static object CreateInstance(string className)
        {
            return Activator.CreateInstance(TypeResolver.resolve(className));
        }

        // creates an instance of given collection type
        public static object CreateCollectionInstance(string collectionType, string className)
        {
            Type type = TypeResolver.resolve(collectionType);
            Type[] typeArgs = { TypeResolver.resolve(className) };
            return Activator.CreateInstance(type.MakeGenericType(typeArgs));
        }

        // sets the object's property with given value
        public static void SetProperty(object obj, string property, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(property);
            propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        // adds to object's collection given value
        public static void AddCollectionItem(object obj, string property, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(obj, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }
    }
}
