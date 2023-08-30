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

        public static void SetCollectionProperty(object modelContext, string property, string collectionType, string className)
        {
            object collection = CreateCollectionInstance(collectionType, className);
            SetProperty(modelContext, property, collection);
        }

        // sets the object's property with given value
        public static void SetProperty(object modelContext, string property, object value)
        {
            PropertyInfo propertyInfo = modelContext.GetType().GetProperty(property);
            propertyInfo.SetValue(modelContext, Convert.ChangeType(value, propertyInfo.PropertyType));
        }

        // adds to object's collection given value
        public static void AddCollectionItem(object modelContext, string property, object value)
        {
            PropertyInfo propertyInfo = modelContext.GetType().GetProperty(property);
            object collection = propertyInfo.GetValue(modelContext, null);
            collection.GetType().GetMethod("Add").Invoke(collection, new[] { value });
        }

        // creates an instance of given collection type
        private static object CreateCollectionInstance(string collectionType, string className)
        {
            Type type = TypeResolver.resolve(collectionType);
            Type[] typeArgs = { TypeResolver.resolve(className) };
            return Activator.CreateInstance(type.MakeGenericType(typeArgs));
        }
    }
}
