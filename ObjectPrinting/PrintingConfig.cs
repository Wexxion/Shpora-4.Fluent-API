using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace ObjectPrinting
{
    public class PrintingConfig<TOwner>
    {
        private readonly string newLine = Environment.NewLine;
        // ReSharper disable once StaticMemberInGenericType
        private static readonly Type[] FinalTypes =
        {
            typeof(int), typeof(double), typeof(float), typeof(string),
            typeof(DateTime), typeof(TimeSpan)
        };

        public PrintingConfig()
        {
            ExcludingTypes = new List<Type>();
            ExcludingProprties = new List<PropertyInfo>();
            TrimmingLength = new Dictionary<PropertyInfo, int>();
            CustomTypeSerialization = new Dictionary<Type, Func<object, string>>();
            NumericCulture = new Dictionary<Type, CultureInfo>();
            CustomPropertySerialization = new Dictionary<PropertyInfo, Func<object, string>>();
        }

        internal Dictionary<PropertyInfo, Func<object, string>> CustomPropertySerialization { get; }

        internal Dictionary<Type, Func<object, string>> CustomTypeSerialization { get; }

        internal List<Type> ExcludingTypes { get; }

        internal List<PropertyInfo> ExcludingProprties { get; }

        internal Dictionary<Type, CultureInfo> NumericCulture { get; }

        internal Dictionary<PropertyInfo, int> TrimmingLength { get; }
 

        public PrintingConfig<TOwner> Excluding<TPropType>()
        {
            ExcludingTypes.Add(typeof(TPropType));
            return this;
        }

        public PrintingConfig<TOwner> Excluding<TPropType>(Expression<Func<TOwner, TPropType>> memberSelector)
        {
            var propInfo = (PropertyInfo) ((MemberExpression) memberSelector.Body).Member;
            ExcludingProprties.Add(propInfo);
            return this;
        }

        public PropertyPrintingConfig<TOwner, TPropType> Printing<TPropType>() 
            => new PropertyPrintingConfig<TOwner, TPropType>(this, typeof(TPropType));

        public PropertyPrintingConfig<TOwner, TPropType> Printing<TPropType>
            (Expression<Func<TOwner, TPropType>> memberSelector)
        {
            var propInfo = (PropertyInfo) ((MemberExpression) memberSelector.Body).Member;
            return new PropertyPrintingConfig<TOwner, TPropType>(this, propInfo);
        }

        public string PrintToString(TOwner obj) => PrintToString(obj, 0);

        private string PrintToString(object obj, int nestingLevel)
        {
            if (obj == null)
                return "null" + newLine;

            var type = obj.GetType();

            if (TryCovertNumericWithCulture(obj, type, out var result))
                return result;

            if (TryConvertTypeWithCustomParams(obj, type, out result))
                return result;

            if (TryConvertFinalType(obj, type, out result))
                return result;

            var identation = new string('\t', nestingLevel + 1);
            var sb = new StringBuilder();
            
            sb.AppendLine(type.Name);
            foreach (var propertyInfo in type.GetProperties())
            {
                if (IgnoreProperty(propertyInfo)) continue;
                if (!TryConvertPropertyWithCustomParams(obj, propertyInfo, out var propertyValue))
                    propertyValue = PrintToString(propertyInfo.GetValue(obj), nestingLevel + 1);
                propertyValue = TryTrimPropertyValue(propertyInfo, propertyValue);
                sb.Append(identation + propertyInfo.Name + " = " + propertyValue);
            }
            return sb.ToString();
        }
        private string TryTrimPropertyValue(PropertyInfo property, string propertyValue)
        {
            if (!TrimmingLength.ContainsKey(property)) return propertyValue;
            return TrimmingLength[property] < propertyValue.Length ?
                propertyValue.Substring(0, TrimmingLength[property]) + newLine : propertyValue;
        }

        private bool TryConvertPropertyWithCustomParams(object obj, PropertyInfo property, out string result)
        {
            result = null;
            if (!CustomPropertySerialization.ContainsKey(property)) return false;
            result = CustomPropertySerialization[property](property.GetValue(obj)) + newLine;
            return true;
        }

        private bool TryConvertFinalType(object obj, Type type, out string result)
        {
            result = null;
            if (!FinalTypes.Contains(type)) return false;
            result = obj + newLine;
            return true;
        }

        private bool TryCovertNumericWithCulture(object obj, Type type, out string result)
        {
            result = null;
            if (!NumericCulture.ContainsKey(type)) return false;
            result = ConverNumeric(obj, type) + newLine;
            return true;
        }

        private bool TryConvertTypeWithCustomParams(object obj, Type type, out string result)
        {
            result = null;
            if (!CustomTypeSerialization.ContainsKey(type)) return false;
            result = CustomTypeSerialization[type](obj) + newLine;
            return true;
        }

        private string ConverNumeric(object obj, Type type)
        {
            if (obj is IFormattable formattableObj)
                return formattableObj.ToString(null, NumericCulture[type]);
            throw new InvalidCastException("Only IFormattable types supported");
        }

        private bool IgnoreProperty(PropertyInfo propertyInfo)
        {
            return ExcludingTypes.Contains(propertyInfo.PropertyType)
                   || ExcludingProprties.Contains(propertyInfo);
        }
    }
}